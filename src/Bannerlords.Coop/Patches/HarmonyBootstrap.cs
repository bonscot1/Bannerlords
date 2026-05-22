using System;
using Bannerlords.Coop.Util;
using HarmonyLib;

namespace Bannerlords.Coop.Patches
{
    public static class HarmonyBootstrap
    {
        public const string HarmonyId = "Bannerlords.Coop";
        private static Harmony _harmony;

        public static void Apply()
        {
            if (_harmony != null) return;
            try
            {
                _harmony = new Harmony(HarmonyId);
                _harmony.PatchAll(typeof(HarmonyBootstrap).Assembly);
                Log.Info("Harmony", "patches applied");
            }
            catch (Exception ex)
            {
                Log.Error("Harmony", ex);
                _harmony = null;
            }
        }

        public static void Unapply()
        {
            if (_harmony == null) return;
            try
            {
                _harmony.UnpatchAll(HarmonyId);
                Log.Info("Harmony", "patches removed");
            }
            catch (Exception ex)
            {
                Log.Error("Harmony", ex);
            }
            finally
            {
                _harmony = null;
            }
        }
    }
}
