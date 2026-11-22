# Change: Add headless test driver

## Why
Verification on real game code needs scripted scenarios: no GPU and no VR hardware are available on the development machine.

## What Changes
- Debug config entries `AutoStart` (host|join), `LoadMap`, `Combat`, `DumpMenu`, `ScreenshotDir`.
- Scripted combat scenario: the client stands 3 m from an enemy at a pose derived from its id, the host wakes enemy 0 and fires a hitscan shot.
- Menu driver that drives the real wrist-menu path and logs every control.
- `Shot` helper that saves PNGs when a GPU exists.

## Impact
- Affected specs: `headless-testing`
- Affected code: `src/Plugin.cs`, `src/MenuDump.cs`, `src/Shot.cs`
