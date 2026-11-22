# effect-replication Specification

## Purpose
Show impact effects and their sounds on every machine.

## Requirements

### Requirement: Impacts are visible everywhere
When a bullet hits something on the machine that fired it, the other machines SHALL replay the same impact effect at the same place.

#### Scenario: Host fire
- **WHEN** host-fired bullets hit walls and avatars
- **THEN** clients replay each impact (20 sent, 20 received in the live test)

#### Scenario: Client fire
- **WHEN** a client fires one bullet
- **THEN** the host and the other clients receive its impact

### Requirement: Effects are harmless
A replayed effect SHALL NOT deal damage or send further network events.

#### Scenario: Replay
- **WHEN** an effect event is replayed
- **THEN** no damage is applied and nothing is re-sent
