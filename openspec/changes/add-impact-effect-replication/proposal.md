# Change: Add impact effect replication

## Why
Replicated bullets only fly and vanish. Impacts, enemy hit reactions (visible on clients only on death) and shot sounds are missing, which makes the other players' fights feel silent and weightless.

## What Changes
- Hook the places that spawn effects (`BulletController.SpawnImpactObjects`, the pooled hit and flash effects, explosions) and send an Effect event: effect id, position, normal, optional enemy index.
- Receivers replay the same prefab or pooled object through the game's own code path; effects are never allowed to deal damage.
- Cap effect events per second and drop the oldest first.
- Sounds that belong to a replayed prefab come along with it; per-enemy footsteps and ambience stay out of scope.

## Impact
- Affected specs: `effect-replication`
- Affected code: n/a
