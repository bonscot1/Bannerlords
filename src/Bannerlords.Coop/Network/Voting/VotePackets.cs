using Bannerlords.Coop.Network.Packet;

namespace Bannerlords.Coop.Network.Voting
{
    /// <summary>
    /// Initiator → all peers. Asks each peer to vote on the proposed action.
    /// </summary>
    public sealed class VoteRequestPacket : IPacket
    {
        public PacketId Id => PacketId.VoteRequest;

        public ushort VoteId;
        public byte Action;          // VoteAction
        public byte ActionDetail;    // action-specific payload
        public float TimeoutSeconds;
        public ulong InitiatorPeerId;
        public string Reason;        // human-readable, shown in UI

        public static void RegisterFactory() =>
            PacketRegistry.Register(PacketId.VoteRequest, () => new VoteRequestPacket());

        public void Write(PacketBuffer buf)
        {
            buf.WriteUShort(VoteId);
            buf.WriteByte(Action);
            buf.WriteByte(ActionDetail);
            buf.WriteFloat(TimeoutSeconds);
            buf.WriteULong(InitiatorPeerId);
            buf.WriteString(Reason ?? string.Empty);
        }

        public void Read(PacketBuffer buf)
        {
            VoteId = buf.ReadUShort();
            Action = buf.ReadByte();
            ActionDetail = buf.ReadByte();
            TimeoutSeconds = buf.ReadFloat();
            InitiatorPeerId = buf.ReadULong();
            Reason = buf.ReadString();
        }
    }

    /// <summary>
    /// Each non-initiator peer → initiator. Carries the local accept/reject.
    /// </summary>
    public sealed class VoteResponsePacket : IPacket
    {
        public PacketId Id => PacketId.VoteResponse;

        public ushort VoteId;
        public bool Accept;
        public ulong ResponderPeerId;

        public static void RegisterFactory() =>
            PacketRegistry.Register(PacketId.VoteResponse, () => new VoteResponsePacket());

        public void Write(PacketBuffer buf)
        {
            buf.WriteUShort(VoteId);
            buf.WriteBool(Accept);
            buf.WriteULong(ResponderPeerId);
        }

        public void Read(PacketBuffer buf)
        {
            VoteId = buf.ReadUShort();
            Accept = buf.ReadBool();
            ResponderPeerId = buf.ReadULong();
        }
    }

    /// <summary>
    /// Initiator → all peers, broadcast after tallying responses. Tells every
    /// peer whether the vote passed and what action+detail to apply.
    /// </summary>
    public sealed class VoteResultPacket : IPacket
    {
        public PacketId Id => PacketId.VoteResult;

        public ushort VoteId;
        public bool Passed;
        public byte Action;
        public byte ActionDetail;

        public static void RegisterFactory() =>
            PacketRegistry.Register(PacketId.VoteResult, () => new VoteResultPacket());

        public void Write(PacketBuffer buf)
        {
            buf.WriteUShort(VoteId);
            buf.WriteBool(Passed);
            buf.WriteByte(Action);
            buf.WriteByte(ActionDetail);
        }

        public void Read(PacketBuffer buf)
        {
            VoteId = buf.ReadUShort();
            Passed = buf.ReadBool();
            Action = buf.ReadByte();
            ActionDetail = buf.ReadByte();
        }
    }
}
