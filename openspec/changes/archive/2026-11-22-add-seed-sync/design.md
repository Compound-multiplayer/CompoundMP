# Design: Add seed sync

## Decisions
- Seed is applied the next time a map is created on the client, so players join from the headquarters and start a new run.
- Enemy spawn order depends only on the seed, which is what `enemy-sync` relies on for its index-based ids.
