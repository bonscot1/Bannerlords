using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Bannerlords.Coop.Util;

namespace Bannerlords.Coop.Network.Transport
{
    /// <summary>
    /// In-process transport used by tests and by single-machine devloop. Two
    /// instances reference each other via <see cref="Pair"/> and shuttle
    /// payloads through a thread-safe queue that's drained on <see cref="Poll"/>.
    /// </summary>
    public sealed class LoopbackTransport : ITransport
    {
        private readonly ConcurrentQueue<(ulong from, byte[] data)> _inbox
            = new ConcurrentQueue<(ulong, byte[])>();
        private LoopbackTransport _peer;
        private readonly HashSet<ulong> _knownPeers = new HashSet<ulong>();

        public ulong LocalId { get; }
        public bool IsRunning { get; private set; }

        public event Action<ulong, byte[]> OnMessage;
        public event Action<ulong> OnPeerConnected;
        public event Action<ulong> OnPeerDisconnected;

        public LoopbackTransport(ulong localId) { LocalId = localId; }

        public static void Pair(LoopbackTransport a, LoopbackTransport b)
        {
            a._peer = b; b._peer = a;
        }

        public void Start()
        {
            IsRunning = true;
            Log.Info("LoopbackTransport", $"started as {LocalId}");
            if (_peer != null && _knownPeers.Add(_peer.LocalId))
                OnPeerConnected?.Invoke(_peer.LocalId);
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            if (_peer != null)
                OnPeerDisconnected?.Invoke(_peer.LocalId);
        }

        public void Send(ulong peer, byte[] data, SendReliability reliability)
        {
            if (!IsRunning || _peer == null) return;
            if (peer != _peer.LocalId)
            {
                Log.Warn("LoopbackTransport",
                    $"send to unknown peer {peer} (only paired peer is {_peer.LocalId})");
                return;
            }
            // Copy so caller can recycle.
            var copy = new byte[data.Length];
            Buffer.BlockCopy(data, 0, copy, 0, data.Length);
            _peer._inbox.Enqueue((LocalId, copy));
        }

        public void Poll()
        {
            if (!IsRunning) return;
            while (_inbox.TryDequeue(out var msg))
                OnMessage?.Invoke(msg.from, msg.data);
        }

        public void Dispose() => Stop();
    }
}
