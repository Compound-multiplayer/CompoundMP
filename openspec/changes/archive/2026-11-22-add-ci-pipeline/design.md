# Design: Add ci pipeline

## Decisions
- No third-party actions beyond `actions/*`; releases use the preinstalled `gh` CLI.
- Release job is separate so only it has `contents: write`.
- Compiling without game files is added by `add-reference-stubs-for-ci`.
