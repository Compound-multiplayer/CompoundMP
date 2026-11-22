# Change: Add run notices

## Why
Deaths and departures were silent, and a client kept running after the host stopped.

## What Changes
- Toast when a player goes down and when everyone is down; toasts when players join or lose connection.
- A client leaves with "The host left the game" when the host sends Bye.
- Each player's death is handled by the game's own flow (their run ends); the mod does not force a shared game over.

## Impact
- Affected specs: `run-outcome-sync`
- Affected code: `src/Plugin.cs`
