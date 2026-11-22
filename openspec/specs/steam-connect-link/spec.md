# steam-connect-link Specification

## Purpose
Join a friend from a Steam link instead of typing an address.

## Requirements

### Requirement: Command-line join
The game started with `+connect host:port` SHALL join that host automatically.

#### Scenario: Launch
- **WHEN** the game is started with +connect 127.0.0.1:27777
- **THEN** it joins the host at that address

### Requirement: Invite link
When the host's public address is known the mod SHALL log a steam://run link containing it.

#### Scenario: UPnP success
- **WHEN** a public IP is known
- **THEN** the log contains steam://run/615120//+connect%20<ip>:<port>
