# Project Context

## Purpose
A BepInEx 5 co-op multiplayer mod for COMPOUND (Unity 2019.3 Mono VR game). One player hosts, others join over UDP; levels are generated from a shared seed, enemies run on the host, and players see each other as avatars.

## Tech Stack
- C# (net472 plugin, LangVersion 10), BepInEx 5.4.23.3, HarmonyX patches, Unity 2019.3.10f1 (Mono, OpenVR, Steamworks.NET)
- Plain UDP, Unity-free `Protocol.cs` and `Upnp.cs` so they can be tested with .NET 8
- GitHub Actions for CI

## Project Conventions

### Code Style
Match the surrounding code: tabs, `internal` over `public`, minimal comments that explain why. Networking code stays free of UnityEngine.

### Architecture Patterns
- Host-authoritative enemies; clients mirror them. Star topology through the host.
- The game's own code is patched with Harmony prefixes/postfixes rather than reimplemented.
- Debug and test drivers live behind `[Debug]` config entries and default to off.

### Testing Strategy
- `tests/` (no game needed): loopback UDP, reliable delivery under loss, UPnP against a fake router.
- Real-game checks run headless under CrossOver with two to six instances (see `headless-testing`).
- Headset testing is still open (`verify-in-headset`).

### Git Workflow
Conventional Commits (`feat`, `fix`, `docs`, `test`, `chore`, `refactor`, `perf`, `ci`, `build`, `style`, `revert`), checked on pull requests. Commit trailers from the tooling are kept.

## Domain Context
Levels are generated deterministically from `GameController.RandomSeed + levelNumber`, so the index into `Map.Enemies` is a stable network id. The wrist menu is built from `MenuOption`/`ASCIIText` objects on the `Menu` layer, selected with `MenuSelector`'s sphere overlap and the trigger.

## Important Constraints
- The game's assemblies are copyrighted: never commit them or decompiled source.
- The plugin only builds against a local game install (or the CI secret).
- BepInEx keeps at most five log files per folder; several test instances must use separate game-folder copies.

## External Dependencies
- BepInEx 5.4.23.3 (win_x64), HarmonyX (bundled)
- Optional: UPnP-capable router, Tailscale/ZeroTier for VPN play
