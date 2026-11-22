# run-outcome-sync Specification

## Purpose
Tell players what happens to the others, and end the session cleanly when the host leaves.

## Requirements

### Requirement: Deaths are announced
When another player's dead flag turns on, every other player SHALL see a toast.

#### Scenario: Player down
- **WHEN** a remote avatar's dead flag turns on
- **THEN** a toast says a player went down

### Requirement: Host leaving ends the session
When the host leaves, every client SHALL be told and stop.

#### Scenario: Host quits
- **WHEN** the host leaves
- **THEN** clients log "The host left the game" and stop
