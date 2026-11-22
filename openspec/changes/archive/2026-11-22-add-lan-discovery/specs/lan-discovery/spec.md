## ADDED Requirements

### Requirement: Hosts appear in the menu
A host on the same LAN SHALL appear in the FIND GAMES list within a few seconds of starting, with its name, address, player count and a password marker.

#### Scenario: LAN
- **WHEN** a host starts on the same network
- **THEN** the list shows e.g. "COMPOUND co-op 192.168.89.92 3P PW"

### Requirement: Foreign announcements are ignored
Announcements from a different protocol version, from the listener itself, and malformed packets SHALL NOT be listed.

#### Scenario: Old build
- **WHEN** a host announces protocol version 1
- **THEN** it is not listed

### Requirement: Stale hosts disappear
A host that stops announcing SHALL leave the list within about 7 seconds.

#### Scenario: Host quits
- **WHEN** a host stops announcing
- **THEN** it is no longer listed
