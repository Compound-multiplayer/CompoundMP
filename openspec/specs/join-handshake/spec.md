# join-handshake Specification

## Purpose
Make joining reliable, optionally password protected, and notice dead connections.

## Requirements

### Requirement: Join survives packet loss
A client SHALL repeat its Hello once per second until it receives Welcome, and SHALL report failure after 15 s without an answer.

#### Scenario: Silent host
- **WHEN** no host answers
- **THEN** after 15 s the client stops and reports "No answer from the host"

### Requirement: Password check
If the host has a password, a Hello whose proof is missing or wrong SHALL be answered with Reject and the client SHALL not be registered. A correct proof SHALL register the client and start a tagged session.

#### Scenario: Wrong password
- **WHEN** a client joins with the wrong password
- **THEN** the host logs a rejection
- **AND** the client stops with "Wrong password"

#### Scenario: Missing password
- **WHEN** a client without a password joins a protected host
- **THEN** the client stops with "This host needs a password"

#### Scenario: Right password
- **WHEN** a client joins with the right password
- **THEN** the client is registered and receives the seed over a tagged session

### Requirement: Dead peers are removed
The host SHALL remove a client silent for 10 s, and a client SHALL leave when the host is silent for 15 s.

#### Scenario: Killed client
- **WHEN** a client process is killed
- **THEN** the host logs that it timed out and removes its avatar

#### Scenario: Killed host
- **WHEN** the host process is killed
- **THEN** the client logs "Lost connection to the host" and stops
