## ADDED Requirements

### Requirement: Password never crosses the wire
The password SHALL NOT be sent; the client SHALL prove knowledge of it with a response to a host-supplied nonce.

#### Scenario: Handshake
- **WHEN** a client joins a password-protected host
- **THEN** a packet capture of the handshake contains no password

### Requirement: Tampered packets are dropped
A packet whose authentication tag does not verify SHALL be dropped.

#### Scenario: Injected damage
- **WHEN** an attacker sends a forged PlayerDamage
- **THEN** it is ignored
