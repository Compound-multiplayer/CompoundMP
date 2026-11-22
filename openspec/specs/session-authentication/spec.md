# session-authentication Specification

## Purpose
Authenticate peers with a shared password and reject forged or altered packets, without ever sending the password.

## Requirements

### Requirement: The password is never sent
The password SHALL NOT appear in any packet; peers SHALL prove knowledge of it with an HMAC over fresh nonces.

#### Scenario: Handshake
- **WHEN** a client joins a password-protected host
- **THEN** the Hello carries a proof and a nonce, not the password
- **AND** the Welcome carries the host's proof so the client knows the host also has the password

### Requirement: Packets are authenticated
With a password set, every packet after the handshake SHALL carry a tag made with the peer's session key, and a packet with a missing or wrong tag SHALL be dropped.

#### Scenario: Forgery
- **WHEN** an attacker sends a PlayerDamage packet without a valid tag
- **THEN** it is dropped and counted as rejected

#### Scenario: Wrong key
- **WHEN** a packet is tagged with another session's key
- **THEN** it is dropped

### Requirement: No silent downgrade
A client that has a password SHALL refuse to join a host that does not.

#### Scenario: Open host
- **WHEN** a client with a password joins an open host
- **THEN** the client stops with "Host is not password protected"

### Requirement: Protocol versions
A host SHALL reject a Hello from a different protocol version with a message naming both versions.

#### Scenario: Old client
- **WHEN** a client with protocol version 1 joins
- **THEN** it is rejected with "Version mismatch"
