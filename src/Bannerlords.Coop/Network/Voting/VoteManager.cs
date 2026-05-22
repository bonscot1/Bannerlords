using System;
using System.Collections.Generic;
using Bannerlords.Coop.Network.Packets;
using Bannerlords.Coop.Network.Session;
using Bannerlords.Coop.Network.Transport;
using Bannerlords.Coop.Util;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace Bannerlords.Coop.Network.Voting
{
    /// <summary>
    /// Orchestrates coop votes. One vote in flight at a time across the
    /// session (M0 limitation; a queue can come later). The same instance
    /// handles both initiator and responder roles — the
    /// <see cref="PendingVote.IsLocalInitiator"/> flag distinguishes them.
    ///
    /// Responder UI is a vanilla <see cref="InquiryData"/> popup with
    /// Yes/No buttons; the Y/N hotkey poll is kept as a backup for
    /// contexts where the inquiry doesn't render. When
    /// <see cref="CoopConfig.VotingEnabled"/> is false the responder
    /// auto-accepts without showing the popup (useful for solo testing).
    /// </summary>
    public sealed class VoteManager
    {
        private readonly CoopSession _session;
        private readonly CoopConfig _config;
        private PendingVote _pending;
        private ushort _nextVoteId;

        public bool IsVoteInFlight => _pending != null;
        public ushort PendingVoteId => _pending?.VoteId ?? (ushort)0;

        public VoteManager(CoopSession session, CoopConfig config)
        {
            _session = session;
            _config = config;
        }

        // ---------- initiator API ----------

        /// <summary>
        /// Convenience: vote to set <c>Campaign.TimeControlMode</c>. Returns
        /// true if the vote was initiated (or short-circuited to passed when
        /// solo).
        /// </summary>
        public bool RequestSetTimeControl(WireTimeControl mode, string reason,
            Action onPassed, Action onFailed) =>
            TryStartVote(VoteAction.SetTimeControl, (byte)mode,
                _config.TimeControlVoteTimeoutSeconds, reason, onPassed, onFailed);

        /// <summary>
        /// Convenience: vote to pause the world because a menu would normally
        /// pause it locally. Only fires when
        /// <see cref="CoopConfig.VoteOnMenuPause"/> is true.
        /// </summary>
        public bool RequestMenuPause(string reason,
            Action onPassed, Action onFailed) =>
            TryStartVote(VoteAction.MenuPause, (byte)WireTimeControl.Stop,
                _config.MenuPauseVoteTimeoutSeconds, reason, onPassed, onFailed);

        public bool TryStartVote(VoteAction action, byte detail,
            float timeoutSeconds, string reason,
            Action onPassed, Action onFailed)
        {
            // Solo: short-circuit to pass. No peers means no one to vote;
            // just apply locally. Broadcasts skip naturally since the peer
            // list is empty.
            var liveRemotePeers = CountLiveRemotePeers();
            if (_session.State != SessionState.Live || liveRemotePeers == 0)
            {
                SafeInvoke(onPassed);
                return true;
            }
            if (_pending != null)
            {
                Log.Warn("VoteManager", $"vote already in flight (id={_pending.VoteId}); rejecting new request");
                SafeInvoke(onFailed);
                return false;
            }

            var id = unchecked(++_nextVoteId);
            var timeout = timeoutSeconds <= 0 ? _config.VoteDefaultTimeoutSeconds : timeoutSeconds;
            _pending = new PendingVote
            {
                VoteId = id,
                Action = action,
                ActionDetail = detail,
                TimeoutSeconds = timeout,
                InitiatorPeerId = _session.LocalPeerId,
                IsLocalInitiator = true,
                Reason = reason ?? string.Empty,
                OnPassed = onPassed,
                OnFailed = onFailed,
            };
            _pending.Yeses.Add(_session.LocalPeerId); // initiator implicit yes

            _session.Broadcast(new VoteRequestPacket
            {
                VoteId = id,
                Action = (byte)action,
                ActionDetail = detail,
                TimeoutSeconds = timeout,
                InitiatorPeerId = _session.LocalPeerId,
                Reason = _pending.Reason,
            }, SendReliability.Reliable);

            if (_config.VotingEnabled) DisplayInitiatorPrompt(_pending);
            Log.Info("VoteManager",
                $"started vote id={id} action={action} detail={detail} timeout={timeout:F1}s reason='{_pending.Reason}'");
            return true;
        }

        // ---------- per-frame ----------

        public void Tick(float dt)
        {
            if (_pending == null) return;
            _pending.ElapsedSeconds += dt;

            // Responder side: poll Y/N as a backup for contexts where the
            // inquiry popup doesn't render. The popup's buttons hit the
            // same SendLocalResponse path; LocalResponseSent guards
            // double-fire.
            if (!_pending.IsLocalInitiator && !_pending.LocalResponseSent && _config.VotingEnabled)
            {
                if (Input.IsKeyPressed(InputKey.Y)) SendLocalResponse(true);
                else if (Input.IsKeyPressed(InputKey.N)) SendLocalResponse(false);
            }

            if (_pending.ElapsedSeconds < _pending.TimeoutSeconds) return;

            // Timeout reached.
            if (_pending.IsLocalInitiator)
            {
                // Treat silent peers as rejection — safer default than auto-yes.
                Log.Info("VoteManager", $"vote {_pending.VoteId} timed out without unanimous yes");
                Resolve(passed: false);
            }
            else if (!_pending.LocalResponseSent)
            {
                // Responder never pressed a key — auto-no.
                Log.Info("VoteManager", $"vote {_pending.VoteId} responder timed out (auto-no)");
                SendLocalResponse(false);
            }
        }

        // ---------- inbound packet handlers ----------

        public void OnVoteRequest(CoopPeer from, VoteRequestPacket pkt)
        {
            if (_pending != null)
            {
                // Already busy — auto-reject so the initiator doesn't hang.
                _session.Send(from, new VoteResponsePacket
                {
                    VoteId = pkt.VoteId,
                    Accept = false,
                    ResponderPeerId = _session.LocalPeerId,
                }, SendReliability.Reliable);
                return;
            }
            _pending = new PendingVote
            {
                VoteId = pkt.VoteId,
                Action = (VoteAction)pkt.Action,
                ActionDetail = pkt.ActionDetail,
                TimeoutSeconds = pkt.TimeoutSeconds,
                InitiatorPeerId = pkt.InitiatorPeerId,
                IsLocalInitiator = false,
                Reason = pkt.Reason ?? string.Empty,
            };

            if (!_config.VotingEnabled)
            {
                // Voting disabled on this PC — auto-accept without UI.
                // Keeps both peers in sync while skipping prompts.
                Log.Info("VoteManager",
                    $"voting disabled locally; auto-accepting vote {pkt.VoteId}");
                SendLocalResponse(true);
                return;
            }

            DisplayResponderInquiry(_pending);
            Log.Info("VoteManager",
                $"received vote id={pkt.VoteId} action={(VoteAction)pkt.Action} from {from}");
        }

        public void OnVoteResponse(CoopPeer from, VoteResponsePacket pkt)
        {
            if (_pending == null || _pending.VoteId != pkt.VoteId || !_pending.IsLocalInitiator) return;
            if (pkt.Accept) _pending.Yeses.Add(from.SteamId);
            else _pending.Nos.Add(from.SteamId);
            Log.Info("VoteManager",
                $"vote {pkt.VoteId} response from {from}: {(pkt.Accept ? "yes" : "no")}");

            // Any no → fail immediately.
            if (_pending.Nos.Count > 0)
            {
                Resolve(passed: false);
                return;
            }
            // All required participants said yes.
            var required = CountLiveRemotePeers() + 1; // peers + self
            if (_pending.Yeses.Count >= required) Resolve(passed: true);
        }

        public void OnVoteResult(CoopPeer from, VoteResultPacket pkt)
        {
            if (_pending == null || _pending.VoteId != pkt.VoteId) return;
            if (_pending.IsLocalInitiator)
            {
                // Our own broadcast shouldn't loop back; defensive guard.
                return;
            }
            if (pkt.Passed) ApplyAction((VoteAction)pkt.Action, pkt.ActionDetail);
            DisplayResult(_pending, pkt.Passed);
            Log.Info("VoteManager", $"vote {pkt.VoteId} resolved: passed={pkt.Passed}");
            _pending = null;
        }

        // ---------- internals ----------

        private void Resolve(bool passed)
        {
            if (_pending == null) return;
            // Broadcast result so responders also apply.
            _session.Broadcast(new VoteResultPacket
            {
                VoteId = _pending.VoteId,
                Passed = passed,
                Action = (byte)_pending.Action,
                ActionDetail = _pending.ActionDetail,
            }, SendReliability.Reliable);

            if (passed) ApplyAction(_pending.Action, _pending.ActionDetail);
            DisplayResult(_pending, passed);

            if (passed) SafeInvoke(_pending.OnPassed);
            else SafeInvoke(_pending.OnFailed);
            _pending = null;
        }

        private void SendLocalResponse(bool accept)
        {
            if (_pending == null || _pending.IsLocalInitiator || _pending.LocalResponseSent) return;
            _pending.LocalResponseSent = true;
            CoopPeer initiator = null;
            foreach (var p in _session.Peers)
            {
                if (p.SteamId == _pending.InitiatorPeerId) { initiator = p; break; }
            }
            if (initiator == null)
            {
                Log.Warn("VoteManager", $"can't find initiator {_pending.InitiatorPeerId} for vote {_pending.VoteId}");
                return;
            }
            _session.Send(initiator, new VoteResponsePacket
            {
                VoteId = _pending.VoteId,
                Accept = accept,
                ResponderPeerId = _session.LocalPeerId,
            }, SendReliability.Reliable);
            if (_config.VotingEnabled)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Coop vote] You voted {(accept ? "YES" : "NO")} on vote #{_pending.VoteId}."));
            }
        }

        private void ApplyAction(VoteAction action, byte detail)
        {
            try
            {
                switch (action)
                {
                    case VoteAction.SetTimeControl:
                        Patches.TimeControlPatch.ApplyRemote((WireTimeControl)detail);
                        break;
                    case VoteAction.MenuPause:
                        // detail carries the WireTimeControl byte to set —
                        // typically Stop. Auto-unpause on menu close is not
                        // yet wired (see CoopConfig.VoteOnMenuPause notes).
                        Patches.TimeControlPatch.ApplyRemote((WireTimeControl)detail);
                        break;
                    case VoteAction.EnterSettlement:
                        // Reserved for M3 alongside settlement-state sync;
                        // applying it locally without the rest of the world
                        // catching up would just teleport the initiator.
                        Log.Debug("VoteManager", "EnterSettlement action not yet wired");
                        break;
                }
            }
            catch (Exception ex) { Log.Error("VoteManager", ex); }
        }

        private int CountLiveRemotePeers()
        {
            int n = 0;
            foreach (var p in _session.Peers)
                if (p.HandshakeComplete) n++;
            return n;
        }

        private static void SafeInvoke(Action a)
        {
            if (a == null) return;
            try { a(); } catch (Exception ex) { Log.Error("VoteManager", ex); }
        }

        // ---------- UI ----------

        private static void DisplayInitiatorPrompt(PendingVote v)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Coop vote #{v.VoteId}] You proposed: {v.Reason}. Waiting on peers ({v.TimeoutSeconds:F0}s)..."));
        }

        private void DisplayResponderInquiry(PendingVote v)
        {
            var voteId = v.VoteId; // capture for closure stability
            try
            {
                var inquiry = new InquiryData(
                    titleText: $"Coop vote #{voteId}",
                    text: $"{v.Reason}\n\nThe world keeps running until you decide. Auto-NO in ~{v.TimeoutSeconds:F0}s.",
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: "Accept (Y)",
                    negativeText: "Reject (N)",
                    affirmativeAction: () => RespondById(voteId, true),
                    negativeAction: () => RespondById(voteId, false));
                InformationManager.ShowInquiry(inquiry, pauseGameActiveState: false);
            }
            catch (Exception ex)
            {
                Log.Error("VoteManager", ex);
                // Fall back to the text-mode prompt so the user still knows
                // a vote is in flight and can use the Y/N hotkeys.
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Coop vote #{v.VoteId}] {v.Reason} — press [Y] to accept, [N] to reject ({v.TimeoutSeconds:F0}s)."));
            }
        }

        private void RespondById(ushort voteId, bool accept)
        {
            // Guard against stale popup clicks: only act if the in-flight
            // vote is still this one and we haven't already responded.
            if (_pending == null || _pending.VoteId != voteId) return;
            SendLocalResponse(accept);
        }

        private static void DisplayResult(PendingVote v, bool passed)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[Coop vote #{v.VoteId}] {(passed ? "PASSED" : "FAILED")}: {v.Reason}"));
        }

        // ---------- internal state ----------

        private sealed class PendingVote
        {
            public ushort VoteId;
            public VoteAction Action;
            public byte ActionDetail;
            public float TimeoutSeconds;
            public float ElapsedSeconds;
            public ulong InitiatorPeerId;
            public bool IsLocalInitiator;
            public string Reason;
            public bool LocalResponseSent;
            public readonly HashSet<ulong> Yeses = new HashSet<ulong>();
            public readonly HashSet<ulong> Nos = new HashSet<ulong>();
            public Action OnPassed;
            public Action OnFailed;
        }
    }
}
