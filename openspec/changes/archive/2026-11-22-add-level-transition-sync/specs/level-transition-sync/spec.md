## ADDED Requirements

### Requirement: Everyone changes level together
When the host changes level, every connected client SHALL change to the same level with the same seed.

#### Scenario: Host takes the exit
- **WHEN** the host's level trigger changes level to map01_sewers_b
- **THEN** each client starts its own trigger towards map01_sewers_b
- **AND** all players have the same seed and enemy count

### Requirement: A lone client cannot split the group
A client SHALL NOT change level while the host is still in the client's current level.

#### Scenario: Client elevator
- **WHEN** a client reaches its elevator before the host
- **THEN** nothing happens and a toast says only the host can start the next level

### Requirement: Fallback load
If the client's own trigger does not reach the host's level within 8 seconds, the client SHALL load that level directly.

#### Scenario: No trigger
- **WHEN** the client scene has no usable trigger
- **THEN** the level is loaded directly
