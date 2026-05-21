using System;
using Bannerlords.Coop.Util;
using Steamworks;

namespace Bannerlords.Coop.Network.Session
{
    /// <summary>
    /// Thin wrapper around Steam Matchmaking lobbies, used for discovery and
    /// invites only — actual game traffic is on
    /// <see cref="Network.Transport.SteamP2PTransport"/>.
    ///
    /// Host: <see cref="CreateLobby"/> -> Steam fires <c>LobbyCreated_t</c> ->
    /// host invites a friend via the Steam overlay.
    /// Client: accepting an invite fires <c>GameLobbyJoinRequested_t</c> ->
    /// we call <see cref="JoinLobby"/> -> <c>LobbyEnter_t</c> -> caller is
    /// notified of the host's SteamID via <see cref="OnJoinedLobbyAsClient"/>.
    /// </summary>
    public sealed class SteamLobby : IDisposable
    {
        private const string LOBBY_DATA_KEY_MOD = "Bannerlords.Coop";
        private const string LOBBY_DATA_VAL_MOD = "1";

        private Callback<LobbyCreated_t> _cbLobbyCreated;
        private Callback<GameLobbyJoinRequested_t> _cbJoinRequested;
        private Callback<LobbyEnter_t> _cbLobbyEnter;

        private CSteamID _currentLobby = CSteamID.Nil;
        private readonly CoopConfig _config;

        public event Action<CSteamID /*lobby*/> OnHostedLobbyReady;
        /// <summary>Fired on the client after we've joined a lobby; argument
        /// is the host's SteamID (lobby owner).</summary>
        public event Action<ulong /*hostSteamId*/> OnJoinedLobbyAsClient;
        public event Action<string /*reason*/> OnLobbyError;

        public SteamLobby(CoopConfig config) { _config = config; }

        public void Start()
        {
            if (!SteamAPI.IsSteamRunning())
            {
                Log.Error("SteamLobby", "Steam is not running");
                return;
            }
            _cbLobbyCreated  = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            _cbJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
            _cbLobbyEnter    = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            Log.Info("SteamLobby", "lobby callbacks registered");
        }

        public void Dispose()
        {
            LeaveLobby();
            _cbLobbyCreated?.Dispose();
            _cbJoinRequested?.Dispose();
            _cbLobbyEnter?.Dispose();
            _cbLobbyCreated = null;
            _cbJoinRequested = null;
            _cbLobbyEnter = null;
        }

        // ---------- host ----------

        public void CreateLobby()
        {
            var type = _config.FriendsOnly
                ? ELobbyType.k_ELobbyTypeFriendsOnly
                : ELobbyType.k_ELobbyTypePublic;
            SteamMatchmaking.CreateLobby(type, _config.MaxLobbyMembers);
            Log.Info("SteamLobby", $"requested lobby creation (type={type}, max={_config.MaxLobbyMembers})");
        }

        /// <summary>Open the Steam overlay so the user can invite a friend.</summary>
        public void OpenInviteOverlay()
        {
            if (_currentLobby == CSteamID.Nil)
            {
                Log.Warn("SteamLobby", "no lobby to invite to");
                return;
            }
            SteamFriends.ActivateGameOverlayInviteDialog(_currentLobby);
        }

        // ---------- client ----------

        public void JoinLobby(CSteamID lobby)
        {
            SteamMatchmaking.JoinLobby(lobby);
            Log.Info("SteamLobby", $"requested lobby join: {lobby}");
        }

        // ---------- both ----------

        public void LeaveLobby()
        {
            if (_currentLobby == CSteamID.Nil) return;
            SteamMatchmaking.LeaveLobby(_currentLobby);
            Log.Info("SteamLobby", $"left lobby {_currentLobby}");
            _currentLobby = CSteamID.Nil;
        }

        // ---------- callbacks ----------

        private void OnLobbyCreated(LobbyCreated_t evt)
        {
            if (evt.m_eResult != EResult.k_EResultOK)
            {
                Log.Error("SteamLobby", $"lobby creation failed: {evt.m_eResult}");
                OnLobbyError?.Invoke($"create failed: {evt.m_eResult}");
                return;
            }
            _currentLobby = new CSteamID(evt.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyData(_currentLobby, LOBBY_DATA_KEY_MOD, LOBBY_DATA_VAL_MOD);
            Log.Info("SteamLobby", $"hosted lobby {_currentLobby} ready");
            OnHostedLobbyReady?.Invoke(_currentLobby);
        }

        private void OnJoinRequested(GameLobbyJoinRequested_t evt)
        {
            // Fired when the user clicks "Join Game" on a friend's profile or
            // accepts a popup invite.
            Log.Info("SteamLobby", $"join requested for lobby {evt.m_steamIDLobby}");
            JoinLobby(evt.m_steamIDLobby);
        }

        private void OnLobbyEnter(LobbyEnter_t evt)
        {
            if (evt.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Log.Error("SteamLobby", $"failed to enter lobby: {evt.m_EChatRoomEnterResponse}");
                OnLobbyError?.Invoke($"enter failed: {evt.m_EChatRoomEnterResponse}");
                return;
            }
            _currentLobby = new CSteamID(evt.m_ulSteamIDLobby);
            var owner = SteamMatchmaking.GetLobbyOwner(_currentLobby);
            var meSteamId = SteamUser.GetSteamID();
            Log.Info("SteamLobby",
                $"entered lobby {_currentLobby}, owner={owner}, me={meSteamId}");

            if (owner.m_SteamID == meSteamId.m_SteamID)
            {
                // We are the owner — this is the host's own enter callback.
                // Nothing to do; the host already got OnHostedLobbyReady.
                return;
            }
            OnJoinedLobbyAsClient?.Invoke(owner.m_SteamID);
        }
    }
}
