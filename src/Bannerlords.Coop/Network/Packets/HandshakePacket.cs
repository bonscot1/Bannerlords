using Bannerlords.Coop.Network.Packet;
using Bannerlords.Coop.Util;

namespace Bannerlords.Coop.Network.Packets
{
    /// <summary>
    /// Client → host on first connection. Asserts the client's protocol
    /// version and identifies the client.
    /// </summary>
    public sealed class HandshakePacket : IPacket
    {
        public PacketId Id => PacketId.Handshake;

        public ushort ProtocolVersion;
        public ulong SteamId;
        public string DisplayName;
        public string ModVersion;

        public static void RegisterFactory() =>
            PacketRegistry.Register(PacketId.Handshake, () => new HandshakePacket());

        public void Write(PacketBuffer buf)
        {
            buf.WriteUShort(ProtocolVersion);
            buf.WriteULong(SteamId);
            buf.WriteString(DisplayName ?? string.Empty);
            buf.WriteString(ModVersion ?? string.Empty);
        }

        public void Read(PacketBuffer buf)
        {
            ProtocolVersion = buf.ReadUShort();
            SteamId = buf.ReadULong();
            DisplayName = buf.ReadString();
            ModVersion = buf.ReadString();
        }

        public static HandshakePacket Local(ulong steamId, string displayName) => new HandshakePacket
        {
            ProtocolVersion = CoopConfig.ProtocolVersion,
            SteamId = steamId,
            DisplayName = displayName,
            ModVersion = "0.0.1",
        };
    }
}
