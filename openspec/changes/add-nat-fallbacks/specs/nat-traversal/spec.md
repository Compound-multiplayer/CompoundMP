## ADDED Requirements

### Requirement: Fallback to NAT-PMP
When UPnP finds no gateway, the host SHALL try NAT-PMP/PCP before giving up.

#### Scenario: PMP router
- **WHEN** UPnP is disabled but NAT-PMP answers
- **THEN** the port is mapped and the public address is shown

### Requirement: CGNAT is explained
When the router's external address is not publicly routable the status SHALL say so and suggest a VPN.

#### Scenario: 100.64.x.x
- **WHEN** the external IP is 100.64.1.2
- **THEN** the menu says the connection is behind carrier-grade NAT
