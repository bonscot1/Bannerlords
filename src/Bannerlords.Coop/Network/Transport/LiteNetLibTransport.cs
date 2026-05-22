using System;
using Bannerlords.Coop.Util;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Bannerlords.Coop.Network.Transport
{
    /// <summary>
    /// LiteNetLib-backed UDP transport. M0 supports exactly one host plus one
    /// client; peer IDs are fixed (host=1, client=2). When we extend to
    /// multi-client in M2 the peer-id assignment will move into the host's
    /// connection-accept path; the rest of the session code already treats
    /// IDs as opaque ulongs.
    /// </summary>
    public sealed class LiteNetLibTransport : ITransport
    {
        public const ulong HostId = 1UL;
        public const ulong ClientId = 2UL;

        private readonly bool _isHost;
        private readonly int _listenPort;
        private readonly string _connectAddress;
        private readonly int _connectPort;
        private readonly string _connectionKey;

        private EventBasedNetListener _listener;
        private NetManager _net;
        private NetPeer _peer;

        public ulong LocalId => _isHost ? HostId : ClientId;
        public bool IsRunning { get; private set; }

        public event Action<ulong, byte[]> OnMessage;
        public event Action<ulong> OnPeerConnected;
        public event Action<ulong> OnPeerDisconnected;

        public LiteNetLibTransport(
            bool isHost,
            int listenPort,
            string connectAddress,
            int connectPort,
            string connectionKey)
        {
            _isHost = isHost;
            _listenPort = listenPort;
            _connectAddress = connectAddress ?? string.Empty;
            _connectPort = connectPort;
            _connectionKey = connectionKey ?? string.Empty;
        }

        public void Start()
        {
            if (IsRunning) return;
            try
            {
                _listener = new EventBasedNetListener();
                _net = new NetManager(_listener)
                {
                    AutoRecycle = true,
                    UnconnectedMessagesEnabled = false,
                    UpdateTime = 15,
                };

                _listener.NetworkReceiveEvent += HandleReceive;
                _listener.PeerConnectedEvent += HandlePeerConnected;
                _listener.PeerDisconnectedEvent += HandlePeerDisconnected;

                if (_isHost)
                {
                    _listener.ConnectionRequestEvent += req => req.AcceptIfKey(_connectionKey);
                    if (!_net.Start(_listenPort))
                    {
                        Log.Error("LiteNetLibTransport", $"failed to bind port {_listenPort}");
                        return;
                    }
                    Log.Info("LiteNetLibTransport", $"hosting on port {_listenPort}");
                }
                else
                {
                    if (!_net.Start())
                    {
                        Log.Error("LiteNetLibTransport", "client NetManager failed to start");
                        return;
                    }
                    Log.Info("LiteNetLibTransport",
                        $"connecting to {_connectAddress}:{_connectPort}");
                    _net.Connect(_connectAddress, _connectPort, _connectionKey);
                }

                IsRunning = true;
            }
            catch (Exception ex)
            {
                Log.Error("LiteNetLibTransport", ex);
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            try { _net?.Stop(); } catch (Exception ex) { Log.Error("LiteNetLibTransport", ex); }
            _net = null;
            _listener = null;
            _peer = null;
        }

        public void Send(ulong peer, byte[] data, SendReliability reliability)
        {
            if (!IsRunning || _peer == null) return;
            if (_peer.ConnectionState != ConnectionState.Connected) return;
            if (peer != RemoteId)
            {
                Log.Warn("LiteNetLibTransport", $"send to unknown peer {peer}");
                return;
            }
            try
            {
                var dm = reliability == SendReliability.Reliable
                    ? DeliveryMethod.ReliableOrdered
                    : DeliveryMethod.Unreliable;
                _peer.Send(data, dm);
            }
            catch (Exception ex)
            {
                Log.Error("LiteNetLibTransport", ex);
            }
        }

        public void Poll()
        {
            if (!IsRunning) return;
            try { _net?.PollEvents(); }
            catch (Exception ex) { Log.Error("LiteNetLibTransport", ex); }
        }

        public void Dispose() => Stop();

        // ---------- internals ----------

        private ulong RemoteId => _isHost ? ClientId : HostId;

        private void HandleReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            try
            {
                var data = reader.GetRemainingBytes();
                OnMessage?.Invoke(RemoteId, data);
            }
            catch (Exception ex) { Log.Error("LiteNetLibTransport", ex); }
        }

        private void HandlePeerConnected(NetPeer peer)
        {
            _peer = peer;
            Log.Info("LiteNetLibTransport", $"peer connected: {peer.EndPoint}");
            OnPeerConnected?.Invoke(RemoteId);
        }

        private void HandlePeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            Log.Info("LiteNetLibTransport",
                $"peer disconnected: {peer.EndPoint} reason={info.Reason}");
            _peer = null;
            OnPeerDisconnected?.Invoke(RemoteId);
        }
    }
}
