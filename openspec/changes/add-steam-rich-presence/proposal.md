# Change: Add steam rich presence

## Why
`+connect` links work (see `add-steam-connect-link`) but the friends list button needs rich presence, which the mod cannot publish: the game ships Steamworks.NET without the friends API.

## What Changes
- Publish the host's join information through Steam rich presence (`connect`).
- Handle the join request when the game is already running.

## Impact
- Affected specs: `steam-rich-presence`
- Affected code: n/a
