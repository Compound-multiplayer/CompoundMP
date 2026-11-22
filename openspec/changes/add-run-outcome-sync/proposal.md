# Change: Add run outcome sync

## Why
Death and game-over are per machine today. If the host dies enemies keep hunting the nearest live avatar, but nothing says whether the run ends for everyone, whether dead players spectate, or what happens when the host quits.

## What Changes
- Decide and implement: run ends when all players are dead; dead players keep their avatar marked dead.
- Host leaving ends the session for everyone with a clear message (host migration is out of scope).
- Show a toast when a player dies or leaves.

## Impact
- Affected specs: `run-outcome-sync`
- Affected code: n/a
