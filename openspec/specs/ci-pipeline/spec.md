# ci-pipeline Specification

## Purpose
Check every push automatically before the project is published.

## Requirements

### Requirement: Tests gate every push
Every push and pull request SHALL run the protocol and UPnP tests.

#### Scenario: Push
- **WHEN** a commit is pushed
- **THEN** the test job runs `dotnet run --project tests`

### Requirement: Conventional commits on PRs
A pull request SHALL fail if any commit subject is not a conventional commit.

#### Scenario: Bad subject
- **WHEN** a PR commit is titled "Fix stuff"
- **THEN** the commits job fails and names it

### Requirement: Plugin build needs game files
The plugin SHALL be built in CI only when the game assemblies are provided through a secret, and the workflow SHALL say so when it skips the build.

#### Scenario: No secret
- **WHEN** GAME_ASSEMBLIES_URL is not set
- **THEN** the build is skipped with a notice and the pipeline still passes

### Requirement: Releases from tags
A tag `v*` SHALL publish a release containing the built DLL and README.

#### Scenario: Tag
- **WHEN** tag v0.3.0 is pushed and the build succeeded
- **THEN** a release with CompoundMP.dll is created

### Requirement: Plugin always compiles in CI
Every push and pull request SHALL build the plugin against reference stubs without needing the game's assemblies.

#### Scenario: Fork PR
- **WHEN** a pull request comes from a fork without secrets
- **THEN** the compile job still runs

### Requirement: No game code in the repository
The repository SHALL NOT contain the game's assemblies or decompiled source.

#### Scenario: Stubs
- **WHEN** ci/stubs is inspected
- **THEN** it holds only member signatures
