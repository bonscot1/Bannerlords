using System;
using Bannerlords.Coop.Util;

namespace Bannerlords.Coop.Network.Session
{
    /// <summary>
    /// Steam Matchmaking lobby wrapper — STUB. Held off until the Steam
    /// wrapper question (see <see cref="Transport.SteamP2PTransport"/>)
    /// is resolved.
    /// </summary>
    public sealed class SteamLobby : IDisposable
    {
        public event Action<ulong /*lobby steamID*/> OnHostedLobbyReady { add { } remove { } }
        public event Action<ulong /*hostSteamId*/> OnJoinedLobbyAsClient { add { } remove { } }
        public event Action<string> OnLobbyError { add { } remove { } }

        public SteamLobby(Util.CoopConfig _) { }

        public void Start()
        {
            Log.Info("SteamLobby", "stub started (no-op until Steam wrapper choice lands)");
        }

        public void CreateLobby()
        {
            Log.Error("SteamLobby", "stub: CreateLobby called but Steam is not wired");
        }

        public void OpenInviteOverlay() { }
        public void JoinLobby(ulong lobby) { }
        public void LeaveLobby() { }

        public void Dispose() { }
    }
}
