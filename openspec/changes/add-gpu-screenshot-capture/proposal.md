# Change: Add gpu screenshot capture

## Why
Under CrossOver the game renders but every frame comes back black (see below), so only layout drawings exist. The capture helper (`Shot.cs`, `DumpMenu`, `ScreenshotDir`) is written and runs end to end, saving PNGs.

## What Changes
- Run the existing scenario on a machine where the game renders (a Windows PC).
- Add a host-side camera that frames a remote avatar and an enemy for a multiplayer screenshot.
- Commit the PNGs under `docs/screenshots/` and replace the SVG previews in the README.

## Impact
- Affected specs: `screenshot-capture`
- Affected code: n/a
