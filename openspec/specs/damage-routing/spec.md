# damage-routing Specification

## Purpose
Turn hits on a remote avatar into real damage on that player's machine.

## Requirements

### Requirement: Avatar hits hurt the owner
A hit on an avatar SHALL reduce the real player's health on the machine that owns the avatar and SHALL NOT affect any other player.

#### Scenario: Six players
- **WHEN** five avatars stand around an enemy and it shoots the nearest
- **THEN** only that client's health drops, 5 to 0
- **AND** the other four are untouched

### Requirement: No double application
Damage applied because of a network message SHALL NOT be forwarded again.

#### Scenario: Applied once
- **WHEN** a client applies a PlayerDamage packet
- **THEN** the host does not receive a second damage event for it

### Requirement: The game's own rules apply
Network damage SHALL pass through the game's `TakeDamage`, so its invulnerability, shield and cool-down rules still apply.

#### Scenario: Spawn protection
- **WHEN** the player is inside spawn invulnerability
- **THEN** health does not drop
