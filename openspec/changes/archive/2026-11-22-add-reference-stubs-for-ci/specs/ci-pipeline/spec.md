## ADDED Requirements

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
