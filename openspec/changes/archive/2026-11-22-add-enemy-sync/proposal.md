# Change: Add enemy sync

## Why
Two independently simulated enemy populations would disagree about positions, health and targets.

## What Changes
- Use the index into `Map.Enemies` as the network id, with a header of map name and enemy count as a sanity check.
- Host streams alive/position/yaw at 10 Hz in chunks of 40 enemies (each packet under the MTU).
- Clients disable `EnemyController` and `MapNavigator`, lerp to host data and kill enemies the host reports dead.
- Client damage to an enemy is forwarded to the host, which applies it.
- Harmony prefix/finalizer on every `EnemyController` subclass `Update`/`FixedUpdate` (22 methods) temporarily moves the host player's position to the nearest living player or avatar.

## Impact
- Affected specs: `enemy-sync`
- Affected code: `src/EnemySync.cs`, `src/Patches.cs (EnemyTargetPatch)`
