## ADDED Requirements

### Requirement: Scripted two-player scenario
With the debug options set, two instances SHALL join, load the same level and run the combat scenario without input.

#### Scenario: Combat
- **WHEN** host and client run with Combat enabled
- **THEN** the host logs enemy hits on the client's avatar
- **AND** the client logs its health dropping

### Requirement: Debug options are off by default
Debug options SHALL default to disabled so normal play is unaffected.

#### Scenario: Fresh install
- **WHEN** the config file is generated
- **THEN** AutoStart and LoadMap are empty and Combat is false
