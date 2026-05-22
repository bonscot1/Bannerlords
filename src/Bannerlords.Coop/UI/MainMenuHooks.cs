using Bannerlords.Coop.Network.Session;
using Bannerlords.Coop.Util;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Bannerlords.Coop.UI
{
    /// <summary>
    /// Adds "Host coop game" / "Join coop game" entries to the initial state
    /// menu. M0 uses IP-based join: host listens on
    /// <see cref="CoopConfig.ListenPort"/>; client connects to
    /// <see cref="CoopConfig.JoinAddress"/>:<see cref="CoopConfig.JoinPort"/>.
    /// A proper address-input dialog comes with the M1 lobby UI; until then
    /// the join target is configured in CoopConfig.
    /// </summary>
    public static class MainMenuHooks
    {
        private static CoopSession _session;

        public static void Install(CoopSession session)
        {
            _session = session;

            Module.CurrentModule.AddInitialStateOption(new InitialStateOption(
                "Bannerlords.Coop.Host",
                new TextObject("{=BLC_HOST}Host coop game"),
                9990,
                HostCoopGame,
                () => (false, null)));

            Module.CurrentModule.AddInitialStateOption(new InitialStateOption(
                "Bannerlords.Coop.Join",
                new TextObject("{=BLC_JOIN}Join coop game"),
                9991,
                JoinCoopGame,
                () => (false, null)));

            Log.Info("MainMenuHooks", "menu entries installed");
        }

        // ---------- option handlers ----------

        private static void HostCoopGame()
        {
            Log.Info("MainMenuHooks", "host coop game selected");
            if (_session == null)
            {
                Log.Error("MainMenuHooks", "session unavailable");
                return;
            }
            if (!_session.StartHost("Host")) return;
            InformationManager.DisplayMessage(new InformationMessage(
                $"Coop: hosting on port {_session.Config.ListenPort}. Share your IP with your peer."));
        }

        private static void JoinCoopGame()
        {
            Log.Info("MainMenuHooks", "join coop game selected");
            if (_session == null)
            {
                Log.Error("MainMenuHooks", "session unavailable");
                return;
            }
            if (!_session.JoinHost("Client")) return;
            InformationManager.DisplayMessage(new InformationMessage(
                $"Coop: connecting to {_session.Config.JoinAddress}:{_session.Config.JoinPort}..."));
        }
    }
}
