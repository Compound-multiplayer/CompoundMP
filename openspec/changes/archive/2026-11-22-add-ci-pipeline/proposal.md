# Change: Add ci pipeline

## Why
The repository is going to GitHub; tests, conventional commits and release packaging should not depend on one machine.

## What Changes
- GitHub Actions workflow: conventional-commit check on pull requests, protocol/UPnP tests on every push, plugin build and release packaging.
- The plugin build needs the game's copyrighted assemblies, so it runs only when the `GAME_ASSEMBLIES_URL` secret is set; BepInEx 5.4.23.3 is downloaded and checked against a SHA-256.
- Dependabot for GitHub Actions and NuGet.

## Impact
- Affected specs: `ci-pipeline`
- Affected code: `.github/`
