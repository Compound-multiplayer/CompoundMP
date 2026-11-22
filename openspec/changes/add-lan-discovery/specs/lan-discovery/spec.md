## ADDED Requirements

### Requirement: Hosts appear in the menu
A host on the same LAN SHALL appear in the co-op page within five seconds of starting.

#### Scenario: LAN
- **WHEN** a host starts on the same network
- **THEN** the join list shows it with its player count

### Requirement: Version mismatch is hidden
Announcements from a different mod version SHALL NOT be offered for joining.

#### Scenario: Old build
- **WHEN** a host runs another version
- **THEN** it is not listed
