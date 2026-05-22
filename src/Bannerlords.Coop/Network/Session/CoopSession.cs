using System;
using System.Collections.Generic;
using Bannerlords.Coop.Modes;
using Bannerlords.Coop.Modes.CompanionMode;
using Bannerlords.Coop.Modes.WarbandMode;
using Bannerlords.Coop.Network.Packet;
using Bannerlords.Coop.Network.Packets;
using Bannerlords.Coop.Network.Sync;
using Bannerlords.Coop.Network.Transport;
using Bannerlords.Coop.Network.Voting;
using Bannerlords.Coop.Util;

namespace Bannerlords.Coop.Network.Session
{
    public enum SessionState
    {
        Idle,
        Hosting,         // host: waiting for peers (or with peers already in)
        Connecting,      // client: handshake sent, awaiting Welcome
        Live,            // both sides: handshake complete
        Disconnecting,
    }

    /// <summary>
    /// Orchestrator. One instance per game process, owned by
    /// <see cref="SubModule"/>. Owns the transport, the dispatcher, and the
    /// per-peer bookkeeping. Public surface is intentionally small;
    /// gameplay code reaches in via mode managers, not this class.
    /// </summary>
    public sealed class CoopSession
    {
        public static CoopSession Instance { get; private set; }

        private readonly CoopConfig _config;
        private readonly PacketDispatcher _dispatcher = new PacketDispatcher();
        private readonly Dictionary<ulong, CoopPeer> _peers = new Dictionary<ulong, CoopPeer>();
        private readonly TickSynchronizer _ticks;
        private readonly VoteManager _voteManager;

        private ITransport _transport;
        private ICoopMode _mode;
        private float _uptimeSeconds;
        private float _secondsSinceHeartbeat;
        private uint _heartbeatSeq;
        private uint _timeControlSeqOut;
        private uint _timeControlSeqIn;

        public SessionState State { get; private set; } = SessionState.Idle;
        public CoopRole Role { get; private set; } = CoopRole.None;
        public ulong LocalPeerId => _transport?.LocalId ?? 0;
        public string LocalDisplayName { get; private set; } = "<unknown>";
        public CoopModeKind ModeKind => _mode?.Kind ?? _config.Mode;
        public CoopConfig Config => _config;
        public VoteManager VoteManager => _voteManager;

        public IReadOnlyCollection<CoopPeer> Peers => _peers.Values;
        public uint NextTimeControlSequence() => unchecked(++_timeControlSeqOut);
        public uint LastInboundTimeControlSequence => _timeControlSeqIn;

        public CoopSession(CoopConfig config)
        {
            _config = config ?? CoopConfig.Default;
            _ticks = new TickSynchronizer(_config);
            _voteManager = new VoteManager(this, _config);
            WirePacketHandlers();
            Instance = this;
        }

        // ---------- lifecycle ----------

        public bool StartHost(string localDisplayName)
        {
            if (State != SessionState.Idle)
            {
                Log.Warn("CoopSession", $"StartHost called in state {State}");
                return false;
            }
            LocalDisplayName = localDisplayName;
            _transport = CreateTransport(isHost: true);
            HookTransport();
            _transport.Start();
            if (!_transport.IsRunning)
            {
                Log.Error("CoopSession", "transport failed to start");
                Cleanup();
                return false;
            }
            Role = CoopRole.Host;
            State = SessionState.Hosting;
            ActivateMode();
            Log.Info("CoopSession", $"hosting as {LocalDisplayName} (id {LocalPeerId})");
            return true;
        }

        public bool JoinHost(string localDisplayName)
        {
            if (State != SessionState.Idle)
            {
                Log.Warn("CoopSession", $"JoinHost called in state {State}");
                return false;
            }
            LocalDisplayName = localDisplayName;
            _transport = CreateTransport(isHost: false);
            HookTransport();
            _transport.Start();
            if (!_transport.IsRunning)
            {
                Log.Error("CoopSession", "transport failed to start");
                Cleanup();
                return false;
            }
            Role = CoopRole.Client;
            State = SessionState.Connecting;
            Log.Info("CoopSession",
                $"connecting to {_config.JoinAddress}:{_config.JoinPort} as {LocalDisplayName}");
            return true;
        }

        public void Disconnect(DisconnectReason reason, string detail = "")
        {
            if (State == SessionState.Idle) return;
            State = SessionState.Disconnecting;
            var pkt = new DisconnectPacket { Reason = reason, Detail = detail ?? string.Empty };
            foreach (var peer in _peers.Values)
                TrySend(peer, pkt, Transport.SendReliability.Reliable);
            Cleanup();
        }

        private void Cleanup()
        {
            try { _mode?.Deactivate(this); } catch (Exception ex) { Log.Error("CoopSession", ex); }
            _mode = null;
            try { _transport?.Stop(); } catch (Exception ex) { Log.Error("CoopSession", ex); }
            try { _transport?.Dispose(); } catch (Exception ex) { Log.Error("CoopSession", ex); }
            _transport = null;
            _peers.Clear();
            State = SessionState.Idle;
            Role = CoopRole.None;
        }

        // ---------- per-frame tick ----------

        public void Tick(float dt)
        {
            if (State == SessionState.Idle) return;
            _uptimeSeconds += dt;
            _transport?.Poll();
            _voteManager.Tick(dt);
            try { _mode?.Tick(this, dt); }
            catch (Exception ex) { Log.Error("CoopSession", ex); }

            if (_ticks.Advance(dt, _uptimeSeconds))
                OnNetworkTick();
        }

        private void OnNetworkTick()
        {
            _secondsSinceHeartbeat += 1f / _config.NetworkTickRate;
            if (_secondsSinceHeartbeat >= _config.HeartbeatInterval)
            {
                _secondsSinceHeartbeat = 0f;
                Broadcast(new HeartbeatPacket
                {
                    SequenceNumber = unchecked(++_heartbeatSeq),
                    SenderUptimeSeconds = _uptimeSeconds,
                }, Transport.SendReliability.Unreliable);
            }

            // Timeout sweep: drop peers we haven't heard from in too long.
            var tolerance = _config.HeartbeatInterval * _config.HeartbeatMissTolerance;
            List<CoopPeer> dead = null;
            foreach (var peer in _peers.Values)
            {
                if (!peer.HandshakeComplete) continue;
                if (_uptimeSeconds - peer.LastHeartbeatSeconds <= tolerance) continue;
                (dead ??= new List<CoopPeer>()).Add(peer);
            }
            if (dead != null)
            {
                foreach (var peer in dead) DropPeer(peer, DisconnectReason.Timeout);
            }
        }

        // ---------- send ----------

        public void Send<T>(CoopPeer peer, T packet, Transport.SendReliability reliability)
            where T : IPacket
        {
            if (_transport == null || peer == null) return;
            try
            {
                var buf = new PacketBuffer(64);
                buf.WriteByte((byte)packet.Id);
                packet.Write(buf);
                _transport.Send(peer.SteamId, buf.ToArray(), reliability);
            }
            catch (Exception ex) { Log.Error("CoopSession", ex); }
        }

        private void TrySend<T>(CoopPeer peer, T packet, Transport.SendReliability reliability)
            where T : IPacket
        {
            try { Send(peer, packet, reliability); } catch (Exception ex) { Log.Error("CoopSession", ex); }
        }

        public void Broadcast<T>(T packet, Transport.SendReliability reliability) where T : IPacket
        {
            foreach (var peer in _peers.Values)
                Send(peer, packet, reliability);
        }

        // ---------- transport plumbing ----------

        private ITransport CreateTransport(bool isHost)
        {
            if (_config.UseLoopbackTransport)
                return new LoopbackTransport(localId: isHost ? LiteNetLibTransport.HostId : LiteNetLibTransport.ClientId);
            return new LiteNetLibTransport(
                isHost: isHost,
                listenPort: _config.ListenPort,
                connectAddress: _config.JoinAddress,
                connectPort: _config.JoinPort,
                connectionKey: _config.ConnectionKey);
        }

        private void HookTransport()
        {
            _transport.OnMessage += OnRawMessage;
            _transport.OnPeerConnected += OnTransportPeerConnected;
            _transport.OnPeerDisconnected += OnTransportPeerDisconnected;
        }

        private void OnRawMessage(ulong from, byte[] payload)
        {
            if (payload == null || payload.Length < 1) return;
            try
            {
                var buf = new PacketBuffer(payload, payload.Length);
                var id = (PacketId)buf.ReadByte();
                if (!PacketRegistry.IsRegistered(id))
                {
                    Log.Warn("CoopSession", $"unregistered packet 0x{(byte)id:X2} from {from}");
                    return;
                }
                var pkt = PacketRegistry.Create(id);
                pkt.Read(buf);

                if (!_peers.TryGetValue(from, out var peer))
                {
                    // Inbound from someone we haven't registered yet — for the
                    // host this is the first hello; track them with a
                    // placeholder name until handshake fills it in.
                    peer = new CoopPeer(from, $"peer:{from}", isHost: false);
                    _peers[from] = peer;
                }
                _dispatcher.Dispatch(peer, pkt);
            }
            catch (Exception ex) { Log.Error("CoopSession", ex); }
        }

        private void OnTransportPeerConnected(ulong peerId)
        {
            Log.Info("CoopSession", $"transport reports peer connected: {peerId}");
            if (Role != CoopRole.Client) return;
            // Client side: now that the UDP session is up, register the host
            // peer and fire the app-level handshake. On the host side the
            // CoopPeer is created lazily on the first inbound packet
            // (in OnRawMessage), once we have a display name to attach.
            if (_peers.ContainsKey(peerId)) return;
            var hostPeer = new CoopPeer(peerId, "<host>", isHost: true);
            _peers[peerId] = hostPeer;
            var handshake = HandshakePacket.Local(LocalPeerId, LocalDisplayName);
            Send(hostPeer, handshake, Transport.SendReliability.Reliable);
        }

        private void OnTransportPeerDisconnected(ulong peerId)
        {
            Log.Info("CoopSession", $"transport reports peer disconnected: {peerId}");
            if (_peers.TryGetValue(peerId, out var peer))
                DropPeer(peer, DisconnectReason.Timeout);
        }

        private void DropPeer(CoopPeer peer, DisconnectReason reason)
        {
            Log.Info("CoopSession", $"dropping peer {peer} reason={reason}");
            _peers.Remove(peer.SteamId);
            try { _mode?.OnPeerLeft(this, peer); } catch (Exception ex) { Log.Error("CoopSession", ex); }
            if (Role == CoopRole.Client && peer.IsHost)
            {
                // Host went away — we have nothing to do here.
                Cleanup();
            }
        }

        // ---------- packet handlers ----------

        private void WirePacketHandlers()
        {
            _dispatcher.On<HandshakePacket>(PacketId.Handshake, HandleHandshake);
            _dispatcher.On<WelcomePacket>(PacketId.Welcome, HandleWelcome);
            _dispatcher.On<DisconnectPacket>(PacketId.Disconnect, HandleDisconnect);
            _dispatcher.On<HeartbeatPacket>(PacketId.Heartbeat, HandleHeartbeat);
            _dispatcher.On<TimeControlPacket>(PacketId.TimeControl, HandleTimeControl);
            _dispatcher.On<VoteRequestPacket>(PacketId.VoteRequest, (peer, pkt) => _voteManager.OnVoteRequest(peer, pkt));
            _dispatcher.On<VoteResponsePacket>(PacketId.VoteResponse, (peer, pkt) => _voteManager.OnVoteResponse(peer, pkt));
            _dispatcher.On<VoteResultPacket>(PacketId.VoteResult, (peer, pkt) => _voteManager.OnVoteResult(peer, pkt));
        }

        private void HandleHandshake(CoopPeer peer, HandshakePacket pkt)
        {
            if (Role != CoopRole.Host)
            {
                Log.Warn("CoopSession", "received Handshake but we are not host");
                return;
            }
            peer.DisplayName = string.IsNullOrEmpty(pkt.DisplayName) ? peer.DisplayName : pkt.DisplayName;
            peer.LastHeartbeatSeconds = _uptimeSeconds;

            var accepted = pkt.ProtocolVersion == CoopConfig.ProtocolVersion;
            var welcome = new WelcomePacket
            {
                ProtocolVersion = CoopConfig.ProtocolVersion,
                HostSteamId = LocalPeerId,
                HostDisplayName = LocalDisplayName,
                Accepted = accepted,
                RejectReason = accepted
                    ? string.Empty
                    : $"protocol {pkt.ProtocolVersion} != host {CoopConfig.ProtocolVersion}",
            };
            Send(peer, welcome, Transport.SendReliability.Reliable);

            if (!accepted)
            {
                Log.Warn("CoopSession", $"rejecting {peer}: {welcome.RejectReason}");
                DropPeer(peer, DisconnectReason.ProtocolMismatch);
                return;
            }
            peer.HandshakeComplete = true;
            Log.Info("CoopSession", $"handshake complete with {peer}");
            try { _mode?.OnPeerJoined(this, peer); } catch (Exception ex) { Log.Error("CoopSession", ex); }
        }

        private void HandleWelcome(CoopPeer peer, WelcomePacket pkt)
        {
            if (Role != CoopRole.Client)
            {
                Log.Warn("CoopSession", "received Welcome but we are not client");
                return;
            }
            peer.DisplayName = string.IsNullOrEmpty(pkt.HostDisplayName) ? peer.DisplayName : pkt.HostDisplayName;
            peer.IsHost = true;
            peer.LastHeartbeatSeconds = _uptimeSeconds;

            if (!pkt.Accepted)
            {
                Log.Error("CoopSession", $"host rejected handshake: {pkt.RejectReason}");
                Cleanup();
                return;
            }
            peer.HandshakeComplete = true;
            State = SessionState.Live;
            ActivateMode();
            Log.Info("CoopSession", $"connected to host {peer}");
        }

        private void HandleDisconnect(CoopPeer peer, DisconnectPacket pkt)
        {
            Log.Info("CoopSession", $"peer {peer} disconnected: {pkt.Reason} {pkt.Detail}");
            DropPeer(peer, pkt.Reason);
        }

        private void HandleHeartbeat(CoopPeer peer, HeartbeatPacket pkt)
        {
            peer.LastHeartbeatSeconds = _uptimeSeconds;
        }

        private void HandleTimeControl(CoopPeer peer, TimeControlPacket pkt)
        {
            // Drop out-of-order packets.
            if (pkt.Sequence != 0 && pkt.Sequence <= _timeControlSeqIn) return;
            _timeControlSeqIn = pkt.Sequence;
            Log.Debug("CoopSession", $"inbound time control {pkt.Mode} seq={pkt.Sequence} from {peer}");
            Patches.TimeControlPatch.ApplyRemote(pkt.Mode);
        }

        // ---------- mode activation ----------

        private void ActivateMode()
        {
            _mode = _config.Mode switch
            {
                CoopModeKind.CompanionInArmy => new CompanionModeManager(),
                CoopModeKind.IndependentWarband => new WarbandModeManager(),
                _ => new CompanionModeManager(),
            };
            try { _mode.Activate(this); }
            catch (Exception ex)
            {
                Log.Error("CoopSession", ex);
                _mode = null;
            }
        }
    }
}
