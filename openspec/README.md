# OpenSpec

Specs for the co-op mod, in the [OpenSpec](https://github.com/Fission-AI/OpenSpec) layout.

- `specs/` current requirements, one folder per capability.
- `changes/archive/2026-11-22-<change>/` completed changes: proposal, tasks (all done), design notes and the spec delta that produced the current spec.
- `changes/<change>/` **proposed** changes that are not implemented yet. Each has a proposal, a task list and a proposed spec delta. When one is finished, move it to `changes/archive/<date>-<change>/` and merge its delta into `specs/`.

## Open proposals
- `verify-in-headset`: Record what has to be checked with real VR hardware, because every test so far ran headless.
- `add-gpu-screenshot-capture`: Capture real screenshots of the menu and a multiplayer view.
- `add-steam-rich-presence`: Join a friend with the "Join Game" button in the Steam friends list.

## Archived
- `2026-11-22-add-udp-session-transport`: Plain UDP transport between one host and many clients, carrying small typed packets.
- `2026-11-22-add-seed-sync`: Make every player generate the identical level so enemy indexes and positions line up.
- `2026-11-22-add-player-avatars`: Show each remote player as a visible, shootable avatar.
- `2026-11-22-add-enemy-sync`: Run enemy AI on the host only and mirror it on clients; make enemies hunt every player.
- `2026-11-22-add-damage-routing`: Turn hits on a remote avatar into real damage on that player's machine.
- `2026-11-22-add-projectile-replication`: Let every player see the projectiles fired by others and by enemies.
- `2026-11-22-add-upnp-port-mapping`: Let a host behind a home router open its UDP port automatically.
- `2026-11-22-add-vr-coop-menu`: Host and join from inside VR using the game's own wrist menu.
- `2026-11-22-add-reliable-delivery`: Make sure damage events are not silently lost on a lossy network.
- `2026-11-22-add-join-handshake`: Make joining reliable, optionally password protected, and notice dead connections.
- `2026-11-22-add-protocol-tests`: Test the wire protocol, transport, reliable delivery and UPnP without the game.
- `2026-11-22-add-headless-test-driver`: Exercise the mod in the real game without a headset or display.
- `2026-11-22-add-ci-pipeline`: Check every push automatically before the project is published.
- `2026-11-22-add-session-authentication`: Authenticate peers with a shared password and reject forged or altered packets, without ever sending the password.
- `2026-11-22-add-nat-pmp-fallback`: Open the host's port on routers that have UPnP switched off, and explain the cases no mapping can fix.
- `2026-11-22-add-lan-discovery`: Find hosts on the local network without typing an address.
- `2026-11-22-add-vr-text-entry`: Enter hostnames and the password, and pick hosts, without a keyboard.
- `2026-11-22-add-level-transition-sync`: Move all players between levels together, led by the host.
- `2026-11-22-add-impact-effect-replication`: Show impact effects and their sounds on every machine.
- `2026-11-22-add-network-metrics`: Make the mod's network cost visible and testable.
- `2026-11-22-add-run-notices`: Tell players what happens to the others, and end the session cleanly when the host leaves.
- `2026-11-22-add-steam-connect-link`: Join a friend from a Steam link instead of typing an address.
- `2026-11-22-add-reference-stubs-for-ci`: Compile the plugin in CI without the game's copyrighted assemblies.
