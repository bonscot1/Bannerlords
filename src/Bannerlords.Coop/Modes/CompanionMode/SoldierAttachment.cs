using Bannerlords.Coop.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Bannerlords.Coop.Modes.CompanionMode
{
    /// <summary>
    /// M0 attachment: when a peer joins, insert a single tier-2 troop into
    /// the host's MainParty roster as a placeholder representing the client.
    ///
    /// Real Hero provisioning (skills, perks, equipment, persistence) lands
    /// in M2 — this exists so M0 has something visible to point at when
    /// verifying that the handshake actually reached gameplay.
    /// </summary>
    public static class SoldierAttachment
    {
        // Vanilla recruit ID is stable across 1.2.x; if a culture-aware troop
        // is desired later we can pick based on MainParty.Owner.Culture.
        private const string PlaceholderTroopId = "imperial_recruit";

        public static void AttachAsSoldier(string clientDisplayName)
        {
            if (Campaign.Current == null)
            {
                Log.Warn("SoldierAttachment", "no active campaign — skipping attach");
                return;
            }

            var mainParty = MobileParty.MainParty;
            if (mainParty == null || mainParty.MemberRoster == null)
            {
                Log.Warn("SoldierAttachment", "no main party — skipping attach");
                return;
            }

            var troop = MBObjectManager.Instance.GetObject<CharacterObject>(PlaceholderTroopId);
            if (troop == null)
            {
                Log.Error("SoldierAttachment",
                    $"placeholder troop '{PlaceholderTroopId}' not found in object manager");
                return;
            }

            mainParty.MemberRoster.AddToCounts(troop, 1);
            Log.Info("SoldierAttachment",
                $"added 1x {troop.StringId} representing '{clientDisplayName}' to MainParty");
        }
    }
}
