## ADDED Requirements

### Requirement: Plugin always compiles in CI
Every push and pull request SHALL build the plugin against reference stubs, without needing the game's assemblies.

#### Scenario: Fork PR
- **WHEN** a pull request comes from a fork without secrets
- **THEN** the plugin build job still runs and passes or fails

### Requirement: No game code in the repository
The repository SHALL NOT contain the game's assemblies or decompiled source.

#### Scenario: Stubs
- **WHEN** the stub project is inspected
- **THEN** it holds only signatures
