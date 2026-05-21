using Bannerlords.Coop.Network.Session;
using Bannerlords.Coop.Util;
using Steamworks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Bannerlords.Coop.UI
{
    /// <summary>
    /// Adds "Host coop game" / "Join coop game" entries to the initial
    /// state menu, and an in-overlay "Invite friend" trigger once hosting.
    /// </summary>
    public static class MainMenuHooks
    {
        private static CoopSession _session;
        private static SteamLobby _lobby;

        public static void Install(CoopSession session, SteamLobby lobby)
        {
            _session = session;
            _lobby = lobby;

            if (_lobby != null)
            {
                _lobby.OnHostedLobbyReady += OnHostedLobbyReady;
                _lobby.OnJoinedLobbyAsClient += OnJoinedLobbyAsClient;
                _lobby.OnLobbyError += OnLobbyError;
            }

            Module.CurrentModule.AddInitialStateOption(new InitialStateOption(
                "Bannerlords.Coop.Host",
                new TextObject("{=BLC_HOST}Host coop game"),
                9990,
                HostCoopGame,
                () => (false, null)));

            Module.CurrentModule.AddInitialStateOption(new InitialStateOption(
                "Bannerlords.Coop.Join",
                new TextObject("{=BLC_JOIN}Join coop game"),
                9991,
                JoinCoopGame,
                () => (false, null)));

            Log.Info("MainMenuHooks", "menu entries installed");
        }

        private static string LocalPersona()
        {
            try { return SteamFriends.GetPersonaName(); }
            catch { return "Player"; }
        }

        // ---------- option handlers ----------

        private static void HostCoopGame()
        {
            Log.Info("MainMenuHooks", "host coop game selected");
            if (_session == null || _lobby == null)
            {
                Log.Error("MainMenuHooks", "session or lobby unavailable");
                return;
            }
            if (!_session.StartHost(LocalPersona())) return;
            _lobby.CreateLobby();
            InformationManager.DisplayMessage(new InformationMessage(
                "Coop: hosting; invite a friend via the Steam overlay (Shift+Tab)."));
        }

        private static void JoinCoopGame()
        {
            Log.Info("MainMenuHooks", "join coop game selected");
            // For M0 the only join path is accepting an invite from the
            // Steam overlay, which fires SteamLobby.OnJoinRequested
            // automatically. The menu entry just tells the user that.
            InformationManager.DisplayMessage(new InformationMessage(
                "Coop: accept a Steam invite from your friend to join."));
        }

        // ---------- lobby callbacks ----------

        private static void OnHostedLobbyReady(CSteamID lobby)
        {
            // Pop the invite overlay as a convenience when the host's lobby
            // is ready.
            _lobby?.OpenInviteOverlay();
        }

        private static void OnJoinedLobbyAsClient(ulong hostSteamId)
        {
            if (_session == null) return;
            if (_session.State != SessionState.Idle)
            {
                Log.Warn("MainMenuHooks", $"already in session ({_session.State}); ignoring lobby join");
                return;
            }
            _session.JoinHost(hostSteamId, LocalPersona());
            InformationManager.DisplayMessage(new InformationMessage(
                "Coop: connecting to host..."));
        }

        private static void OnLobbyError(string reason)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"Coop: lobby error — {reason}"));
        }
    }
}
