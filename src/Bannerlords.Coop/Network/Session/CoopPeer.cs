using System;

namespace Bannerlords.Coop.Network.Session
{
    /// <summary>
    /// Identifies a remote participant in a coop session. Wraps a Steam ID
    /// when the transport is Steam; for the loopback transport
    /// <see cref="SteamId"/> is a synthetic value.
    /// </summary>
    public sealed class CoopPeer : IEquatable<CoopPeer>
    {
        public ulong SteamId { get; }
        public string DisplayName { get; internal set; }
        public bool IsHost { get; internal set; }
        public bool HandshakeComplete { get; internal set; }
        public float LastHeartbeatSeconds { get; internal set; }

        public CoopPeer(ulong steamId, string displayName, bool isHost)
        {
            SteamId = steamId;
            DisplayName = displayName ?? "<unknown>";
            IsHost = isHost;
        }

        public bool Equals(CoopPeer other) => other != null && SteamId == other.SteamId;
        public override bool Equals(object obj) => Equals(obj as CoopPeer);
        public override int GetHashCode() => SteamId.GetHashCode();
        public override string ToString() => $"{DisplayName}({SteamId})";
    }
}
