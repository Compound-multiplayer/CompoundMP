# level-sync Specification

## Purpose
Make every player generate the identical level so enemy indexes and positions line up.

## Requirements

### Requirement: Client adopts the host seed
When a client creates a map after receiving a host seed, it SHALL generate that map from the host's seed.

#### Scenario: Same level
- **WHEN** host and client create map01_sewers with the host seed
- **THEN** both have the same enemy count
- **AND** enemy 0 is at the same position on both

### Requirement: Host announces new seeds
When the host generates a map it SHALL send its seed to every connected client.

#### Scenario: New run
- **WHEN** the host starts a new run
- **THEN** every client receives the new seed
