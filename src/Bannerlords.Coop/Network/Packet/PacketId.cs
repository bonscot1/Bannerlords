namespace Bannerlords.Coop.Network.Packet
{
    /// <summary>
    /// Wire-level packet identifiers. ORDER MATTERS — these values are
    /// serialized as <c>byte</c> over the wire. Never reuse a value, never
    /// renumber an existing entry; only append. Bump
    /// <see cref="Util.CoopConfig.ProtocolVersion"/> when removing or
    /// repurposing.
    /// </summary>
    public enum PacketId : byte
    {
        // 0x00–0x0F: handshake / session lifecycle
        Handshake     = 0x01,
        Welcome       = 0x02,
        Disconnect    = 0x03,
        Heartbeat     = 0x04,

        // 0x10–0x1F: world / time control
        TimeControl   = 0x10,
        GameTimeSync  = 0x11,

        // 0x20–0x2F: chat / misc
        Chat          = 0x20,

        // 0x30–0x3F: voting (initiator -> peers, then result broadcast)
        VoteRequest   = 0x30,
        VoteResponse  = 0x31,
        VoteResult    = 0x32,

        // 0x40–0x7F: gameplay sync (mode-agnostic) — reserved
        // 0x80–0xBF: companion-mode-specific  — reserved
        // 0xC0–0xFE: warband-mode-specific    — reserved
    }
}
