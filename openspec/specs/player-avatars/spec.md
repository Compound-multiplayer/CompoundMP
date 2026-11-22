# player-avatars Specification

## Purpose
Show each remote player as a visible, shootable avatar.

## Requirements

### Requirement: Poses are streamed
Each machine SHALL send its head and both hand poses and its dead flag 20 times per second.

#### Scenario: Visible remote player
- **WHEN** a client is connected
- **THEN** every other player sees an avatar that follows the client's head and hands

### Requirement: Avatars can be hit
An avatar SHALL carry a collider on the `PlayerDamageCollider` layer so enemy bullets collide with it.

#### Scenario: Bullet hits ghost
- **WHEN** an enemy bullet reaches the avatar
- **THEN** the hit is reported for that avatar's owner

### Requirement: Avatars leave with the player
An avatar SHALL be destroyed when its player says Bye.

#### Scenario: Leave
- **WHEN** a client leaves
- **THEN** its avatar disappears for everyone else
