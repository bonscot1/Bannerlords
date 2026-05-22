using System.Collections.Generic;
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
    /// If the campaign isn't loaded yet at join time (host pressed F8 from
    /// the main menu before loading a save) the name is queued and the
    /// attach runs on the first tick where <c>Campaign.Current</c> exists.
    ///
    /// Real Hero provisioning (skills, perks, equipment, persistence) lands
    /// in M2 — this placeholder exists so M0 has something visible to
    /// confirm the handshake reached gameplay.
    /// </summary>
    public static class SoldierAttachment
    {
        // Vanilla recruit ID is stable across 1.2.x; if a culture-aware troop
        // is desired later we can pick based on MainParty.Owner.Culture.
        private const string PlaceholderTroopId = "imperial_recruit";

        private static readonly Queue<string> _pending = new Queue<string>();

        public static void AttachAsSoldier(string clientDisplayName)
        {
            if (!TryAttachNow(clientDisplayName))
            {
                Log.Info("SoldierAttachment",
                    $"campaign not ready; queued attach for '{clientDisplayName}'");
                _pending.Enqueue(clientDisplayName);
            }
        }

        /// <summary>Drain queued attaches. Cheap when the queue is empty; safe
        /// to call every tick.</summary>
        public static void TryFlushPending()
        {
            while (_pending.Count > 0)
            {
                var name = _pending.Peek();
                if (!TryAttachNow(name)) return; // still no campaign — try again later
                _pending.Dequeue();
            }
        }

        private static bool TryAttachNow(string clientDisplayName)
        {
            if (Campaign.Current == null) return false;
            var mainParty = MobileParty.MainParty;
            if (mainParty == null || mainParty.MemberRoster == null) return false;

            var troop = MBObjectManager.Instance.GetObject<CharacterObject>(PlaceholderTroopId);
            if (troop == null)
            {
                Log.Error("SoldierAttachment",
                    $"placeholder troop '{PlaceholderTroopId}' not found in object manager");
                return true; // don't keep retrying for a missing object
            }

            mainParty.MemberRoster.AddToCounts(troop, 1);
            Log.Info("SoldierAttachment",
                $"added 1x {troop.StringId} representing '{clientDisplayName}' to MainParty");
            return true;
        }
    }
}
