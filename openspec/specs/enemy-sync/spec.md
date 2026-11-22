# enemy-sync Specification

## Purpose
Run enemy AI on the host only and mirror it on clients; make enemies hunt every player.

## Requirements

### Requirement: Host owns enemies
Only the host SHALL simulate enemy AI. A client SHALL freeze its enemies' AI and follow the host's positions.

#### Scenario: Frozen client
- **WHEN** a client is in the same level as the host
- **THEN** all of the client's enemies report frozen
- **AND** their positions track the host's

### Requirement: Compact enemy stream
The host SHALL send enemy state at 10 Hz in packets of at most 40 enemies, each smaller than 1200 bytes.

#### Scenario: 95 enemies
- **WHEN** the host has 95 enemies
- **THEN** it sends 3 packets, each under 1200 bytes, that reassemble to all 95

### Requirement: Mismatched levels are skipped
A client SHALL ignore an enemy packet whose map name or enemy count differs from its own and log that it did so.

#### Scenario: Different map
- **WHEN** the host is in another level
- **THEN** the client skips the packet and logs it

### Requirement: Deaths sync
When the host reports an enemy dead the client SHALL kill it locally.

#### Scenario: Kill
- **WHEN** the host's enemy dies
- **THEN** it dies on every client

### Requirement: Client damage reaches the host
Damage a client deals to an enemy SHALL be sent to the host, which applies it.

#### Scenario: Client shot
- **WHEN** a client shoots an enemy
- **THEN** the host applies the damage and the death syncs back

### Requirement: Enemies hunt the nearest player
Each enemy SHALL target the nearest living player, whether that is the host or a remote avatar.

#### Scenario: Five avatars
- **WHEN** five avatars stand at different spots around an enemy
- **THEN** the enemy shoots the nearest one
