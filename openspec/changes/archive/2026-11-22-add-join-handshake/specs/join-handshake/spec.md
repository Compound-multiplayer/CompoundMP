## ADDED Requirements

### Requirement: Join survives packet loss
A client SHALL repeat its Hello once per second until it receives Welcome, and SHALL report failure after 15 s without an answer.

#### Scenario: Silent host
- **WHEN** no host answers
- **THEN** after 15 s the client stops and reports "No answer from the host"

### Requirement: Password check
If the host has a password, a Hello with a different password SHALL be answered with Reject and the client SHALL not be registered.

#### Scenario: Wrong password
- **WHEN** a client joins with the wrong password
- **THEN** the host logs a rejection
- **AND** the client stops with "Wrong password"

#### Scenario: Right password
- **WHEN** a client joins with the right password
- **THEN** the client is registered and receives the seed

### Requirement: Dead peers are removed
The host SHALL remove a client silent for 10 s, and a client SHALL leave when the host is silent for 15 s.

#### Scenario: Killed client
- **WHEN** a client process is killed
- **THEN** the host logs that it timed out and removes its avatar

#### Scenario: Killed host
- **WHEN** the host process is killed
- **THEN** the client logs "Lost connection to the host" and stops
