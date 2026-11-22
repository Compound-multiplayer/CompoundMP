# COMPOUND co-op mod (BepInEx 5)

## Requirements and downloads

Built and tested against COMPOUND Steam build 9190101 (Unity 2019.3.10f1, Mono, Windows x64).

| What | Version | Link |
|---|---|---|
| COMPOUND (Steam) | app 615120 | https://store.steampowered.com/app/615120/ |
| BepInEx 5 (win_x64) | **5.4.23.3** (do not use 6.x/IL2CPP builds; this is a Mono game) | release page https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.3 , zip https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.3/BepInEx_win_x64_5.4.23.3.zip |
| HarmonyX | bundled inside BepInEx 5 (`BepInEx/core/0Harmony.dll`), nothing to install | https://github.com/BepInEx/HarmonyX |
| This plugin | `src/bin/Release/net472/CompoundMP.dll` | built from `src/` (see below) |

Install: unzip BepInEx into the game folder (next to `COMPOUND.exe`), run the game once so `BepInEx/plugins` and `BepInEx/config` exist,
then copy `CompoundMP.dll` into `BepInEx/plugins`. Everyone playing needs the same game build and the same plugin version.

Building from source (optional): .NET SDK 6 or newer https://dotnet.microsoft.com/download , then
`dotnet build src -c Release -p:GameDir="<your COMPOUND folder>"` (or set the `COMPOUND_DIR` environment variable). The game folder must already have BepInEx installed, because the build references its DLLs and the game's own assemblies. The
`Microsoft.NETFramework.ReferenceAssemblies` package (https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies/1.0.3)
is restored automatically.

Playing over the internet without port forwarding or UPnP: a virtual LAN works well, e.g. Tailscale https://tailscale.com/download
or ZeroTier https://www.zerotier.com/download/ ; use the host's VPN address as the join address.

Test setup only (not needed to play): CrossOver https://www.codeweavers.com/crossover on macOS, with the bottle registry override
`winhttp=native,builtin` so the BepInEx loader (`winhttp.dll`) is used.

Install: BepInEx 5.4.23.3 win_x64 + `BepInEx/plugins/CompoundMP.dll` in the game folder.
(Under Wine/CrossOver set `WINEDLLOVERRIDES="winhttp=n,b"`.)

Keys: F8 host, F9 join (`BepInEx/config/com.compoundmp.coop.cfg` -> JoinAddress), F10 leave. UDP port 27777 (host must open it).

Usage: join from the headquarters *before* starting a run so the client picks up the host's seed.

What is synced
- Level seed (identical maps), player head/hands as coloured ghosts.
- Enemies: host runs AI; clients freeze AI and follow host position/death. Enemies hunt the nearest player.
- Enemy bullets hitting a ghost damage the real player on their machine. Client shots at enemies are applied on the host.

Known limits (each has a proposed spec in `openspec/changes/`):
- each player changes level at their own elevator (`add-level-transition-sync`)
- impact effects, hit reactions on living enemies and shot sounds are not replicated (`add-impact-effect-replication`)
- hostnames and the join password can only be set in the config file, not in VR (`add-vr-text-entry`)
- the join password is sent in clear text and packets are not authenticated (`add-session-encryption`)
- UPnP does not help behind carrier-grade NAT (`add-nat-fallbacks`)
- nothing has been tested with a real headset (`verify-in-headset`)

Tests: `cd tests && dotnet run` exercises the wire protocol/transport over loopback UDP and UPnP against a fake router (no game needed).

Verified (headless, two real game instances under CrossOver, map01_sewers loaded on both):
- plugin loads; all 22 Harmony patches apply; join handshake; seed delivery
- both instances generate the identical level (same enemies, same positions); client freezes its AI and follows the host
- client damage to an enemy is forwarded to the host; the death syncs back
- live fight: with the client's ghost 3m from an enemy, host enemies targeted it, their bullets (EnemyBullet) hit the ghost hitbox,
  the host forwarded each hit, and the client's real player health dropped 5 -> 0 (game's own TakeDamage, not immortal)
Not tested: real VR input/headset (the headless client's pose was faked at a fixed spot).
Debug config: `[Debug] AutoStart = host|join`, `LoadMap = <scene>`, `Combat = true`.

## Internet play, UPnP and the VR menu (0.3.0)

- **VR menu**: the game's own wrist menu (the one you poke with a finger and select with the trigger) has a new **CO-OP MULTIPLAYER** line
  between PERFORMANCE OPTIONS and END RUN. It opens a page with HOST GAME, a UPNP on/off toggle, status lines (players connected, your
  public IP to give friends), a poke-able keypad (0-9 . :) to type the host's address (`ip` or `ip:port`), DEL / CLEAR, JOIN GAME and
  DISCONNECT. No keyboard needed. F8/F9/F10 still work as optional shortcuts.
- **UPnP**: when hosting with `UseUPnP` on (default) the mod discovers the router over SSDP, maps UDP `Port` to this PC, reads the public
  IP, shows it in the menu, and removes the mapping when you stop/quit. Config `JoinAddress` may also be a hostname.
- Verified headless in the real game: the menu entry appears on the real BaseMenu prefab, the page opens through MenuButton, every option
  has a collider on the Menu layer that MenuSelector's overlap test finds, keypad/DEL/UPnP toggle/HOST/DISCONNECT/BACK work and the status
  labels update. UPnP is verified against a fake router in `tests/`.
- Not verified: actually poking it with VR controllers (no headset here), how the text looks/is spaced in the headset, and UPnP against a
  real router. CGNAT / double NAT / UPnP-disabled routers still need a VPN (Tailscale, ZeroTier) or manual port forwarding.

## Screenshots

Real in-game screenshots could not be produced: under CrossOver the game gets no GPU (`Renderer: Null Device`, and `-force-glcore`
fails with "InitializeEngineGraphics failed"). `previews/*.svg` are layout previews drawn from the menu's real positions and strings
(not game captures). `Shot.cs` + the `ScreenshotDir` debug option will save real PNGs on a machine where the game can render.

## Projectiles (0.3.0)

Projectiles are simulated only where they are fired (host: enemy fire and its own guns; each client: its own guns). The spawn is sent
(`Projectile`: prefab name, position, velocity, rotation) and every other machine plays a visual-only clone of the same prefab (bullet
logic and colliders off, removed when it hits level geometry or after 8 s). Hitscan shots (lasers etc.) send a `Trail` event, and the
receiver runs the bullet class's own `CreateTrail` on a throwaway copy. Rate-limited to 80 events/s. Clients relay through the host.
- Verified headless (host + client, real game): 8 EnemyBullet spawns on the host arrived as 8 replicas on the client, flew at the
  right speed (~9 m/s) and vanished after 1-2 s; 1 hitscan trail (`Laser`) sent and replayed; no errors.
- Not verified: how the replicas look in a headset, and player-gun projectiles from a client (only host-fired enemy bullets and one
  host hitscan shot were exercised). Impact effects/sounds of replicas are not reproduced.

## Six-player test (0.3.0)

One host + five clients, six real game instances under CrossOver (clients run from APFS clones of the game folder so each has its own
BepInEx log/config; BepInEx 5 only keeps 5 log files per folder, and a shared config raced on `AutoStart`). Result after ~2 min on map01_sewers:
- host `peers=5 avatars=5`; every client `peers=5 avatars=5` (the host relays everyone's pose to everyone else)
- all six on the same seed, same enemy count, clients' AI frozen (`frozen=5`)
- host sent 60 projectiles; clients received 54-63 each (loss is plain UDP; no errors); the one hitscan trail reached 3 of 5 clients
  (the other two were not yet in the map when it was fired)
- five ghosts placed at different spots around an enemy: all 5 enemy hits went to the nearest ghost and only that client's health
  dropped (5 -> 0); the other four were untouched
- no exceptions or bad packets in any log
Not measured: frame rate / bandwidth with six players on real networks, and six real VR headsets.
`AutoStart` is no longer written back to the config by the plugin (it raced between instances).

## Connection handling (0.3.0)

- Damage events (`EnemyDamage`, `PlayerDamage`) are reliable: sequence number, ack, re-send every 200 ms up to 10 times, duplicates applied once.
- Joining: the client repeats Hello every second until the host answers Welcome (gives up after 15 s). Optional `Password` in
  `[Network]`; the same value must be set on host and joiner. A wrong password gets a Reject and the client stops with a message.
- A host drops a client silent for 10 s; a client leaves when the host is silent for 15 s.
- Verified in the real game (two instances): wrong password rejected, right password joined and received damage 5 -> 0 with every hit
  applied exactly once, a killed client was timed out by the host, a killed host was noticed by the client.

## Repository layout

- `src/` plugin sources (`dotnet build src -c Release -p:GameDir=...`)
- `tests/` loopback, reliable-delivery and UPnP tests (`dotnet run --project tests`)
- `openspec/` specs: current requirements, archived completed changes and proposed changes for the open items
- `.github/workflows/ci.yml` conventional-commit check, tests, plugin build and release. The plugin build needs the repository secret
  `GAME_ASSEMBLIES_URL` (a private download of a zip containing `COMPOUND_Data/Managed/*.dll`) because the game's assemblies cannot be
  committed; without it the job is skipped with a notice and the tests still run.
- Commits follow Conventional Commits.
