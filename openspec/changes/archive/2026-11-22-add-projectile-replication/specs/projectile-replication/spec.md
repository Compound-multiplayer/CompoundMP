## ADDED Requirements

### Requirement: Enemy fire is visible
When the host spawns a projectile, every client SHALL spawn a visual copy that travels at the same speed.

#### Scenario: Enemy bullet
- **WHEN** an enemy fires on the host
- **THEN** each client shows an EnemyBullet copy flying at about 9 m/s

### Requirement: Copies are harmless
A replicated projectile SHALL NOT deal damage or collide with players or enemies.

#### Scenario: Copy hits an avatar
- **WHEN** a copy reaches an avatar
- **THEN** no damage event is sent

### Requirement: Hitscan trails
A hitscan shot SHALL be shown on other machines by running the bullet's own trail code.

#### Scenario: Laser
- **WHEN** the host fires a Laser
- **THEN** clients draw the laser trail between the same two points

### Requirement: Bounded traffic
At most 80 projectile or trail events SHALL be sent per second per machine.

#### Scenario: Burst
- **WHEN** a weapon fires faster than 80 events per second
- **THEN** excess events are dropped and counted
