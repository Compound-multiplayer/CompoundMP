# Change: Verify in headset

## Why
No test used a headset or controllers. Poke accuracy, key spacing, text scale, avatar offset and projectile visibility are verified only by code-level checks (every control is found by the same overlap test the hand uses).

## What Changes
- Write a short playtest script covering host, join, keyboard entry, damage, projectiles, level change and leaving.
- Record results (headset, runtime, player count) in `docs/playtests/`.
- Fix whatever the playtest finds (key size and spacing, text scale, avatar offset).

## Impact
- Affected specs: `headset-verification`
- Affected code: `src/CoopMenu.cs`, `src/Plugin.cs (Avatar)`, `docs/playtests/`
