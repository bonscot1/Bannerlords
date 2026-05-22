using Bannerlords.Coop.Network.Packets;
using Bannerlords.Coop.Network.Session;
using Bannerlords.Coop.Network.Transport;
using Bannerlords.Coop.Util;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace Bannerlords.Coop.Patches
{
    /// <summary>
    /// Synchronizes campaign time control across all peers. Postfix on
    /// <c>Campaign.TimeControlMode</c> setter: when someone changes time
    /// locally we broadcast; when a remote change arrives we apply it with
    /// <see cref="_applyingRemote"/> set to suppress the rebroadcast.
    ///
    /// M0 limitation: this only catches changes that flow through the
    /// <c>TimeControlMode</c> setter. Menu-induced pauses (encyclopedia,
    /// inventory) may use a separate freeze flag; those will be hooked in M1
    /// once the map mirror lands.
    /// </summary>
    public static class TimeControlPatch
    {
        private static bool _applyingRemote;

        public static void ApplyRemote(WireTimeControl mode)
        {
            var campaign = Campaign.Current;
            if (campaign == null)
            {
                Log.Debug("TimeControlPatch", "no campaign; ignoring remote time control");
                return;
            }
            try
            {
                _applyingRemote = true;
                campaign.TimeControlMode = ToGame(mode);
                Log.Debug("TimeControlPatch", $"applied remote {mode}");
            }
            finally
            {
                _applyingRemote = false;
            }
        }

        [HarmonyPatch(typeof(Campaign))]
        [HarmonyPatch("TimeControlMode", MethodType.Setter)]
        public static class SetterPostfix
        {
            // ReSharper disable once UnusedMember.Global
            public static void Postfix(CampaignTimeControlMode value)
            {
                if (_applyingRemote) return;

                var session = CoopSession.Instance;
                if (session == null) return;
                if (session.State != SessionState.Live) return;

                session.Broadcast(new TimeControlPacket
                {
                    Mode = ToWire(value),
                    Sequence = session.NextTimeControlSequence(),
                }, SendReliability.Reliable);

                Log.Debug("TimeControlPatch", $"broadcast local change {value}");
            }
        }

        // ---------- enum conversion ----------

        private static WireTimeControl ToWire(CampaignTimeControlMode m)
        {
            switch (m)
            {
                case CampaignTimeControlMode.Stop: return WireTimeControl.Stop;
                case CampaignTimeControlMode.UnstoppablePlay: return WireTimeControl.UnstoppablePlay;
                case CampaignTimeControlMode.StoppableFastForward: return WireTimeControl.StoppableFastForward;
                case CampaignTimeControlMode.StoppablePlay: return WireTimeControl.StoppablePlay;
                case CampaignTimeControlMode.UnstoppableFastForward: return WireTimeControl.UnstoppableFastForward;
                case CampaignTimeControlMode.FastForwardStop: return WireTimeControl.FastForwardStop;
                default: return WireTimeControl.Stop;
            }
        }

        private static CampaignTimeControlMode ToGame(WireTimeControl m)
        {
            switch (m)
            {
                case WireTimeControl.Stop: return CampaignTimeControlMode.Stop;
                case WireTimeControl.UnstoppablePlay: return CampaignTimeControlMode.UnstoppablePlay;
                case WireTimeControl.StoppableFastForward: return CampaignTimeControlMode.StoppableFastForward;
                case WireTimeControl.StoppablePlay: return CampaignTimeControlMode.StoppablePlay;
                case WireTimeControl.UnstoppableFastForward: return CampaignTimeControlMode.UnstoppableFastForward;
                case WireTimeControl.FastForwardStop: return CampaignTimeControlMode.FastForwardStop;
                default: return CampaignTimeControlMode.Stop;
            }
        }
    }
}
