using Bannerlords.Coop.Network.Session;
using Bannerlords.Coop.Util;
using HarmonyLib;
using TaleWorlds.Core;

namespace Bannerlords.Coop.Patches
{
    /// <summary>
    /// While a coop session is live, suppress menu-induced world freezes.
    /// Bannerlord routes encyclopedia / inventory / character-window opens
    /// through <c>GameStateManager.RegisterActiveStateDisableRequest</c>; an
    /// unpatched call pauses the campaign tick, which would desync the world
    /// across peers as soon as one player opens a menu.
    ///
    /// Approach matches BannerlordCoop's. Voting on whether to pause for a
    /// menu is the M0.7 follow-up (see <c>VoteManager</c> / <c>ROADMAP.md</c>);
    /// for M0 we just keep the world ticking regardless.
    /// </summary>
    [HarmonyPatch(typeof(GameStateManager),
        nameof(GameStateManager.RegisterActiveStateDisableRequest))]
    public static class GameStateManagerPatch
    {
        // ReSharper disable once UnusedMember.Global
        public static bool Prefix()
        {
            var session = CoopSession.Instance;
            if (session == null || session.State != SessionState.Live) return true; // single-player: original behavior
            Log.Debug("GameStateManagerPatch", "suppressing menu-pause during coop");
            return false; // skip original — world keeps ticking
        }
    }
}
