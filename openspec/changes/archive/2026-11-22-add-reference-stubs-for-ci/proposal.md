# Change: Add reference stubs for ci

## Why
The real-assembly build is skipped without a secret, so a fork pull request never proved the plugin still compiled.

## What Changes
- `ci/stubs`: signature-only stand-in for the game's `Assembly-CSharp` (about 140 lines, no game code).
- `ci/build-with-stubs.sh` downloads BepInEx 5.4.23.3 and the public Unity 2021.3.33 reference assemblies (checksummed) and builds the plugin.
- A `compile` job runs it on every push and pull request without secrets.

## Impact
- Affected specs: `ci-pipeline`
- Affected code: `ci/`, `.github/workflows/ci.yml`
