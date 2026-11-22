# Change: Add lan discovery

## Why
Typing an IP in VR is slow. On a LAN the host can simply announce itself.

## What Changes
- A host broadcasts an announcement (name, port, players, password flag) every 2 s on each IPv4 interface; `LanDiscovery` and `HostName` config entries control it.
- A listener on UDP 27778 lists hosts heard in the last 6.5 s and ignores itself, other protocol versions and junk.
- The FIND GAMES page lists the hosts so one poke joins.

## Impact
- Affected specs: `lan-discovery`
- Affected code: `src/LanDiscovery.cs`, `src/Plugin.cs`
