using Bannerlords.Coop.Network.Packets;
using Bannerlords.Coop.Network.Session;
using Bannerlords.Coop.Util;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace Bannerlords.Coop.Patches
{
    /// <summary>
    /// Routes local <c>Campaign.TimeControlMode</c> changes through the
    /// vote system. When the player presses spacebar / changes speed in a
    /// live coop session:
    ///   1. The setter Prefix intercepts and requests a vote.
    ///   2. If accepted, the initiator's onPassed callback sets the field
    ///      with <see cref="_applyingRemote"/>=true so the Prefix lets the
    ///      next call through.
    ///   3. The other peer applies the same change via the VoteResult
    ///      packet → <see cref="ApplyRemote"/> (also guarded by
    ///      <see cref="_applyingRemote"/>).
    /// Outside of a live session the Prefix returns true and original
    /// vanilla behavior runs.
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
                Log.Debug("TimeControlPatch", $"applied {mode}");
            }
            finally
            {
                _applyingRemote = false;
            }
        }

        [HarmonyPatch(typeof(Campaign))]
        [HarmonyPatch("TimeControlMode", MethodType.Setter)]
        public static class SetterPrefix
        {
            // ReSharper disable once UnusedMember.Global
            public static bool Prefix(CampaignTimeControlMode value)
            {
                if (_applyingRemote) return true; // re-entrance from ApplyRemote / onPassed

                var session = CoopSession.Instance;
                if (session == null || session.State != SessionState.Live) return true;

                var wireMode = ToWire(value);
                var captured = value;
                session.VoteManager.RequestSetTimeControl(
                    mode: wireMode,
                    reason: $"set time to {captured}",
                    onPassed: () =>
                    {
                        var c = Campaign.Current;
                        if (c == null) return;
                        try
                        {
                            _applyingRemote = true;
                            c.TimeControlMode = captured;
                        }
                        finally { _applyingRemote = false; }
                        Log.Debug("TimeControlPatch", $"applied local {captured} after vote pass");
                    },
                    onFailed: () =>
                    {
                        Log.Info("TimeControlPatch", $"vote rejected; not applying {captured}");
                    });
                return false; // skip original — we'll reapply via onPassed if vote passes
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
