using Bannerlords.Coop.Network.Session;
using Bannerlords.Coop.Util;

namespace Bannerlords.Coop.Modes.CompanionMode
{
    public sealed class CompanionModeManager : ICoopMode
    {
        public CoopModeKind Kind => CoopModeKind.CompanionInArmy;

        public void Activate(CoopSession session)
        {
            Log.Info("CompanionMode", $"activated (role={session.Role})");
        }

        public void Deactivate(CoopSession session)
        {
            Log.Info("CompanionMode", "deactivated");
        }

        public void OnPeerJoined(CoopSession session, CoopPeer peer)
        {
            if (session.Role != CoopRole.Host) return;
            SoldierAttachment.AttachAsSoldier(peer.DisplayName);
        }

        public void OnPeerLeft(CoopSession session, CoopPeer peer)
        {
            // M0 leaves the placeholder troop in place when a peer disconnects
            // mid-session; cleanup arrives with M2's real-hero persistence.
            Log.Info("CompanionMode", $"peer left: {peer} (placeholder troop retained)");
        }
    }
}
