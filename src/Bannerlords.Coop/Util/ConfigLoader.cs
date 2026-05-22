using System;
using System.IO;
using Newtonsoft.Json;

namespace Bannerlords.Coop.Util
{
    /// <summary>
    /// Loads <see cref="CoopConfig"/> from <c>coopconfig.json</c> next to the
    /// module directory. Falls back to defaults when the file is absent or
    /// unreadable so the mod always boots — the load error is logged.
    /// </summary>
    public static class ConfigLoader
    {
        public const string FileName = "coopconfig.json";

        public static CoopConfig LoadOrDefault(string moduleDir)
        {
            var path = Path.Combine(moduleDir ?? string.Empty, FileName);
            if (!File.Exists(path))
            {
                Log.Info("ConfigLoader", $"no {FileName} at {path}; using built-in defaults");
                return CoopConfig.Default;
            }
            try
            {
                var json = File.ReadAllText(path);
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                };
                var cfg = JsonConvert.DeserializeObject<CoopConfig>(json, settings) ?? CoopConfig.Default;
                Log.Info("ConfigLoader",
                    $"loaded {FileName}: Listen={cfg.ListenPort} Join={cfg.JoinAddress}:{cfg.JoinPort} Loopback={cfg.UseLoopbackTransport}");
                return cfg;
            }
            catch (Exception ex)
            {
                Log.Error("ConfigLoader", ex);
                Log.Warn("ConfigLoader", $"failed to load {path}; using built-in defaults");
                return CoopConfig.Default;
            }
        }
    }
}
