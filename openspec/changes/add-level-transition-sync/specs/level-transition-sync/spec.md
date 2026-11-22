## ADDED Requirements

### Requirement: Everyone changes level together
When the host changes level, every connected client SHALL change to the same level.

#### Scenario: Host takes the elevator
- **WHEN** the host starts the next level
- **THEN** every client fades out and loads the same level with the same seed

### Requirement: A lone client cannot split the group
A client that triggers its own level change SHALL NOT leave the others behind in the old level.

#### Scenario: Client elevator
- **WHEN** a client reaches its elevator before the host
- **THEN** the client waits or is held until the group changes
