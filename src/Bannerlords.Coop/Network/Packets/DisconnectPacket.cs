using Bannerlords.Coop.Network.Packet;

namespace Bannerlords.Coop.Network.Packets
{
    public enum DisconnectReason : byte
    {
        Unknown = 0,
        UserQuit = 1,
        ProtocolMismatch = 2,
        HostShutdown = 3,
        Kicked = 4,
        Timeout = 5,
    }

    public sealed class DisconnectPacket : IPacket
    {
        public PacketId Id => PacketId.Disconnect;

        public DisconnectReason Reason;
        public string Detail;

        public static void RegisterFactory() =>
            PacketRegistry.Register(PacketId.Disconnect, () => new DisconnectPacket());

        public void Write(PacketBuffer buf)
        {
            buf.WriteByte((byte)Reason);
            buf.WriteString(Detail ?? string.Empty);
        }

        public void Read(PacketBuffer buf)
        {
            Reason = (DisconnectReason)buf.ReadByte();
            Detail = buf.ReadString();
        }
    }
}
