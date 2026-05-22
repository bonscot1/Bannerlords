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

        /// <summary>If false, every action that would normally vote (pause,
        /// time-speed, settlement entry, menu-pause) goes through
        /// unilaterally. Use during dev or when testing alone.</summary>
        public bool VotingEnabled { get; set; } = true;

        /// <summary>Default seconds non-initiators have to respond to a vote
        /// before the configured default-result applies. Per-action overrides
        /// live next to each vote site.</summary>
        public float VoteDefaultTimeoutSeconds { get; set; } = 8f;

        public static CoopConfig Default => new CoopConfig();
    }
}
