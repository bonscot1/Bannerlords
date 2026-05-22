namespace Bannerlords.Coop.Network.Packet
{
    public interface IPacket
    {
        PacketId Id { get; }
        void Write(PacketBuffer buf);
        void Read(PacketBuffer buf);
    }
}
