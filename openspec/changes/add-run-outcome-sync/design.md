# Design: Add run outcome sync

## Notes
- `Avatar.Dead` already carries a dead flag; the missing piece is the run-ending rule and the client's reaction.
- Needs investigation of the game's own death flow (`PlayerController.IsDead`, `UpdateLeaderboardsWithScore`).
