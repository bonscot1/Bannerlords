using System;
using System.IO;
using Bannerlords.Coop.Network.Session;
using Bannerlords.Coop.Patches;
using Bannerlords.Coop.UI;
using Bannerlords.Coop.Util;
using Bannerlords.Coop.Network.Packets;
using Bannerlords.Coop.Network.Voting;
using TaleWorlds.MountAndBlade;

namespace Bannerlords.Coop
{
    /// <summary>
    /// Mount point that Bannerlord's module loader instantiates (referenced
    /// from <c>SubModule.xml</c>). All initialization happens here, in the
    /// order Bannerlord drives it:
    ///   <list type="number">
    ///     <item>OnSubModuleLoad — once, very early</item>
    ///     <item>OnBeforeInitialModuleScreenSetAsRoot — once, after main menu is ready</item>
    ///     <item>OnGameInitializationFinished — every time a game is started/loaded</item>
    ///     <item>OnApplicationTick — every frame</item>
    ///     <item>OnGameEnd / OnSubModuleUnloaded — teardown</item>
    ///   </list>
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        private CoopSession _session;
        private bool _menuInstalled;

        // ---------- load ----------

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            try
            {
                var logDir = Path.Combine(GetModuleDir(), "Logs");
                Log.Init(logDir);
                Log.Info("SubModule", "OnSubModuleLoad");

                RegisterPackets();
                HarmonyBootstrap.Apply();

                var config = CoopConfig.Default;
                _session = new CoopSession(config);
            }
            catch (Exception ex)
            {
                Log.Error("SubModule", ex);
            }
        }

        // ---------- main menu ----------

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            if (_menuInstalled) return;
            try
            {
                MainMenuHooks.Install(_session);
                _menuInstalled = true;
            }
            catch (Exception ex)
            {
                Log.Error("SubModule", ex);
            }
        }

        // ---------- per-frame ----------

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            try { _session?.Tick(dt); }
            catch (Exception ex) { Log.Error("SubModule.Tick", ex); }
        }

        // ---------- teardown ----------

        public override void OnGameEnd(TaleWorlds.Core.Game game)
        {
            base.OnGameEnd(game);
            Log.Info("SubModule", "OnGameEnd");
            try { _session?.Disconnect(DisconnectReason.UserQuit, "OnGameEnd"); }
            catch (Exception ex) { Log.Error("SubModule", ex); }
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            Log.Info("SubModule", "OnSubModuleUnloaded");
            try
            {
                _session?.Disconnect(DisconnectReason.UserQuit, "module unloaded");
                HarmonyBootstrap.Unapply();
            }
            catch (Exception ex) { Log.Error("SubModule", ex); }
            finally { Log.Shutdown(); }
        }

        // ---------- helpers ----------

        private static void RegisterPackets()
        {
            HandshakePacket.RegisterFactory();
            WelcomePacket.RegisterFactory();
            DisconnectPacket.RegisterFactory();
            HeartbeatPacket.RegisterFactory();
            TimeControlPacket.RegisterFactory();
            VoteRequestPacket.RegisterFactory();
            VoteResponsePacket.RegisterFactory();
            VoteResultPacket.RegisterFactory();
        }

        /// <summary>
        /// Best-effort: <c>Modules/Bannerlords.Coop</c> next to this assembly.
        /// </summary>
        private static string GetModuleDir()
        {
            try
            {
                var asmPath = typeof(SubModule).Assembly.Location;
                if (!string.IsNullOrEmpty(asmPath))
                {
                    // .../Modules/<Mod>/bin/Win64_Shipping_Client/Bannerlords.Coop.dll
                    var bin = Path.GetDirectoryName(asmPath);
                    var binParent = Path.GetDirectoryName(bin);
                    var moduleRoot = Path.GetDirectoryName(binParent);
                    if (!string.IsNullOrEmpty(moduleRoot)) return moduleRoot;
                }
            }
            catch { /* fall through */ }
            return Path.Combine(Path.GetTempPath(), "Bannerlords.Coop");
        }
    }
}
