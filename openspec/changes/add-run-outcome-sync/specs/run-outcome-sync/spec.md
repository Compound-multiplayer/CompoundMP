## ADDED Requirements

### Requirement: Run ends when everyone is dead
The run SHALL end for all players when every connected player is dead.

#### Scenario: Last death
- **WHEN** the last living player dies
- **THEN** every player's run ends

### Requirement: Host leaving ends the session
When the host stops or disconnects, every client SHALL be told and leave the session.

#### Scenario: Host quits
- **WHEN** the host leaves
- **THEN** clients show a message and stop
