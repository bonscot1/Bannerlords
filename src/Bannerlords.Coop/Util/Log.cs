using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Bannerlords.Coop.Util
{
    public enum LogLevel { Trace, Debug, Info, Warn, Error }

    public static class Log
    {
        private static readonly object _gate = new object();
        private static StreamWriter _writer;
        private static LogLevel _minLevel = LogLevel.Debug;
        private static string _path;

        public static string Path => _path;

        public static void Init(string moduleDataDir)
        {
            lock (_gate)
            {
                if (_writer != null) return;
                try
                {
                    Directory.CreateDirectory(moduleDataDir);
                    _path = System.IO.Path.Combine(
                        moduleDataDir,
                        $"bannerlords-coop-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                    _writer = new StreamWriter(
                        new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read),
                        new UTF8Encoding(false))
                    { AutoFlush = true };
                    Write(LogLevel.Info, "Log", $"Logger initialized at {_path}");
                }
                catch
                {
                    // Logging must never crash the game.
                    _writer = null;
                }
            }
        }

        public static void SetLevel(LogLevel level) => _minLevel = level;

        public static void Trace(string tag, string msg) => Write(LogLevel.Trace, tag, msg);
        public static void Debug(string tag, string msg) => Write(LogLevel.Debug, tag, msg);
        public static void Info(string tag, string msg) => Write(LogLevel.Info, tag, msg);
        public static void Warn(string tag, string msg) => Write(LogLevel.Warn, tag, msg);
        public static void Error(string tag, string msg) => Write(LogLevel.Error, tag, msg);
        public static void Error(string tag, Exception ex) =>
            Write(LogLevel.Error, tag, ex == null ? "<null exception>" : ex.ToString());

        private static void Write(LogLevel level, string tag, string msg)
        {
            if (level < _minLevel) return;
            var line = $"{DateTime.Now:HH:mm:ss.fff} [{level,-5}] [{tag}] [T{Thread.CurrentThread.ManagedThreadId}] {msg}";
            lock (_gate)
            {
                try { _writer?.WriteLine(line); } catch { /* never crash */ }
            }
        }

        public static void Shutdown()
        {
            lock (_gate)
            {
                try { _writer?.Flush(); _writer?.Dispose(); } catch { }
                _writer = null;
            }
        }
    }
}
