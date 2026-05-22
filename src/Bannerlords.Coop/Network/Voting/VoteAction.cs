namespace Bannerlords.Coop.Network.Voting
{
    /// <summary>
    /// Vote-able actions. Wire-stable; never renumber, only append.
    /// </summary>
    public enum VoteAction : byte
    {
        /// <summary>
        /// Change <c>Campaign.TimeControlMode</c>. The vote's
        /// <c>ActionDetail</c> byte is a <see cref="Packets.WireTimeControl"/>
        /// value. Demonstrated by the M0 debug hotkey
        /// (<see cref="VoteManager.RequestSetTimeControl"/>); the proper
        /// integration with the setter patch and menu-pause path is M0.7.
        /// </summary>
        SetTimeControl = 0,

        /// <summary>
        /// Reserved: enter a settlement. <c>ActionDetail</c> = settlement
        /// id hash. Wired in M3 once settlement-entry sync lands.
        /// </summary>
        EnterSettlement = 1,

        /// <summary>
        /// Reserved: world-freezing screen (encyclopedia, inventory, etc).
        /// M0 unconditionally suppresses these freezes
        /// (<see cref="Patches.GameStateManagerPatch"/>); voting on them is
        /// a M1 polish item.
        /// </summary>
        MenuPause = 2,
    }
}
