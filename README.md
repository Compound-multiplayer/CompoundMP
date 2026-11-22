# COMPOUND co-op mod (BepInEx 5)

Co-op multiplayer for COMPOUND (Unity 2019.3 Mono VR game). One player hosts, up to five more join. Everyone plays the same level
from a shared seed, enemies run on the host, and players see each other as avatars. Version 0.4.0, network protocol 2.

## Requirements and downloads

Built and tested against COMPOUND Steam build 9190101 (Unity 2019.3.10f1, Mono, Windows x64).

| What | Version | Link |
|---|---|---|
| COMPOUND (Steam) | app 615120 | https://store.steampowered.com/app/615120/ |
| BepInEx 5 (win_x64) | **5.4.23.3** (do not use 6.x/IL2CPP builds; this is a Mono game) | release page https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.3 , zip https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.3/BepInEx_win_x64_5.4.23.3.zip |
| HarmonyX | bundled inside BepInEx 5 (`BepInEx/core/0Harmony.dll`), nothing to install | https://github.com/BepInEx/HarmonyX |
| This plugin | `CompoundMP.dll` (release asset, or built from `src/`) | see below |

Install: unzip BepInEx into the game folder (next to `COMPOUND.exe`), run the game once so `BepInEx/plugins` and `BepInEx/config` exist,
then copy `CompoundMP.dll` into `BepInEx/plugins`. Everyone playing needs the same game build and the same plugin version.

## Playing

Open the wrist menu (the one you poke with a finger and select with the trigger) and choose **CO-OP MULTIPLAYER**, between PERFORMANCE
OPTIONS and END RUN. Everything can be done in VR:

- **HOST GAME** starts hosting; **UPNP: ON/OFF** toggles automatic port opening. The page shows the player count and your public address.
- **Join**: type the host's address on the keypad (`ip` or `ip:port`) and select **JOIN GAME**, or open **FIND GAMES** to pick a host
  on your network or one you joined before. **KEYBOARD** types hostnames (letters, SHIFT for capitals). **PASSWORD** sets the shared password.
- **DISCONNECT** leaves. Keyboard shortcuts F8 (host), F9 (join), F10 (leave) still work as optional extras.
- The host starts levels: when the host changes level every client follows with the same seed, and a client cannot leave a level the
  host is still in. Join from the headquarters and let the host start the run.
- Steam links: opening `steam://run/615120//+connect%20<host>:<port>` starts the game and joins that host (the mod logs a ready-made link
  when your public address is known). The game started with `+connect host:port` does the same.

Config (`BepInEx/config/com.compoundmp.coop.cfg`): `Port` (UDP 27777), `JoinAddress`, `Password`, `UseUPnP`, `LanDiscovery`, `HostName`,
`RecentHosts`; the `[Debug]` section holds test drivers and is off by default.

### Getting through the internet

1. **UPnP** (default): the mod asks the router to forward the UDP port and shows your public IP.
2. **NAT-PMP**: used automatically when UPnP finds no gateway.
3. **Carrier-grade NAT or double NAT** cannot be fixed by the mod; the status says so. Use a virtual LAN such as Tailscale
   https://tailscale.com/download or ZeroTier https://www.zerotier.com/download/ and join the host's VPN address, or forward the port by hand.

The password (if set) is never sent: both sides prove they know it, and every later packet carries an HMAC tag, so strangers cannot join
or inject damage. Without a password the session is open. Traffic is authenticated, not encrypted.

## What is synced

- Level seed (identical maps), player head/hands as avatars with a hitbox; deaths and departures are announced.
- Enemies: the host runs the AI; clients freeze theirs and follow. Enemies hunt the nearest player. Client damage to enemies is applied on the host.
- Enemy bullets hitting an avatar damage the real player on their machine through the game's own damage code.
- Projectiles (enemy and player), hitscan trails, impact effects and hit reactions are replayed on the other machines as harmless visuals.
- Level changes, led by the host.
- Damage events are reliable (sequence number, ack, resend, duplicates applied once); joining is retried; silent peers are dropped
  (host after 10 s, client after 15 s); a client leaves when the host leaves.

Known limits (open proposals in `openspec/changes/`):
- nothing has been tested with a real VR headset (`verify-in-headset`)
- the Steam friends-list "Join Game" button is implemented but could not be tried without a running Steam client (`add-steam-rich-presence`)

## Verification

Run headless under CrossOver (macOS) with real game instances and scripted scenarios, plus unit tests that need no game.

Unit tests (`dotnet run --project tests`): wire protocol and transport over loopback UDP, reliable delivery under loss, packet
authentication (tagged, forged, wrong key), UPnP and NAT-PMP against fake routers, LAN discovery, carrier-grade NAT detection.

In the real game:
- identical levels and enemy counts on host and clients; client AI frozen; client damage reaches the host
- host enemies target the nearest of five avatars; only that player's health drops (5 -> 0, game's own damage path)
- six instances (host + five clients, password on): all see five other avatars, follow a host level change to the same level with the
  same seed (23 enemies on each), receive the host's bullets, hitscan trail and impact effects, and all five notice the host leaving
- a client's own gun shot reaches the host and another client, together with its impact
- wrong password rejected; killed client timed out by the host; killed host noticed by the clients
- UPnP and NAT-PMP mapped and removed a port on the development router (UPnP is also tested against a fake router)
- the wrist menu: every control, including all keyboard keys, is found by the same overlap test the hand uses; the keypad, keyboard
  (SHIFT, masked password), recent hosts and LAN host list all work through the real menu code; the hand's own `MenuSelector` was moved
  onto keys with a simulated trigger and typed `7 2 . 1 DEL 9` correctly (the real selection path, minus a physical controller)
- a second instance on the real network listed the host through LAN discovery
- measured host upload with five clients in combat: about 54 kB/s (0.44 Mbit/s); State dominates (45 kB/s), then Enemies (17 kB/s)

The CI workflow was run for real in Linux containers with `act` (jobs `commits`, `test` and `compile` succeeded; the `release`
job needs a tag, so it was not run). CI needs no game files.

Not verified: a physical headset and controllers (finger accuracy, text size, avatar scale), gameplay on a real internet connection with
several distinct machines, the Steam friends-list button (no Steam client in the test setup), and a run on github.com itself.

## Screenshots

Captured from the running game (headless-capable test setup, `-force-d3d11-warp` under CrossOver):

| | |
|---|---|
| ![Wrist menu with the CO-OP MULTIPLAYER line](docs/screenshots/menu_1_main.png) | ![Co-op page while hosting](docs/screenshots/menu_3_hosting.png) |
| ![FIND GAMES page](docs/screenshots/menu_4_find_games.png) | ![Keyboard page](docs/screenshots/menu_5_keyboard.png) |
| ![Fight seen from the host: the other player's avatar in front of the enemy](docs/screenshots/multiplayer_host_view.png) | ![The same fight from a client: replicated bullets flying at the camera](docs/screenshots/multiplayer_client_view.png) |

Left-to-right, top-to-bottom: the wrist menu with the new line; the co-op page while hosting; the host list; the keyboard; the host's view
(the avatar of the other player, a cube head and two hands, stands in front of the enemy); a client's view of the same fight.
`Shot.cs` with the `DumpMenu`, `MultiplayerShot` and `ScreenshotDir` debug options produces them.

## Building

.NET SDK 6 or newer https://dotnet.microsoft.com/download :

- `dotnet build src -c Release -p:GameDir="<your COMPOUND folder>"` (or set `COMPOUND_DIR`); the folder must have BepInEx installed.
  The `Microsoft.NETFramework.ReferenceAssemblies` package is restored automatically.
- `./ci/build-with-stubs.sh` compiles against checked-in signature stubs and the public Unity reference assemblies, no game needed.
- `dotnet run --project tests` runs the unit tests.

## Repository layout

- `src/` plugin sources; `tests/` unit tests; `ci/` stub assemblies and build script; `docs/screenshots/` game screenshots
- `openspec/` specs: current requirements (`specs/`), 25 archived completed changes (`changes/archive/`) and the open proposals (`changes/`: a headset playtest and a Steam test)
- `.github/workflows/ci.yml`: conventional-commit check on pull requests; unit tests; compile against stubs; a release when a `v*` tag is pushed. The game's assemblies are copyrighted and never committed.
- Commits follow Conventional Commits.

Test setup only (not needed to play): CrossOver https://www.codeweavers.com/crossover on macOS with the bottle override
`winhttp=native,builtin`; instances beyond the second run from copies of the game folder, because BepInEx 5 keeps at most five log files
per folder and instances share one config.
