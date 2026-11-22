## ADDED Requirements

### Requirement: Tests need no game files
The tests SHALL build and run with only the .NET SDK and exit non-zero if any check fails.

#### Scenario: Run
- **WHEN** `dotnet run --project tests` is executed on a clean machine
- **THEN** it prints ALL PASSED and exits 0

### Requirement: Tests exit promptly
The test process SHALL exit on its own after the last check.

#### Scenario: Exit
- **WHEN** all checks have finished
- **THEN** the process ends without a manual kill
