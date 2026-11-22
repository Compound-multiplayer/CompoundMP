# Design: Add steam rich presence

## Options
- Call the native `steam_api64.dll` flat API (`SteamAPI_ISteamFriends_SetRichPresence` and the interface accessor) through P/Invoke. The accessor name carries an interface version, so it has to be matched to the Steam client in use.
- Or bundle a small Steamworks binding in the plugin. Either way it needs two Steam accounts on Windows to test, which the current setup cannot do.
