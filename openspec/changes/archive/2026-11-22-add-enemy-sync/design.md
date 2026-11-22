# Design: Add enemy sync

## Decisions
- **Swap, don't rewrite.** Enemy code reads the player's `transform.position` in many places. Moving the host player for the duration of one enemy's update makes every enemy hunt the nearest player without touching the AI code. A depth counter handles nested base-class calls and the finalizer always restores the position.
- **Index ids.** Levels are deterministic, so the index into `Map.Enemies` is identical on every machine; a header mismatch makes the client skip the packet and log it.
