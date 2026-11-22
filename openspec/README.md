# OpenSpec

Specs for the co-op mod, in the [OpenSpec](https://github.com/Fission-AI/OpenSpec) layout.

- `specs/` current requirements, one folder per capability.
- `changes/archive/2026-11-22-<change>/` completed changes: proposal, tasks (all done), design notes and the spec delta that produced the current spec.
- `changes/<change>/` **proposed** changes that are not implemented yet. Each has a proposal, a task list and a proposed spec delta. When one is finished, move it to `changes/archive/<date>-<change>/` and merge its delta into `specs/`.

## Open proposals
- `verify-in-headset`: Record what has to be checked with real VR hardware, because every test so far ran headless.
- `add-impact-effect-replication`: Show hit sparks, muzzle flashes, explosions and their sounds on every machine.
- `add-level-transition-sync`: Move all players between levels together.
- `add-vr-text-entry`: Let VR players enter hostnames and passwords without a keyboard.
- `add-steam-friend-join`: Join a friend through Steam instead of typing an address.
- `add-session-encryption`: Authenticate peers and protect session traffic.
- `add-nat-fallbacks`: Connect hosts that UPnP cannot help.
- `add-reference-stubs-for-ci`: Let CI compile the plugin without the game's copyrighted assemblies.
- `add-gpu-screenshot-capture`: Capture real screenshots of the menu and multiplayer view.
- `add-network-load-metrics`: Measure what the mod costs in bandwidth and frame time.
- `add-run-outcome-sync`: Define what happens when players die or the host leaves.
- `add-lan-discovery`: Find hosts on the local network without typing an address.

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
