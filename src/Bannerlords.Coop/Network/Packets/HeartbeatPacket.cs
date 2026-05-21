using Bannerlords.Coop.Network.Packet;

namespace Bannerlords.Coop.Network.Packets
{
    /// <summary>
    /// Sent on a fixed interval (see <see cref="Util.CoopConfig.HeartbeatInterval"/>).
    /// Missing heartbeats over <see cref="Util.CoopConfig.HeartbeatMissTolerance"/>
    /// in a row marks the peer dead.
    /// </summary>
    public sealed class HeartbeatPacket : IPacket
    {
        public PacketId Id => PacketId.Heartbeat;

        public uint SequenceNumber;
        public float SenderUptimeSeconds;

        public static void RegisterFactory() =>
            PacketRegistry.Register(PacketId.Heartbeat, () => new HeartbeatPacket());

        public void Write(PacketBuffer buf)
        {
            buf.WriteUInt(SequenceNumber);
            buf.WriteFloat(SenderUptimeSeconds);
        }

        public void Read(PacketBuffer buf)
        {
            SequenceNumber = buf.ReadUInt();
            SenderUptimeSeconds = buf.ReadFloat();
        }
    }
}
