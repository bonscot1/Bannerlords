using Bannerlords.Coop.Network.Packet;

namespace Bannerlords.Coop.Network.Packets
{
    /// <summary>
    /// Host → client response to <see cref="HandshakePacket"/>. Carries the
    /// host's identity and confirms the protocol version was accepted.
    /// </summary>
    public sealed class WelcomePacket : IPacket
    {
        public PacketId Id => PacketId.Welcome;

        public ushort ProtocolVersion;
        public ulong HostSteamId;
        public string HostDisplayName;
        public bool Accepted;
        public string RejectReason; // empty unless Accepted == false

        public static void RegisterFactory() =>
            PacketRegistry.Register(PacketId.Welcome, () => new WelcomePacket());

        public void Write(PacketBuffer buf)
        {
            buf.WriteUShort(ProtocolVersion);
            buf.WriteULong(HostSteamId);
            buf.WriteString(HostDisplayName ?? string.Empty);
            buf.WriteBool(Accepted);
            buf.WriteString(RejectReason ?? string.Empty);
        }

        public void Read(PacketBuffer buf)
        {
            ProtocolVersion = buf.ReadUShort();
            HostSteamId = buf.ReadULong();
            HostDisplayName = buf.ReadString();
            Accepted = buf.ReadBool();
            RejectReason = buf.ReadString();
        }
    }
}
