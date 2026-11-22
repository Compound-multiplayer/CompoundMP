# vr-text-entry Specification

## Purpose
Enter hostnames and the password, and pick hosts, without a keyboard.

## Requirements

### Requirement: Hostnames can be typed
The co-op page SHALL let the player type a hostname made of letters, digits, `.`, `-`, `_` and `:`.

#### Scenario: Hostname
- **WHEN** the player types P (with SHIFT) and then lay.example.org
- **THEN** the address reads Play.example.org

### Requirement: Password can be typed
The co-op page SHALL let the player enter a password, shown masked.

#### Scenario: Masked
- **WHEN** the player enters four characters
- **THEN** the display reads ****_ and the button reads PASSWORD:ON

### Requirement: Recent hosts
The FIND GAMES page SHALL list the hosts joined before, most recent first, and selecting one SHALL start the join.

#### Scenario: Rejoin
- **WHEN** two hosts have been joined
- **THEN** the newest is listed first and selecting it fills the address and joins

### Requirement: Every key can be poked
Every key on the keyboard page SHALL be found by the hand's overlap test.

#### Scenario: Overlap
- **WHEN** the keyboard page is open
- **THEN** no key is reported as unreachable
