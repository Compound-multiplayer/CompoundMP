# Change: Add steam connect link

## Why
Steam starts a game with `+connect host:port` when a `steam://run/615120//+connect%20host:port` link is opened, which needs no Steam API calls.

## What Changes
- The game started with `+connect host:port` (or `--connect`) joins that host.
- When the public address is known the mod logs a ready-to-share invite link.

## Impact
- Affected specs: `steam-connect-link`
- Affected code: `src/Plugin.cs`
