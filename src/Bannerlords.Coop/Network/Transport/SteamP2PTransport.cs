using System;
using System.Collections.Generic;
using Bannerlords.Coop.Util;
using Steamworks;

namespace Bannerlords.Coop.Network.Transport
{
    /// <summary>
    /// Steam Networking Messages (the modern session-based P2P API) transport.
    ///
    /// Wraps <see cref="SteamNetworkingMessages"/> which handles NAT traversal,
    /// session establishment, and per-channel reliability for us. We don't
    /// re-initialize the Steam API — Bannerlord has already done that — we
    /// just register callbacks and use the already-running client.
    /// </summary>
    public sealed class SteamP2PTransport : ITransport
    {
        private const int CHANNEL = 0;

        private const int SEND_FLAG_UNRELIABLE = Constants.k_nSteamNetworkingSend_Unreliable;
        private const int SEND_FLAG_RELIABLE = Constants.k_nSteamNetworkingSend_Reliable;

        private Callback<SteamNetworkingMessagesSessionRequest_t> _cbSessionRequest;
        private Callback<SteamNetworkingMessagesSessionFailed_t> _cbSessionFailed;

        private readonly HashSet<ulong> _connectedPeers = new HashSet<ulong>();
        private IntPtr[] _recvBuf = new IntPtr[64];

        public ulong LocalId { get; private set; }
        public bool IsRunning { get; private set; }

        public event Action<ulong, byte[]> OnMessage;
        public event Action<ulong> OnPeerConnected;
        public event Action<ulong> OnPeerDisconnected;

        public void Start()
        {
            if (IsRunning) return;

            if (!SteamAPI.IsSteamRunning())
            {
                Log.Error("SteamP2PTransport", "Steam is not running");
                return;
            }
            // The game has already called SteamAPI.Init(); calling again is a
            // no-op-with-true return.
            if (!SteamAPI.Init())
            {
                Log.Error("SteamP2PTransport", "SteamAPI.Init() failed");
                return;
            }

            LocalId = SteamUser.GetSteamID().m_SteamID;

            _cbSessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
            _cbSessionFailed = Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);

            IsRunning = true;
            Log.Info("SteamP2PTransport", $"started, local SteamID={LocalId}");
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;

            foreach (var peer in _connectedPeers)
            {
                var id = new SteamNetworkingIdentity();
                id.SetSteamID64(peer);
                SteamNetworkingMessages.CloseSessionWithUser(ref id);
            }
            _connectedPeers.Clear();

            _cbSessionRequest?.Dispose(); _cbSessionRequest = null;
            _cbSessionFailed?.Dispose(); _cbSessionFailed = null;
            Log.Info("SteamP2PTransport", "stopped");
        }

        public void Send(ulong peer, byte[] data, SendReliability reliability)
        {
            if (!IsRunning) return;
            var id = new SteamNetworkingIdentity();
            id.SetSteamID64(peer);

            var flags = reliability == SendReliability.Reliable
                ? SEND_FLAG_RELIABLE
                : SEND_FLAG_UNRELIABLE;

            // Pin the buffer for the duration of the unmanaged send.
            var handle = System.Runtime.InteropServices.GCHandle.Alloc(
                data, System.Runtime.InteropServices.GCHandleType.Pinned);
            try
            {
                var ptr = handle.AddrOfPinnedObject();
                var result = SteamNetworkingMessages.SendMessageToUser(
                    ref id, ptr, (uint)data.Length, flags, CHANNEL);
                if (result != EResult.k_EResultOK)
                    Log.Warn("SteamP2PTransport",
                        $"SendMessageToUser to {peer} returned {result}");
            }
            finally { handle.Free(); }
        }

        public void Poll()
        {
            if (!IsRunning) return;

            // ReceiveMessagesOnChannel returns up to N pointers we must release.
            int received = SteamNetworkingMessages.ReceiveMessagesOnChannel(
                CHANNEL, _recvBuf, _recvBuf.Length);

            for (int i = 0; i < received; i++)
            {
                var ptr = _recvBuf[i];
                if (ptr == IntPtr.Zero) continue;
                try
                {
                    var msg = SteamNetworkingMessage_t.FromIntPtr(ptr);
                    var fromId = msg.m_identityPeer.GetSteamID64();
                    var bytes = new byte[msg.m_cbSize];
                    System.Runtime.InteropServices.Marshal.Copy(
                        msg.m_pData, bytes, 0, msg.m_cbSize);

                    if (_connectedPeers.Add(fromId))
                        OnPeerConnected?.Invoke(fromId);

                    OnMessage?.Invoke(fromId, bytes);
                }
                catch (Exception ex) { Log.Error("SteamP2PTransport", ex); }
                finally
                {
                    SteamNetworkingMessage_t.Release(ptr);
                    _recvBuf[i] = IntPtr.Zero;
                }
            }
        }

        public void Dispose() => Stop();

        // ---------- callbacks ----------

        private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t evt)
        {
            var remote = evt.m_identityRemote.GetSteamID64();
            // For Mode A/B alpha we accept any inbound session; the lobby layer
            // is the real gate on who's allowed to join the session.
            // TODO: cross-check against CoopSession's allowed peer list.
            var id = evt.m_identityRemote;
            SteamNetworkingMessages.AcceptSessionWithUser(ref id);
            if (_connectedPeers.Add(remote))
                OnPeerConnected?.Invoke(remote);
            Log.Info("SteamP2PTransport", $"accepted session from {remote}");
        }

        private void OnSessionFailed(SteamNetworkingMessagesSessionFailed_t evt)
        {
            var remote = evt.m_info.m_identityRemote.GetSteamID64();
            if (_connectedPeers.Remove(remote))
                OnPeerDisconnected?.Invoke(remote);
            Log.Warn("SteamP2PTransport",
                $"session failed with {remote}: state={evt.m_info.m_eState} eEnd={evt.m_info.m_eEndReason}");
        }
    }
}
