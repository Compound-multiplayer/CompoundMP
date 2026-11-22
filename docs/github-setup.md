# Publishing to GitHub

1. Create the repository and push `main` (`git remote add origin ... && git push -u origin main`).
2. The `commits`, `test` and `compile` jobs need nothing else; they run on every push and pull request.
3. To enable the `build` job (compile against the real game assemblies) add a repository secret `GAME_ASSEMBLIES_URL`: a private,
   downloadable zip that contains `COMPOUND_Data/Managed/Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll` and `UnityEngine*.dll`
   (for example a private release asset or object-storage URL with a signed link). Never commit these files.
4. To publish a release push a tag such as `v0.4.0` (`git tag v0.4.0 && git push origin v0.4.0`); the `release` job attaches
   `CompoundMP.dll` and the README once the `build` job passed.
5. Enable Dependabot alerts if you want the monthly action and NuGet update pull requests (`.github/dependabot.yml`).
