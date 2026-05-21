using Bannerlords.Coop.Modes;

namespace Bannerlords.Coop.Util
{
    /// <summary>
    /// Mutable runtime configuration for the coop session. Loaded from a json
    /// file (see <see cref="ConfigLoader"/>) but for now keeps defaults so
    /// other layers can compile and exercise themselves.
    /// </summary>
    public sealed class CoopConfig
    {
        public const ushort ProtocolVersion = 1;

        public CoopModeKind Mode { get; set; } = CoopModeKind.CompanionInArmy;

        /// <summary>Steam lobby max member count. Hard ceiling is 250 but we
        /// cap small while the protocol is alpha.</summary>
        public int MaxLobbyMembers { get; set; } = 4;

        /// <summary>Whether to allow the lobby to be friend-only invitable
        /// (true) or public (false).</summary>
        public bool FriendsOnly { get; set; } = true;

        /// <summary>How often (seconds) to send heartbeat packets.</summary>
        public float HeartbeatInterval { get; set; } = 2.0f;

        /// <summary>Heartbeat misses before we consider a peer dead.</summary>
        public int HeartbeatMissTolerance { get; set; } = 5;

        /// <summary>Logical ticks per real second. Campaign uses variable speed
        /// but our sync layer ticks at a fixed cadence.</summary>
        public int NetworkTickRate { get; set; } = 20;

        /// <summary>If true, transport falls back to in-process loopback
        /// (useful for unit tests and devloop without Steam running).
        /// Defaulted to true while the Steam wrapper choice is pending
        /// (see <c>SteamP2PTransport.cs</c>); flip back to false once
        /// the Steam transport is wired.</summary>
        public bool UseLoopbackTransport { get; set; } = true;

        public static CoopConfig Default => new CoopConfig();
    }
}
