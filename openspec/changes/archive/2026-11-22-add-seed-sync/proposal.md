# Change: Add seed sync

## Why
The game generates levels deterministically from `GameController.RandomSeed + levelNumber`. Players on the same seed get the same level, so only the seed has to be shared.

## What Changes
- Harmony prefix on `MapGenerator.CreateMap` forces the host's seed on a client (`RandomSeedEnabled = true`).
- Harmony postfix on the host sends its seed to every client after it generates a map.
- Host replies to a Hello with the current seed.

## Impact
- Affected specs: `level-sync`
- Affected code: `src/Patches.cs (CreateMapPatch)`, `src/Plugin.cs`
