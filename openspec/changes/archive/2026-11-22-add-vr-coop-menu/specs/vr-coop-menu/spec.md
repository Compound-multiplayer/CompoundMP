## ADDED Requirements

### Requirement: Entry in the wrist menu
The main wrist menu SHALL contain a CO-OP MULTIPLAYER entry that opens the co-op page.

#### Scenario: Open
- **WHEN** the wrist menu is open
- **THEN** a CO-OP MULTIPLAYER line is present
- **AND** selecting it opens the co-op page

### Requirement: Every control can be poked
Every selectable item on the page SHALL have a collider on the Menu layer found by the same overlap test the hand uses.

#### Scenario: Overlap test
- **WHEN** the page is open
- **THEN** each option is found by a sphere overlap at its own position

### Requirement: Hosting and joining without a keyboard
The page SHALL let a player host, toggle UPnP, type a join address, join and disconnect.

#### Scenario: Type an address
- **WHEN** the player selects the keypad digits 192.168.0.
- **THEN** the address line reads 192.168.0.

### Requirement: Live status
The page SHALL show connection state, player count and, when hosting with UPnP, the public IP.

#### Scenario: Hosting
- **WHEN** the host has started
- **THEN** the status reads "Hosting on port 27777"
