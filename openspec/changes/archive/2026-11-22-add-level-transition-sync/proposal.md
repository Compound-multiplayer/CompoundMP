# Change: Add level transition sync

## Why
Each player changed level at their own elevator, so players ended up in different levels and enemy packets were skipped as mismatched.

## What Changes
- When the host's `NextLevelTrigger` changes level it announces the new level and its seed reliably.
- Clients run their own level trigger towards that level (the same fade and save handling as the elevator) and fall back to loading the scene directly after 8 s.
- A client cannot leave a level the host is still in; it is told that only the host can start the next level.
- In the chaos mutator the client uses the host's chosen level instead of its own random roll.
- Only runs and the headquarters are followed (`map*` scenes), not practice or tutorial areas.

## Impact
- Affected specs: `level-transition-sync`
- Affected code: `src/LevelSync.cs`, `src/Plugin.cs`
