# poke-simulation Specification

## Purpose
Exercise the real hand-selection path without a headset.

## Requirements

### Requirement: Keys work through the game's own selection code
Putting the hand on a key and pulling the trigger SHALL select that key through `MenuSelector`.

#### Scenario: Keypad
- **WHEN** the hand is placed on 7, 2, ., 1, DEL and 9 in turn and the trigger is pulled each time
- **THEN** the join address reads 72.9

### Requirement: Test hooks are inert in play
The poke patches SHALL do nothing unless the test is active.

#### Scenario: Normal play
- **WHEN** the test is not running
- **THEN** trigger and pose calls behave as in the unmodified game
