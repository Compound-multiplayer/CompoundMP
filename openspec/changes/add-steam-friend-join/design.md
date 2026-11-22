# Design: Add steam friend join

## Questions to answer first
- Does the Steamworks.NET build in the game expose `ISteamFriends` and `ISteamNetworkingSockets` and is `SteamManager` initialised in the main menu?
- Steam P2P needs both players to own the game and be logged in, which is true for normal players but not for the headless tests.
- Raw UDP stays as the LAN / VPN / UPnP path. Steam is an additional transport behind the same `Transport` surface.

## Risk
Cannot be tested under the current CrossOver setup (no Steam client). Needs a Windows machine with two Steam accounts.
