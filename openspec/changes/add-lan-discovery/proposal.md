# Change: Add lan discovery

## Why
Typing an IP in VR is slow. On a LAN the host can simply announce itself.

## What Changes
- Host broadcasts a small announcement (name, port, player count, whether a password is needed) on the LAN.
- The co-op page lists discovered hosts to poke.
- Discovery is opt-in per host and ignores hosts on other game versions.

## Impact
- Affected specs: `lan-discovery`
- Affected code: n/a
