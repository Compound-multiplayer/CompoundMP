# Change: Add steam friend join

## Why
Typing an IP in VR is awkward and exposes it. The game already ships Steamworks.NET, so Steam friends, rich presence and Steam's relay could remove addresses and NAT problems entirely.

## What Changes
- Publish the host's join information through Steam rich presence (`connect`).
- Handle `GameRichPresenceJoinRequested` so "Join Game" in the Steam friends list starts the join.
- Optionally carry packets over Steam networking (relay) instead of raw UDP.

## Impact
- Affected specs: `steam-friend-join`
- Affected code: n/a
