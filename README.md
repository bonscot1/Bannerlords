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
| M0 | Two PCs handshake over Steam, second player appears in host's roster as a soldier, pause/time-speed syncs both ways | in progress |
| M1 | Client sees a live mirror of the host's campaign map (read-only) | not started |
| M2 | Client's soldier becomes a real Hero with skills/perks/inventory | not started |
| M3 | Battles: client controls their hero on the host's battlefield | not started |
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
                 |  (Steam P2P / Loopback)     |
                 +-----------------------------+
```

- **Transport:** Steam Networking Messages (session-based P2P). NAT punch
  is handled by Steam; no port forwarding. A loopback transport exists for
  in-process testing.
- **Discovery / invite:** Steam Matchmaking lobby. Host creates a
  friends-only lobby; client accepts a Steam invite.
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
- Steam running on both PCs (the mod uses Steam P2P / lobby APIs).

## Building

Prerequisites: .NET Framework 4.7.2 developer pack, .NET SDK 6.0+
(for the `dotnet` CLI), Windows or Linux (CI builds on Windows).

```sh
dotnet restore Bannerlords.Coop.sln
dotnet build -c Release Bannerlords.Coop.sln
```

The compiled DLL lives at
`src/Bannerlords.Coop/bin/Release/Bannerlords.Coop.dll`.

## Installing into the game

Until we ship an installer, copy by hand:

```
<Bannerlord>/Modules/Bannerlords.Coop/
  SubModule.xml                    (from src/Bannerlords.Coop/SubModule.xml)
  bin/Win64_Shipping_Client/
    Bannerlords.Coop.dll
    0Harmony.dll
    Steamworks.NET.dll
```

Enable the module in the Bannerlord launcher's "Mods" tab. Order:
**Native → SandBoxCore → Sandbox → StoryMode → Bannerlord.Harmony →
Bannerlords.Coop**.

## Hosting / joining (M0 target behavior)

> Not yet implemented — this is the M0 acceptance test, kept here so
> readers know what "done" looks like.

1. Both players launch the game with the mod loaded.
2. Host: main menu → "Host coop game". A Steam friends-only lobby is
   created.
3. Host: invite the other player through the Steam overlay
   (Shift+Tab → invite).
4. Client: accepts the invite. Main menu → "Join coop game" finishes the
   handshake.
5. Host: starts a sandbox campaign normally.
6. Client's clock follows host's clock; client's name appears in host's
   party roster as a soldier.

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
