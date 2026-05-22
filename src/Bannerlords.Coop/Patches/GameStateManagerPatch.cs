using Bannerlords.Coop.Network.Packets;
using Bannerlords.Coop.Network.Session;
using Bannerlords.Coop.Util;
using HarmonyLib;
using TaleWorlds.Core;

namespace Bannerlords.Coop.Patches
{
    /// <summary>
    /// Controls what happens when Bannerlord wants to freeze the world for a
    /// menu (encyclopedia, inventory, character window). In single-player
    /// the original behavior runs unchanged.
    ///
    /// In a live coop session, two modes:
    ///   <list type="bullet">
    ///     <item><b>Suppress (default):</b> the freeze is unconditionally
    ///       skipped. Every player browses menus without affecting the
    ///       shared world clock.</item>
    ///     <item><b>Vote (<see cref="CoopConfig.VoteOnMenuPause"/>=true):</b>
    ///       the freeze is suppressed locally and a coop vote is fired. If
    ///       it passes, both peers pause via the time-control path. Note:
    ///       auto-unpause on menu close is not yet wired — after a yes-vote
    ///       the world stays paused until someone resumes via spacebar.</item>
    ///   </list>
    /// </summary>
    [HarmonyPatch(typeof(GameStateManager),
        nameof(GameStateManager.RegisterActiveStateDisableRequest))]
    public static class GameStateManagerPatch
    {
        // ReSharper disable once UnusedMember.Global
        public static bool Prefix()
        {
            var session = CoopSession.Instance;
            if (session == null || session.State != SessionState.Live) return true; // single-player

            if (session.Config.VoteOnMenuPause)
            {
                Log.Debug("GameStateManagerPatch", "menu-pause: requesting vote");
                session.VoteManager.RequestMenuPause(
                    reason: "pause the world for a menu",
                    onPassed: () =>
                    {
                        // The MenuPause action in VoteManager.ApplyAction
                        // sets TimeControlMode=Stop on the initiator side
                        // already; nothing extra to do here.
                        Log.Info("GameStateManagerPatch", "menu-pause vote passed");
                    },
                    onFailed: () =>
                    {
                        Log.Info("GameStateManagerPatch", "menu-pause vote failed; world keeps running");
                    });
            }
            else
            {
                Log.Debug("GameStateManagerPatch", "menu-pause suppressed (vote disabled)");
            }

            return false; // suppress the original — world keeps ticking until the vote (if any) settles
        }
    }
}
