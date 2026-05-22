namespace Bannerlords.Coop.Modes
{
    public enum CoopModeKind : byte
    {
        /// <summary>Second player is a hero inside the host's party.</summary>
        CompanionInArmy = 0,
        /// <summary>Second player has their own clan and party.</summary>
        IndependentWarband = 1,
    }
}
