using System;
using System.Collections.Generic;
using Bannerlords.Coop.Util;

namespace Bannerlords.Coop.Network.Packet
{
    /// <summary>
    /// Maps <see cref="PacketId"/> -> factory. Every concrete packet must
    /// register itself here (typically from a static ctor). Registration is
    /// expected at module load; lookups happen on the dispatch hot path.
    /// </summary>
    public static class PacketRegistry
    {
        private static readonly Dictionary<PacketId, Func<IPacket>> _factories
            = new Dictionary<PacketId, Func<IPacket>>();

        public static void Register(PacketId id, Func<IPacket> factory)
        {
            if (_factories.ContainsKey(id))
            {
                Log.Warn("PacketRegistry", $"PacketId {id} re-registered, overwriting");
            }
            _factories[id] = factory;
        }

        public static IPacket Create(PacketId id)
        {
            if (!_factories.TryGetValue(id, out var factory))
                throw new InvalidOperationException(
                    $"No factory registered for PacketId {id} (0x{(byte)id:X2})");
            return factory();
        }

        public static bool IsRegistered(PacketId id) => _factories.ContainsKey(id);
    }
}
