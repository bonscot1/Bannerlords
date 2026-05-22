using Bannerlords.Coop.Network.Session;

namespace Bannerlords.Coop.Modes
{
    /// <summary>
    /// One implementation per <see cref="CoopModeKind"/>. CoopSession activates
    /// the configured mode after the campaign loads, and routes peer-lifecycle
    /// events to it.
    /// </summary>
    public interface ICoopMode
    {
        CoopModeKind Kind { get; }

        /// <summary>Called once after the campaign is loaded and the session
        /// is live. Host and client both call this; check
        /// <see cref="CoopSession.Role"/> if behavior differs.</summary>
        void Activate(CoopSession session);

        /// <summary>Called when this mode is being torn down (session end /
        /// campaign exit).</summary>
        void Deactivate(CoopSession session);

        /// <summary>Host-only: called when a remote peer finishes handshake.</summary>
        void OnPeerJoined(CoopSession session, CoopPeer peer);

        /// <summary>Host-only: called when a peer leaves (clean or timeout).</summary>
        void OnPeerLeft(CoopSession session, CoopPeer peer);

        /// <summary>Called every <c>CoopSession.Tick</c>. Most modes only
        /// implement this to drain deferred work (e.g. attaches that ran
        /// before a campaign existed).</summary>
        void Tick(CoopSession session, float dt);
    }
}
