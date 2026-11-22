## ADDED Requirements

### Requirement: Traffic is observable
The status line SHALL report throughput sent and received, the busiest packet types and the rejected-packet count.

#### Scenario: Status
- **WHEN** a session is running
- **THEN** the log shows e.g. "tx=14.1kB/s rx=3.6kB/s [State 10.7 Enemies 7.0]"

### Requirement: Loss can be simulated
A debug option SHALL drop a percentage of outgoing packets.

#### Scenario: 100 percent
- **WHEN** SimulatedLossPercent is 100
- **THEN** no packet leaves

### Requirement: Budget
With six players in combat the host's upload SHOULD stay under 1 Mbit/s.

#### Scenario: Six players
- **WHEN** one host and five clients fight on one level
- **THEN** the host transmits about 54 kB/s (0.44 Mbit/s), of which State is 45 kB/s and Enemies 17 kB/s
