# Publishing to GitHub

1. Create the repository and push `main` (`git remote add origin ... && git push -u origin main`).
2. The `commits`, `test` and `compile` jobs need nothing else; they run on every push and pull request.
3. To publish a release push a tag such as `v0.4.0` (`git tag v0.4.0 && git push origin v0.4.0`); the `release` job attaches
   `CompoundMP.dll` and the README once the `compile` job passed (CI never needs the game files).
4. Enable Dependabot alerts if you want the monthly action and NuGet update pull requests (`.github/dependabot.yml`).
