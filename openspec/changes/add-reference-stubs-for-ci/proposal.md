# Change: Add reference stubs for ci

## Why
The plugin build is skipped unless a secret points at the game's assemblies, so a pull request from a fork never proves the plugin still compiles.

## What Changes
- Generate reference-only stub assemblies for the exact game and Unity types the plugin uses, from a checked-in list of signatures.
- Commit only the generator and the signature list, never the game's code.
- CI builds the plugin against the stubs on every push and still builds the real one when the secret is set.

## Impact
- Affected specs: `ci-pipeline`
- Affected code: n/a
