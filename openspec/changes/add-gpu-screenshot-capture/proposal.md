# Change: Add gpu screenshot capture

## Why
Under CrossOver the game gets no GPU, so only layout drawings exist. `Shot.cs` is written but has never run.

## What Changes
- Run the existing `DumpMenu` + `ScreenshotDir` scenario on a machine where the game renders (Windows PC).
- Add a host-side camera that frames a remote avatar and an enemy for a multiplayer screenshot.
- Optionally call Steam's screenshot function so players can use the overlay shortcut.

## Impact
- Affected specs: `screenshot-capture`
- Affected code: n/a
