# Design: Add ci pipeline

## Decisions
- No third-party actions beyond `actions/*`; releases use the preinstalled `gh` CLI.
- Release job is separate so only it has `contents: write`.
- Always-building without game files is proposed in `add-reference-stubs-for-ci`.
