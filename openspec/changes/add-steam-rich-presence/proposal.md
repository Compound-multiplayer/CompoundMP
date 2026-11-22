# Change: Add steam rich presence

## Why
The mod already publishes the host's address as Steam rich presence (`connect`) while hosting and handles `GameRichPresenceJoinRequested` while running, through the game's own Steamworks.NET (`SteamJoin.cs`); a game started from the button receives `+connect` (see `add-steam-connect-link`). None of this could be exercised here: Steam is not running under the test setup, so `SteamManager.Initialized` is false and the calls are skipped.

## What Changes
- Test with two Steam accounts on Windows: the host sees `Steam rich presence connect=...` in the log, the friend sees Join Game, and pressing it joins (game not running and game running).
- Fix whatever that shows (for example which address to publish when UPnP and NAT-PMP both fail).

## Impact
- Affected specs: `steam-rich-presence`
- Affected code: n/a
