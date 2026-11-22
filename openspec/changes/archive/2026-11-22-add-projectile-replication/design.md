# Design: Add projectile replication

## Decisions
- **Prefab lookup by name** (`Resources.FindObjectsOfTypeAll`), cached. A miss is retried every 2 s because the prefab only loads once the client's own level exists.
- **Replay the game's own trail code** instead of re-implementing laser/trail styles.
- Copies never apply damage; damage already flows through `damage-routing`.
