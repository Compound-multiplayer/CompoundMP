# Design: Add level transition sync

## Decisions
- Host leads (option 1 of the proposal). A ready-check could replace it later without changing the packet.
- The seed travels with the announcement so the client generates the same level even if the separate seed packet is late.
