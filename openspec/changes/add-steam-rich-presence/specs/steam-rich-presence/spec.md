## ADDED Requirements

### Requirement: Join from the friends list
A player SHALL be able to join a friend's hosted session from the Steam friends list without typing an address.

#### Scenario: Join Game
- **WHEN** a friend is hosting and has published rich presence
- **THEN** choosing Join Game connects to that session, whether or not the game is already running

### Requirement: Direct joining stays available
Typed addresses, LAN discovery and +connect links SHALL keep working.

#### Scenario: Typed address
- **WHEN** the player enters an address
- **THEN** the session joins as before
