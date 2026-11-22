# Change: Add projectile replication

## Why
Enemy AI only runs on the host and each player's gun only fires on their own machine, so other machines never spawned those projectiles.

## What Changes
- Send a Projectile event (prefab name, position, velocity, rotation) when a non-hitscan `BulletController` starts, from whichever machine fired it.
- Receivers instantiate the same prefab as a visual-only copy: bullet logic and colliders disabled, driven by the prefab's own rigidbody.
- Copies are removed when they hit level geometry, after 8 s or 600 m.
- Hitscan shots send a Trail event; the receiver runs the bullet class's own `CreateTrail` on a throwaway copy.
- Rate limit of 80 events per second; clients relay through the host.

## Impact
- Affected specs: `projectile-replication`
- Affected code: `src/ProjectileSync.cs`, `src/Patches.cs`
