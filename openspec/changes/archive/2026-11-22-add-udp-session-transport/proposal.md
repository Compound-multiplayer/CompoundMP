# Change: Add udp session transport

## Why
COMPOUND has no networking. A co-op mod needs a transport that works on a LAN, behind a port forward or over a VPN without extra services.

## What Changes
- Add a UDP transport (`Transport`) with a background receive thread feeding a queue read on the Unity main thread.
- Define the packet format `[type byte][body]` and the packet types Hello, Seed, State, Bye, Enemies, EnemyDamage, PlayerDamage.
- Host relays each client's State packet to the other clients.
- Add keyboard shortcuts (F8 host, F9 join, F10 leave) and the `Port` / `JoinAddress` config entries.

## Impact
- Affected specs: `session-transport`
- Affected code: `src/Protocol.cs`, `src/Plugin.cs`
