# Change: Verify in headset

## Why
No test used a headset or controllers. The wrist-menu spacing, finger-poke accuracy, avatar scale and projectile visibility are only verified by code-level checks.

## What Changes
- Write a short playtest script covering host, join, keypad entry, damage, projectiles and leaving.
- Record results (headset, runtime, player count) in `docs/playtests/`.
- Fix whatever the playtest finds (key size and spacing, text scale, avatar offset).

## Impact
- Affected specs: `headset-verification`
- Affected code: `src/CoopMenu.cs`, `src/Plugin.cs (Avatar)`, `docs/playtests/`
