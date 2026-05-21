using System;
using System.Collections.Generic;
using Bannerlords.Coop.Network.Session;
using Bannerlords.Coop.Util;

namespace Bannerlords.Coop.Network.Packet
{
    /// <summary>
    /// Routes deserialized packets to subscribed handlers. Handlers run on the
    /// thread that called <see cref="Dispatch"/>, which is the main game
    /// thread when invoked from <see cref="CoopSession.Tick"/>.
    /// </summary>
    public sealed class PacketDispatcher
    {
        public delegate void Handler<T>(CoopPeer from, T packet) where T : IPacket;

        private readonly Dictionary<PacketId, Action<CoopPeer, IPacket>> _handlers
            = new Dictionary<PacketId, Action<CoopPeer, IPacket>>();

        public void On<T>(PacketId id, Handler<T> handler) where T : IPacket
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            Action<CoopPeer, IPacket> wrapped = (peer, p) =>
            {
                if (p is T t) handler(peer, t);
                else Log.Warn("Dispatcher",
                    $"Handler for {id} got mismatched type {p?.GetType().Name}");
            };
            if (_handlers.ContainsKey(id))
                _handlers[id] += wrapped;
            else
                _handlers[id] = wrapped;
        }

        public void Dispatch(CoopPeer from, IPacket packet)
        {
            if (!_handlers.TryGetValue(packet.Id, out var h))
            {
                Log.Trace("Dispatcher", $"No handler for {packet.Id} from {from}");
                return;
            }
            try { h(from, packet); }
            catch (Exception ex) { Log.Error("Dispatcher", ex); }
        }

        public bool HasHandler(PacketId id) => _handlers.ContainsKey(id);
    }
}
