## ADDED Requirements

### Requirement: Join from the friends list
A player SHALL be able to join a friend's hosted session from the Steam friends list without typing an address.

#### Scenario: Join Game
- **WHEN** a friend is hosting and has rich presence set
- **THEN** choosing Join Game connects to that session

### Requirement: UDP stays available
Direct UDP joining SHALL keep working for LAN, VPN and port-forwarded play.

#### Scenario: Typed address
- **WHEN** the player enters an IP
- **THEN** the session joins over UDP as today
