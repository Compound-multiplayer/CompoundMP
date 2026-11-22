# Change: Add player avatars

## Why
Players need to see where the others are, and enemy bullets need something to hit.

## What Changes
- Send each player's head and hand poses and a dead flag at 20 Hz.
- Render a remote player as a cube head and two sphere hands, smoothed toward the latest pose.
- Give the avatar a body hitbox (capsule on the `PlayerDamageCollider` layer with a `Damageable` and a `GhostMarker`) that follows the head.

## Impact
- Affected specs: `player-avatars`
- Affected code: `src/Plugin.cs (Avatar, Pose3)`
