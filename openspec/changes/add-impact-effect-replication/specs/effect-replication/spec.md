## ADDED Requirements

### Requirement: Impacts are visible everywhere
When a bullet hits something on the machine that fired it, the other machines SHALL show the same impact effect at the same place.

#### Scenario: Wall hit
- **WHEN** a player's bullet hits a wall
- **THEN** every other player sees the same spark effect at that point

#### Scenario: Enemy hit
- **WHEN** a player's bullet hits a living enemy
- **THEN** other players see the hit reaction, not only the death

### Requirement: Effects are harmless
A replayed effect SHALL NOT deal damage, knock objects or send further network events.

#### Scenario: Replay
- **WHEN** an effect event is replayed
- **THEN** no damage or force is applied and nothing is re-sent

### Requirement: Bounded effect traffic
Effect events SHALL be rate limited per machine and excess events SHALL be dropped and counted.

#### Scenario: Minigun
- **WHEN** a minigun fires continuously
- **THEN** effect events stay under the cap
