# Change: Add hand poke test

## Why
Calling `Select()` directly skips the game's own `MenuSelector` (overlap test, highlight, trigger), which is the part a headset player actually uses.

## What Changes
- The menu driver moves the right-hand `MenuSelector` onto a key and pulls a simulated trigger by patching `SteamVRWrapper.JustTriggerPulled` for that hand only, with the hand-pose calls that need hardware disabled while the test runs (all inert unless the test is active).
- The hand that does not carry the wrist menu is used, because moving the left controller would carry the menu away.

## Impact
- Affected specs: `poke-simulation`
- Affected code: `src/PokeTest.cs`, `src/MenuDump.cs`
