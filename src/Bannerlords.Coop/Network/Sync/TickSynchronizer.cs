using Bannerlords.Coop.Util;

namespace Bannerlords.Coop.Network.Sync
{
    /// <summary>
    /// Fixed-rate tick clock layered on top of the variable game frame. M0
    /// uses it only to pace heartbeats; M1+ uses it to schedule state-delta
    /// emission.
    /// </summary>
    public sealed class TickSynchronizer
    {
        private readonly float _tickIntervalSeconds;
        private float _accum;

        public uint TickNumber { get; private set; }
        public float LastTickAtUptime { get; private set; }

        public TickSynchronizer(CoopConfig config)
        {
            _tickIntervalSeconds = 1f / config.NetworkTickRate;
        }

        /// <summary>Advance the clock by a frame delta. Returns true exactly
        /// once per tick interval, allowing the caller to gate per-tick work
        /// inside a per-frame call.</summary>
        public bool Advance(float dt, float uptimeSeconds)
        {
            _accum += dt;
            if (_accum < _tickIntervalSeconds) return false;
            _accum -= _tickIntervalSeconds;
            // If we fell badly behind (alt-tab, debugger pause), don't burn a
            // burst of ticks catching up — drop accumulated remainder.
            if (_accum > _tickIntervalSeconds * 4f) _accum = 0f;

            TickNumber++;
            LastTickAtUptime = uptimeSeconds;
            return true;
        }
    }
}
