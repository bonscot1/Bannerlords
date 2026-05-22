# Bannerlords Coop

A campaign-coop mod for *Mount & Blade II: Bannerlord*. Two players, one
shared (or two parallel) campaign world. Status: **pre-alpha, M0 in
progress** — see [ROADMAP.md](./ROADMAP.md).

## What this is

Vanilla Bannerlord is single-player. This mod adds a second human player to
the same campaign in two modes:

- **Mode A — Companion in your army.** The second player is a hero who
  lives inside the host's party. They have their own skills, perks,
  inventory, relations, and quest log, but they ride with the host on the
  campaign map. When the host enters a battle, the second player fights as
  their own hero on the field.
- **Mode B — Independent warband.** The second player has their own clan
  and party. They explore, fight, trade, and politic independently. The
  host is still the authority on world state, but each player drives their
  own party with their own input.

The host is authoritative for all world simulation; clients send requests
and render the host's state. This is how we avoid having to rewrite the
campaign loop to be deterministic.

## Status

| Milestone | What it gives you | State |
|-----------|-------------------|-------|
| M0 | Two PCs handshake over UDP, second player appears in host's roster as a soldier, pause/time-speed syncs both ways | code complete, exit criteria pending on-hardware verification |
| M0.7 | Voting UX (Gauntlet popup) + vote-gated pause/menu/settlement entry | not started |
| M1 | Client sees a live mirror of the host's campaign map (read-only) | not started |
| M2 | Client's soldier becomes a real Hero with skills/perks/inventory | not started |
| M3 | Battles: client controls their hero on the host's battlefield | not started |
| M3.5 | Combat roles: Captain (commands a formation) or Tactical Advisor | not started |
| M4 | Mode A feature-complete: dialog, quests, settlement actions | not started |
| M5 | Mode B foundation: each player commands an independent party | not started |
| M6 | Mode B feature-complete: real-time interaction between players | not started |

No timeline is promised. See [ROADMAP.md](./ROADMAP.md) for the full
milestone breakdown and what's explicitly out of scope.

## How it works (architecture)

```
                 +-----------------------------+
  Game code <--->|   Harmony patches           |
                 |   Modes (Companion/Warband) |
                 +-------------+---------------+
                               |
                 +-------------v---------------+
                 |       CoopSession           |
                 +------+-------------+--------+
                        |             |
                 +------v---+   +-----v-----+
                 | Dispatch |   |  Sync     |
                 +------+---+   +-----+-----+
                        |             |
                 +------v-------------v--------+
                 |    Packet (de)serialize     |
                 +-------------+---------------+
                               |
                 +-------------v---------------+
                 |    ITransport               |
                 |  (LiteNetLib UDP / Loopback)|
                 +-----------------------------+
```

- **Transport:** LiteNetLib UDP (the same transport BannerlordCoop uses).
  IP-based joins; no Steam dependency. NAT traversal is the user's problem
  for now — a public IP, port forward, or a VPN works. A loopback transport
  exists for in-process testing.
- **Discovery / invite:** none — host and client configure address + port
  in `coopconfig.json` and trigger via the main menu or F8/F9 hotkeys.
  A proper lobby UI lands in M1.
- **Protocol:** length-prefixed binary, byte packet IDs, little-endian.
  Versioned by `CoopConfig.ProtocolVersion`.
- **Authority:** host owns world state, client owns input intent.
- **Save compatibility:** coop-only data is stored in a sidecar file
  (`<save>.coop.json`) so the underlying save remains loadable in vanilla
  single-player.

## Requirements

- Mount & Blade II: Bannerlord, **version 1.2.12** (Steam build). The mod
  may compile against other 1.2.x patches by bumping
  `BannerlordReferenceAssembliesVersion` in `Directory.Build.props`, but
  only 1.2.12 is exercised right now.
- [Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006)
  module installed and loaded above this one.
- An open UDP path between the two PCs. Defaults to port 9000 — both
  players configure the address + port in `coopconfig.json`.

## Building

Prerequisites: .NET Framework 4.7.2 developer pack, .NET SDK 6.0+
(for the `dotnet` CLI), Windows or Linux (CI builds on Windows).

```sh
dotnet restore Bannerlords.Coop.sln
dotnet build -c Release Bannerlords.Coop.sln
```

The build drops a drop-in module layout under `dist/Bannerlords.Coop/`:

```
dist/Bannerlords.Coop/
  SubModule.xml
  coopconfig.sample.json
  README.txt
  bin/Win64_Shipping_Client/
    Bannerlords.Coop.dll
    LiteNetLib.dll
    Newtonsoft.Json.dll
    0Harmony.dll
```

## Installing into the game

Copy the whole `dist/Bannerlords.Coop/` folder into your Bannerlord
`Modules/` directory:

```
<Bannerlord>/Modules/Bannerlords.Coop/   <- this whole folder
```

Then copy `coopconfig.sample.json` → `coopconfig.json` next to
`SubModule.xml` and edit the `JoinAddress` / `ListenPort` / `ConnectionKey`
fields for your two-PC setup. Both players need their own
`coopconfig.json` with matching ports/keys.

Enable the module in the Bannerlord launcher's "Mods" tab. Order:
**Native → SandBoxCore → Sandbox → StoryMode → Bannerlord.Harmony →
Bannerlords.Coop**.

## Hosting / joining (M0 behavior)

1. Both players launch the game with the mod loaded.
2. Both players load a save OR start a new sandbox campaign. The host
   needs an active campaign for the troop attach to fire.
3. Host PC: press **F8** to start hosting (F8 again to stop).
4. Client PC: press **F9** to connect to the host (F9 again to
   disconnect). The host address comes from `coopconfig.json`.
5. The host should see a new Imperial Recruit troop in their party
   within a second of the handshake. Pause / time-speed changes now
   sync to the other player.

The main menu also has "Host coop game" / "Join coop game" entries,
which work the same way as the hotkeys but trigger before any campaign
is loaded — the soldier attach is then deferred until you load a save.
The hotkeys-in-campaign flow is the recommended path.

## Logging

The mod writes a timestamped log to the game's
`Modules/Bannerlords.Coop/Logs/` directory on every launch. Attach this
when filing a bug.

## Contributing

This is a hobby project; PRs welcome but expect slow review. Two things
that make a PR easy to merge:

1. **One milestone at a time.** Don't mix M0 plumbing with M3 mission
   sync — they need different review attention.
2. **Don't break the vanilla save format.** Anything coop-only goes in
   the sidecar.

The code is MIT-licensed (see `LICENSE` when added; until then, treat as
"unlicensed, contact author").

## Disclaimer

Not affiliated with TaleWorlds Entertainment. "Mount & Blade" and
"Bannerlord" are trademarks of TaleWorlds. Use at your own risk; modded
saves may break across game updates.
