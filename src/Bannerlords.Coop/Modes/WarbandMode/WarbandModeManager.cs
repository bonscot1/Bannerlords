using System;
using Bannerlords.Coop.Network.Session;
using Bannerlords.Coop.Util;

namespace Bannerlords.Coop.Modes.WarbandMode
{
    /// <summary>
    /// Mode B — independent warbands. Stub: the mode is enumerable so menus
    /// can list it, but selecting it throws until M5 lands.
    /// </summary>
    public sealed class WarbandModeManager : ICoopMode
    {
        public CoopModeKind Kind => CoopModeKind.IndependentWarband;

        public void Activate(CoopSession session)
        {
            Log.Error("WarbandMode",
                "IndependentWarband mode is not implemented before M5");
            throw new NotImplementedException(
                "IndependentWarband mode is not implemented before M5 — see ROADMAP.md");
        }

        public void Deactivate(CoopSession session) { }
        public void OnPeerJoined(CoopSession session, CoopPeer peer) { }
        public void OnPeerLeft(CoopSession session, CoopPeer peer) { }
        public void Tick(CoopSession session, float dt) { }
    }
}
