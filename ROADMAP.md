# Roadmap

Milestones are ordered, but no timeline is promised. Each milestone has an
exit criterion that can be demonstrated end-to-end on two real PCs (except
M0's CI portion, which builds in GitHub Actions). We don't move on until
the previous milestone passes its exit criterion on a clean install.

---

## M0 — Handshake + global pause + soldier-join

The smallest playable thing. Two players connect over UDP (IP-based join,
no Steam), the second player's name shows up in the host's party roster
as a generic troop, and either player pressing pause / changing time
speed updates the other's clock.

**Deliverables**
- `CoopSession` state machine (Idle → Hosting / Connecting → Live).
- `LiteNetLibTransport` — UDP transport via LiteNetLib (the same transport
  the BannerlordCoop reference mod uses). Host listens on a configured
  port; client connects to a configured address. Fixed peer IDs for M0
  (host=1, client=2); dynamic assignment lands in M2 alongside
  multi-client.
- Packets: `Handshake`, `Welcome`, `Disconnect`, `Heartbeat`, `TimeControl`,
  `VoteRequest`, `VoteResponse`, `VoteResult`.
- `TimeControlPatch` — Harmony postfix on `Campaign.TimeControlMode`
  setter that broadcasts the new value and short-circuits on inbound
  application (loop-back guarded).
- `GameStateManagerPatch` — Harmony prefix on
  `GameStateManager.RegisterActiveStateDisableRequest` that suppresses
  menu-induced world freezes while a session is live (encyclopedia,
  inventory, character window no longer pause the world for the other
  player).
- `VoteManager` — protocol scaffolding for coop votes (initiator broadcasts
  `VoteRequest`, peers respond, initiator broadcasts `VoteResult`).
  Configurable per-action timeout. M0 ships the manager and a text-mode
  responder prompt (`[Y]/[N]` hotkey poll via `InformationManager`); the
  Gauntlet popup and the actual integration with pause/menu/settlement
  call-sites lands in M0.7.
- `SoldierAttachment` — when a client joins, host inserts a placeholder
  troop into `MainParty.MemberRoster` with the client's persona name.
  Attaches are queued if no campaign is loaded yet at handshake time and
  drained on the first tick where `Campaign.Current` exists.
- `MainMenuHooks` + F8/F9 hotkeys — main-menu entries trigger host/join
  before a campaign is loaded; F8/F9 toggle the same actions from inside
  a campaign. The in-campaign hotkey is the recommended M0 flow because
  the soldier attach fires immediately rather than queueing.
- `ConfigLoader` — reads `coopconfig.json` from the module directory at
  load time. Falls back to built-in defaults when missing. A sample
  config ships at `dist/Bannerlords.Coop/coopconfig.sample.json`.
- Drop-in packaging: build output lands in `dist/Bannerlords.Coop/`,
  which is the exact layout Bannerlord expects under `Modules/`. The
  user copies one folder and edits `coopconfig.json` — no other
  install steps.
- CI workflow that runs `dotnet restore && dotnet build -c Release` on
  every PR.

**Exit criterion**
1. Mod loads on Bannerlord 1.2.12 without Harmony errors.
2. Two real PCs handshake over LiteNetLib, log shows
   `Handshake → Welcome → Live`.
3. Client's persona appears as a troop in host's party.
4. Host hits spacebar → client's clock pauses within ~250 ms (and the
   reverse).
5. Opening encyclopedia/inventory on one client does NOT freeze the
   other client's world.
6. Disconnect on either side cleanly tears down the session on the other.

---

## M0.7 — Voting UX + integration

The wire protocol exists in M0; M0.7 wires it into the actions players
actually trigger and replaces the text-mode prompt with real UI.

**Deliverables**
- Gauntlet popup for the responder ("X wants to pause", Yes/No buttons,
  countdown bar). Replaces M0's `InformationManager` placeholder.
- Hook `TimeControlPatch` to route the local change through `VoteManager`
  rather than broadcasting unconditionally.
- Hook `GameStateManagerPatch` to (optionally) request a vote for
  menu-pause instead of just suppressing.
- Hook the settlement-enter path through `VoteManager` (groundwork for
  M3+).
- Per-action timeout overrides in `CoopConfig` (pause vs. settlement-
  enter want different defaults).

**Exit criterion**
- Client presses spacebar; host sees the Gauntlet popup with a
  countdown; pressing Y on host pauses both clients within one network
  tick; pressing N keeps both unpaused.

---

## M1 — Read-only campaign mirror

Client sees what the host sees on the campaign map. No input on the map yet;
input requests come in M2/M4 (Mode A) and M5+ (Mode B).

**Deliverables**
- `MapStateSyncer` — host snapshots key entities (own party position +
  speed, time of day, visible parties within X, kingdoms basics) and ships
  deltas at the network tick rate.
- Camera: client follows host's party by default; free-look toggle.
- Map-input suppression on client.
- Packets: `MapStateSnapshot`, `MapStateDelta`, `CameraFocus`.

**Exit criterion**
- Host moves the party, client sees the icon move in near-real-time
  (≤ 1 network tick of lag, no rubber-banding visible at speed 1).

---

## M2 — Client owns a real Hero

The placeholder troop from M0 is upgraded to a `Hero` with persistent
skills, perks, focus points, attributes, traits, and a real inventory.

**Deliverables**
- `HeroProvisioning` — on first client join, host creates a
  special hero via `HeroCreator.CreateSpecialHero`, stashes the mapping
  (Steam ID → Hero StringId) in the coop sidecar.
- Skills / perks / focus screen: client opens UI locally, edits are
  routed as requests to host, host applies, change broadcasts back.
- Inventory packets: client UI shows host's authoritative inventory for
  its own hero; swap/equip/trade actions go host-side then echo back.
- Sidecar persistence layer (`<save>.coop.json`) with versioning.

**Exit criterion**
- Client levels up a skill, save game, reload — skill survives.
- Client equips an item from host's stash, both players see the change,
  save+reload preserves it.
- Save loads in vanilla single-player without error (hero still exists,
  sidecar simply unused).

---

## M3 — Battles

Bannerlord's mission system was designed for either SP or its own MP
modes, not for arbitrary live coop. This is by far the biggest milestone.

**Deliverables**
- Mission-side networking, separate from campaign-side. The campaign loop
  pauses while a mission runs, so M0's pause infra covers map-side timing.
- Agent-state sync on `Mission.Tick`. Host owns AI agents and physics;
  client controls their hero agent and sends input deltas.
- Mission lifecycle packets: `MissionStart`, `MissionTick`, `MissionEnd`,
  `MissionResultAck`.
- Battle-result reconciliation: party rosters, casualties, loot,
  influence all applied host-side, then broadcast.

**Exit criterion**
- Host attacks a looter party; client joins the field battle, controls
  their own hero, both players see the same outcome screen.
- Crashes/desyncs during a battle leave the campaign side recoverable
  (battle aborts, both return to map with pre-battle state).

---

## M3.5 — Combat Roles (Captain / Tactical Advisor)

Mode-A polish for the battlefield: the host's hero is the player on the
field; the client(s) can take a non-direct-combat role and still affect
the fight meaningfully.

**Deliverables**
- Role selection in pre-battle screen: each non-host participant picks
  **Captain** (assume command of a sub-formation, give orders via the
  standard formation UI) or **Tactical Advisor** (no body on the field,
  but markers + voice/ping commands visible to all).
- Captain mode: the client owns a `Formation` for the duration of the
  mission; their formation-order packets bypass the host's command UI
  and apply directly.
- Tactical Advisor mode: a top-down map view alongside the host's normal
  camera; the advisor can drop pings (move-here, focus-fire-here,
  retreat) that overlay on the host's HUD.
- Both roles share the existing M3 mission-lifecycle / agent-state
  packets; only the per-role command surface is new.

**Exit criterion**
- Client picks Captain, takes command of the host's cavalry formation,
  orders a flank — formation moves under client's control while host
  continues to lead the infantry.
- Client picks Tactical Advisor, drops a ping on an archer line; ping is
  visible to the host within one network tick.

---

## M4 — Mode A feature-complete

Client can act inside the host's party context: dialogues, quests, their
own relations with NPCs, settlement-action menus.

**Deliverables**
- Dialog protocol: only one player in a given dialog at a time; the
  other sees "X is talking to Y".
- Per-hero relation tracking (vanilla already supports this; we just sync
  client-driven changes).
- Quest log per-hero; client owns their own quest state, host arbitrates
  world effects.
- Settlement actions menu request/grant.

**Exit criterion**
- Client takes a quest from a notable, fulfills it, gets rewarded —
  visible only to them.
- Two simultaneous attempted dialogs are serialized cleanly (no double-
  open, no soft-lock).

---

## M5 — Mode B foundation

Second player gets their own `MobileParty` on the campaign map.

**Deliverables**
- Clan + party provisioning for the client on first Mode-B join.
- Two-way map-state sync (no longer just host → client).
- Map input on client: click-to-move, attack-party, enter-settlement.
- Two-party encounter flow: independent travel, independent battles
  against AI parties.
- Time still globally paused/unpaused (real-time interaction is M6).

**Exit criterion**
- Client moves their party to a settlement and enters it while the host
  is elsewhere on the map.
- Client engages an AI party in a battle; host's UI is unaffected.

---

## M6 — Mode B feature-complete

Real-time, simultaneous interaction between players' clans.

**Deliverables**
- Action-conflict arbitrator (first-to-host-tick wins; loser sees
  reject + reason).
- Diplomacy packets between player clans (alliance, war, trade pact,
  truce).
- Optional shared kingdom / vassalage paths.

**Exit criterion**
- Two players can simultaneously fight each other on the campaign map.
- Two players can simultaneously trade with the same notable without
  desyncing each other's inventory.

---

## Post-M6 (not scheduled)

- Player count > 2 (transport supports it; the protocol and authority
  model need re-validation past 2 because of vote-tallying and per-hero
  sync).
- Host migration when the host disconnects.
- Mod-compatibility surface: documented load-order rules, opt-out
  configuration for known-broken mods.
- In-game settings UI (currently config is file-driven).
- Optional dedicated-host build that runs the simulation headless.
- Steam-rich-presence / friend-invite integration on top of the IP-based
  transport (cosmetic — UDP is what actually carries the session).

---

## Explicitly out of scope

- **No MMO-style server.** This is a small-group P2P mod, not a hosted
  service.
- **No anti-cheat.** Trust your party. Host has full authority and can,
  in principle, fake any state.
- **No console / Game Pass support.** PC (Win64) only.
- **No automatic mod-compatibility guarantees.** Mods that patch the same
  campaign hooks we patch (time control, party tick, mission init) will
  conflict. Expect manual triage.
- **No backward-compatibility promise during alpha.** Saves made on an
  older alpha may not load on a newer alpha until M2 ships the sidecar
  versioning layer.
