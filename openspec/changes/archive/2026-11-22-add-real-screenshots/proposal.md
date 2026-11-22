# Change: Add real screenshots

## Why
Only layout drawings existed, and the first capture attempts came back black.

## What Changes
- `Shot` renders a camera into a texture (menu cameras are orthographic and see only the Menu layer; scene cameras are normal perspective cameras) and saves PNGs; `DumpMenu` + `ScreenshotDir` and `MultiplayerShot` drive it.
- Run with `-force-d3d11-warp` under CrossOver, which gives the game a Direct3D device.
- Screenshots of the main wrist menu, the co-op page (idle and hosting), the host list and the keyboard page, plus the fight seen from the host (the other player's avatar in front of the enemy) and from a client (replicated bullets flying at the camera), are in `docs/screenshots/`.
- The screenshots exposed real layout bugs, now fixed: the left button column overlapped the keypad, the right column overlapped the keypad's last column, and long status lines ran off the menu.

## Impact
- Affected specs: `screenshot-capture`
- Affected code: `src/Shot.cs`, `src/MenuDump.cs`, `src/Plugin.cs`, `docs/screenshots/`
