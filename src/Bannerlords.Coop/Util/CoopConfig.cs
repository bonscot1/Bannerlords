using Bannerlords.Coop.Modes;

namespace Bannerlords.Coop.Util
{
    /// <summary>
    /// Mutable runtime configuration for the coop session. M0 defaults are
    /// inlined here; a json loader will come in a later milestone.
    /// </summary>
    public sealed class CoopConfig
    {
        public const ushort ProtocolVersion = 1;

        public CoopModeKind Mode { get; set; } = CoopModeKind.CompanionInArmy;

        /// <summary>Soft cap on connected players. Hard cap on transport is
        /// higher; this is a sanity limit while the protocol is alpha.</summary>
        public int MaxPlayers { get; set; } = 4;

        /// <summary>How often (seconds) to send heartbeat packets.</summary>
        public float HeartbeatInterval { get; set; } = 2.0f;

        /// <summary>Heartbeat misses before we consider a peer dead.</summary>
        public int HeartbeatMissTolerance { get; set; } = 5;

        /// <summary>Logical ticks per real second. Campaign uses variable
        /// speed but our sync layer ticks at a fixed cadence.</summary>
        public int NetworkTickRate { get; set; } = 20;

        /// <summary>Port the host binds to.</summary>
        public int ListenPort { get; set; } = 9000;

        /// <summary>Hostname or IP the client connects to.</summary>
        public string JoinAddress { get; set; } = "127.0.0.1";

        /// <summary>Port the client connects to.</summary>
        public int JoinPort { get; set; } = 9000;

        /// <summary>Shared secret used by LiteNetLib's connection handshake.
        /// Filters obviously-wrong packets at the transport layer; the real
        /// version gate is <see cref="ProtocolVersion"/> in the app
        /// handshake.</summary>
        public string ConnectionKey { get; set; } = "bannerlords-coop-v1";

        /// <summary>If true, transport falls back to in-process loopback
        /// (used by unit tests and headless devloop).</summary>
        public bool UseLoopbackTransport { get; set; } = false;

        // ---------- voting (folded into M0; see ROADMAP M0.7) ----------

        /// <summary>If false, vote requests still go over the wire (so peers
        /// stay in sync) but the responder auto-accepts without showing a
        /// popup. Useful for solo testing and dev workflow; in real play
        /// leave this true.</summary>
        public bool VotingEnabled { get; set; } = true;

        /// <summary>Fallback timeout when an action-specific override isn't
        /// set.</summary>
        public float VoteDefaultTimeoutSeconds { get; set; } = 8f;

        /// <summary>Timeout for time-control votes (pause / play / speed).
        /// Short by design — quick decisions, low friction.</summary>
        public float TimeControlVoteTimeoutSeconds { get; set; } = 5f;

        /// <summary>Timeout for menu-pause votes when
        /// <see cref="VoteOnMenuPause"/> is true.</summary>
        public float MenuPauseVoteTimeoutSeconds { get; set; } = 8f;

        /// <summary>Timeout for settlement-entry votes. Longer by default
        /// because the consequences are bigger.</summary>
        public float SettlementEnterVoteTimeoutSeconds { get; set; } = 15f;

        /// <summary>When true, opening a world-freezing screen (encyclopedia,
        /// inventory, character window) requests a vote; if accepted, both
        /// peers pause. When false (default) the freeze is unconditionally
        /// suppressed — every player browses menus without affecting the
        /// shared world. Auto-unpause on menu close is not yet wired (see
        /// ROADMAP M0.7 notes); after a yes-vote, the world stays paused
        /// until someone explicitly resumes via spacebar.</summary>
        public bool VoteOnMenuPause { get; set; } = false;

        public static CoopConfig Default => new CoopConfig();
    }
}
