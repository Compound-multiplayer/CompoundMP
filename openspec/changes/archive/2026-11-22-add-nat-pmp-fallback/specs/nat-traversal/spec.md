## ADDED Requirements

### Requirement: Fallback to NAT-PMP
When UPnP finds no gateway, the host SHALL try NAT-PMP before giving up.

#### Scenario: PMP router
- **WHEN** UPnP finds no gateway but the router answers NAT-PMP
- **THEN** the port is mapped for 2 hours and the public address is shown
- **AND** the mapping is removed when hosting stops

### Requirement: CGNAT is explained
When the router's external address is not publicly routable the status SHALL say so and suggest a VPN.

#### Scenario: 100.64.x.x
- **WHEN** the external IP is 100.70.1.1
- **THEN** the status says carrier-grade NAT

#### Scenario: Double NAT
- **WHEN** the external IP is 192.168.1.2
- **THEN** the status says the router is behind another router
