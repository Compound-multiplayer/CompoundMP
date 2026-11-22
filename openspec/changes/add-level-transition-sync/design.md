# Design: Add level transition sync

## Options
1. **Host leads.** The host's elevator ends the level for everyone; clients fade out and load the same level. Simple and fits the host-authoritative model.
2. **Ready check.** Anyone can start the change but it only happens when all players are in an elevator.

Recommend option 1 first; option 2 can replace it later without changing the packet.

## Notes
- The seed is already pushed on map creation (`level-sync`); the new packet only has to say "load the next level now".
