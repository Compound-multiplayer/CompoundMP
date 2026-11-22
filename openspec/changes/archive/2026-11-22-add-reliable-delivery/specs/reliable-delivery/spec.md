## ADDED Requirements

### Requirement: Lost damage is re-sent
A reliable packet SHALL be re-sent every 200 ms until acknowledged, for at most 10 transmissions.

#### Scenario: Loss
- **WHEN** the first two transmissions are dropped
- **THEN** the third arrives
- **AND** the sender stops re-sending after the ack

### Requirement: Duplicates apply once
A receiver SHALL acknowledge every copy but apply a given sequence number once.

#### Scenario: Re-send after a lost ack
- **WHEN** the same packet arrives twice
- **THEN** the damage is applied once
- **AND** both copies are acknowledged

### Requirement: Giving up
A packet that is never acknowledged SHALL be abandoned after the last attempt.

#### Scenario: Dead peer
- **WHEN** every ack is lost
- **THEN** the pending packet is removed after 10 transmissions
