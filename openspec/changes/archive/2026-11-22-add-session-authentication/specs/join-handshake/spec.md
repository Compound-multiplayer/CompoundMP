## MODIFIED Requirements

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
