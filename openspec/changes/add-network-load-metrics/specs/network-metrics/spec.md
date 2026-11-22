## ADDED Requirements

### Requirement: Traffic is observable
The status line SHALL report bytes per second sent and received for each packet type.

#### Scenario: Status
- **WHEN** a session is running
- **THEN** the log shows per-type throughput

### Requirement: Budget
With six players the host's upload SHOULD stay under 1 Mbit/s.

#### Scenario: Six players
- **WHEN** six players are connected in combat
- **THEN** measured host upload is recorded and compared with the budget
