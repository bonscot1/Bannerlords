Bannerlords Coop — drop-in module
=================================

A two-player cooperative campaign mod for Mount & Blade II: Bannerlord 1.2.12.
M0 (current): two PCs connect over UDP, the client appears as a placeholder
troop in the host's party, and pause / time-speed changes synchronize.


Install
-------
1. Build the mod: from the repository root run

       dotnet build src/Bannerlords.Coop/Bannerlords.Coop.csproj -c Release

   This produces `dist/Bannerlords.Coop/bin/Win64_Shipping_Client/Bannerlords.Coop.dll`
   plus the LiteNetLib, Newtonsoft.Json, and 0Harmony DLLs alongside it.

2. Copy the whole `dist/Bannerlords.Coop/` folder into your Bannerlord modules
   directory:

       <Steam>/steamapps/common/Mount & Blade II Bannerlord/Modules/Bannerlords.Coop/

3. Copy `coopconfig.sample.json` to `coopconfig.json` (next to SubModule.xml)
   and edit it. The fields that matter for a two-PC test:

       ListenPort      - host's UDP port (default 9000)
       JoinAddress     - host's reachable IP (default 127.0.0.1 — loopback only)
       JoinPort        - host's port from the client's side
       ConnectionKey   - must match on both PCs

   Both PCs need their own copy; only the addresses differ. The host PC's
   `JoinAddress` can stay at the default (it doesn't connect anywhere); the
   client PC needs `JoinAddress` set to the host's LAN / public IP.

4. Launch Bannerlord, open the Mods launcher window, and tick "Bannerlords Coop".
   It depends on Bannerlord.Harmony — make sure that's installed and ticked above
   us in the load order.


Use (M0)
--------
1. Start the game on both PCs.
2. Load a save OR start a new campaign on BOTH PCs first. The host needs an
   active campaign for the troop attach to fire.
3. On the host PC: press F8 (toggles host on / off).
4. On the client PC: press F9 (toggles join / disconnect).
5. The host should see a new "Imperial Recruit" troop in MainParty within a
   second of the handshake completing. Pause / time-speed changes on either
   side now sync to the other.

The main menu also has "Host coop game" / "Join coop game" entries; they work
the same as F8/F9 but trigger before any campaign is loaded, which means the
soldier attach is deferred until you load a save. The hotkeys-in-campaign
path is the recommended flow for the M0 exit criterion.


Troubleshooting
---------------
- Mod doesn't appear in the launcher: confirm `SubModule.xml` is at the
  module root (`Modules/Bannerlords.Coop/SubModule.xml`), not in `bin/`.
- Mod loads but nothing happens: check `Modules/Bannerlords.Coop/Logs/` —
  every session decision is logged there.
- "no active campaign" in the log when hosting: you triggered host before
  loading a save. The attach is queued and will run as soon as the campaign
  loads.
- Client can't connect: confirm `ConnectionKey` matches, the host's
  `ListenPort` is reachable from the client (firewall + NAT — UDP), and the
  client's `JoinAddress` points at the host's public IP.
- Y/N hotkeys do nothing during a vote: known limitation of the M0 text-mode
  responder. A proper popup lands in M0.7.


License + status
----------------
Alpha. Saves made on one alpha may not load on a newer alpha until M2 ships
sidecar versioning. No anti-cheat — the host has full authority.

See `ROADMAP.md` in the repository root for milestone-by-milestone scope.
