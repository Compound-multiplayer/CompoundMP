## ADDED Requirements

### Requirement: Hostnames can be typed
The co-op page SHALL let the player type a hostname made of letters, digits, `.`, `-` and `:`.

#### Scenario: Hostname
- **WHEN** the player types play.example.org
- **THEN** the address line shows it and JOIN GAME resolves it

### Requirement: Password can be typed
The co-op page SHALL let the player enter a join password, shown masked.

#### Scenario: Masked
- **WHEN** the player enters a password
- **THEN** the display shows asterisks and Hello carries the password

### Requirement: Recent hosts
The page SHALL list the last five hosts joined and joining one SHALL need a single selection.

#### Scenario: Rejoin
- **WHEN** the player picks a recent host
- **THEN** the address is filled in and the join starts
