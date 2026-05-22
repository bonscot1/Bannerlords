using Bannerlords.Coop.Network.Packet;

namespace Bannerlords.Coop.Network.Packets
{
    /// <summary>
    /// Mirrors TaleWorlds.CampaignSystem.CampaignTimeControlMode. Duplicated as
    /// a byte enum here so the wire format isn't tied to game-DLL versions —
    /// if TaleWorlds renumbers their enum we only have to update the converter
    /// in TimeControlPatch, not the packet layout.
    /// </summary>
    public enum WireTimeControl : byte
    {
        Stop = 0,
        UnstoppablePlay = 1,
        StoppableFastForward = 2,
        StoppablePlay = 3,
        UnstoppableFastForward = 4,
        FastForwardStop = 5,
    }

    /// <summary>
    /// Broadcast whenever the host changes <c>Campaign.TimeControlMode</c>.
    /// Clients apply it locally with the loop-back guard set so the postfix
    /// patch doesn't echo it back.
    /// </summary>
    public sealed class TimeControlPacket : IPacket
    {
        public PacketId Id => PacketId.TimeControl;

        public WireTimeControl Mode;
        /// <summary>Monotonic counter so out-of-order packets can be ignored.</summary>
        public uint Sequence;

        public static void RegisterFactory() =>
            PacketRegistry.Register(PacketId.TimeControl, () => new TimeControlPacket());

        public void Write(PacketBuffer buf)
        {
            buf.WriteByte((byte)Mode);
            buf.WriteUInt(Sequence);
        }

        public void Read(PacketBuffer buf)
        {
            Mode = (WireTimeControl)buf.ReadByte();
            Sequence = buf.ReadUInt();
        }
    }
}
