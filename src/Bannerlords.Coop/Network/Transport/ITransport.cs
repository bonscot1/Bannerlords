using System;

namespace Bannerlords.Coop.Network.Transport
{
    public enum SendReliability
    {
        /// <summary>UDP-style: may drop, may arrive out of order. Cheapest.</summary>
        Unreliable,
        /// <summary>Guaranteed delivery, in order. Use for state changes.</summary>
        Reliable,
    }

    /// <summary>
    /// Bytes-in / bytes-out abstraction the rest of the network layer talks
    /// to. The transport is responsible for framing (one Send -> one OnMessage
    /// on the other side), but NOT for serialization of <see cref="Packet.IPacket"/>;
    /// that lives in <see cref="Session.CoopSession"/>.
    /// </summary>
    public interface ITransport : IDisposable
    {
        /// <summary>Local identifier — Steam ID on the steam transport.</summary>
        ulong LocalId { get; }

        bool IsRunning { get; }

        /// <summary>Fired when a fully framed message arrives from a peer.</summary>
        event Action<ulong /*from*/, byte[] /*payload*/> OnMessage;

        /// <summary>Fired when the transport observes a new peer connecting.</summary>
        event Action<ulong /*peer*/> OnPeerConnected;

        /// <summary>Fired when a peer disconnects or times out at the transport layer.</summary>
        event Action<ulong /*peer*/> OnPeerDisconnected;

        void Start();
        void Stop();

        /// <summary>
        /// Send a framed payload to a single peer. <paramref name="data"/> must
        /// be wholly owned by the transport after this call returns until it
        /// is sent — copy if you need to reuse the buffer.
        /// </summary>
        void Send(ulong peer, byte[] data, SendReliability reliability);

        /// <summary>Pump the transport. Must be called every network tick.</summary>
        void Poll();
    }
}
