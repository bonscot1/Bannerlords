using System;
using Bannerlords.Coop.Util;

namespace Bannerlords.Coop.Network.Transport
{
    /// <summary>
    /// Steam Networking Messages transport — STUB.
    ///
    /// The first attempt referenced Steamworks.NET 20.x, but that package
    /// dropped its net4x targets and only ships netstandard2.1, which is
    /// not consumable by Bannerlord's net472 runtime. The replacement
    /// candidates (in priority order) are:
    ///   1. Facepunch.Steamworks 2.3.3 — net472-compatible NuGet, but
    ///      different API surface (async/await, events) so the rewrite
    ///      below has to be redone end-to-end against its types.
    ///   2. Steamworks.NET shipped as a vendored DLL built for net472
    ///      from the upstream repo's project file.
    ///   3. TaleWorlds.PlatformService.Steam — Bannerlord's own Steam
    ///      wrapper; least dependency churn but undocumented surface.
    ///
    /// Decision lives in the Bannerlord-Coop-Team/BannerlordCoop research
    /// task; until that resolves, this class throws on use and the
    /// loopback transport carries M0 testing.
    /// </summary>
    public sealed class SteamP2PTransport : ITransport
    {
        public ulong LocalId => 0;
        public bool IsRunning => false;

        public event Action<ulong, byte[]> OnMessage { add { } remove { } }
        public event Action<ulong> OnPeerConnected { add { } remove { } }
        public event Action<ulong> OnPeerDisconnected { add { } remove { } }

        public void Start()
        {
            Log.Error("SteamP2PTransport",
                "Steam transport is stubbed in this build; use LoopbackTransport. " +
                "See SteamP2PTransport.cs for the pending wrapper-choice decision.");
            throw new NotSupportedException(
                "Steam P2P transport is not yet wired. Set CoopConfig.UseLoopbackTransport=true.");
        }

        public void Stop() { }
        public void Send(ulong peer, byte[] data, SendReliability reliability) { }
        public void Poll() { }
        public void Dispose() { }
    }
}
