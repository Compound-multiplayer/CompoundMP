# Change: Add level transition sync

## Why
Each player currently changes level at their own elevator. A player who goes first sees the new level while the others are still in the old one, and enemy packets are skipped as mismatched.

## What Changes
- Host announces level changes (new level number and seed) and clients follow them.
- Define the rule for who can trigger a change (host only, or any player with a vote) and what a lagging player sees.
- Show a short countdown or toast when the group is about to change level.

## Impact
- Affected specs: `level-transition-sync`
- Affected code: n/a
