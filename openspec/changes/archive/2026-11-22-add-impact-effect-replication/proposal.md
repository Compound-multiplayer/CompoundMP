# Change: Add impact effect replication

## Why
Replicated bullets only flew and vanished: impacts, blood and decals existed only where the bullet hit.

## What Changes
- Harmony postfix on `BulletController.SpawnImpactObjects` sends an Effect event (bullet prefab, hit enemy index or -1, position, normal) from the machine that owns the bullet.
- Receivers run the bullet class's own impact code on a throwaway copy of the prefab, with the same enemy as target when its index is known.
- Replays never send events or deal damage; events share the 80-per-second limit with projectiles; clients relay through the host.

## Impact
- Affected specs: `effect-replication`
- Affected code: `src/ProjectileSync.cs`, `src/Patches.cs`
