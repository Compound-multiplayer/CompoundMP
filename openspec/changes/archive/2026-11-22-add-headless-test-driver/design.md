# Design: Add headless test driver

## Notes
- Runs under CrossOver with `-batchmode -nographics` and the bottle override `winhttp=native,builtin`.
- BepInEx 5 keeps at most five log files per folder and instances share one config file, so more than two instances run from APFS clones of the game folder.
