## ADDED Requirements

### Requirement: Real screenshots exist
The repository SHALL contain game screenshots of the main wrist menu, the co-op page and a multiplayer view.

#### Scenario: Docs
- **WHEN** the README is opened
- **THEN** it shows captured images, not layout drawings

### Requirement: Menu text does not collide
On every co-op page no label SHALL overlap another control, and long status text SHALL wrap instead of running off the menu.

#### Scenario: Co-op page
- **WHEN** the page is captured while hosting
- **THEN** the three button columns, the keypad and the status lines are all legible
