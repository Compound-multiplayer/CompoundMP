## ADDED Requirements

### Requirement: Menu is usable with controllers
A player wearing a headset SHALL be able to open the wrist menu, open the co-op page, type an address or password, host and join using only the controllers.

#### Scenario: Keyboard
- **WHEN** the keyboard page is open in the headset
- **THEN** a finger poke selects exactly one key
- **AND** the highlight and tooltip match the key

### Requirement: Remote players are readable
Avatars and replicated projectiles and impacts SHALL be visible and correctly scaled for a headset wearer.

#### Scenario: Two headsets
- **WHEN** two players are in the same level
- **THEN** each sees the other's head and hands where they are
- **AND** enemy bullets and their impacts are visible

### Requirement: Results are recorded
Each playtest SHALL record the headset, runtime, player count and the observed pass or fail of every step.

#### Scenario: Report
- **WHEN** a playtest has been run
- **THEN** a dated report exists in docs/playtests/
