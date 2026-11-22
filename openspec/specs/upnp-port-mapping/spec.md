# upnp-port-mapping Specification

## Purpose
Let a host behind a home router open its UDP port automatically.

## Requirements

### Requirement: Port is mapped when hosting
When hosting with UPnP enabled the mod SHALL ask the router to forward the UDP port to this machine and SHALL report the public IP.

#### Scenario: Router supports UPnP
- **WHEN** a UPnP router answers discovery
- **THEN** the port is mapped and the public IP is shown

### Requirement: Mapping is removed
The mod SHALL remove the mapping when the session stops.

#### Scenario: Leave
- **WHEN** the host stops
- **THEN** DeletePortMapping is sent

### Requirement: Leases are preferred
The mod SHALL request a leased mapping and renew it, falling back to a permanent mapping only if the router refuses leases.

#### Scenario: Permanent only
- **WHEN** the router answers error 725 to a leased request
- **THEN** the mod retries with a permanent mapping

### Requirement: No router is not an error
If no gateway answers, hosting SHALL continue and the status SHALL say that no gateway was found.

#### Scenario: No router
- **WHEN** discovery times out
- **THEN** status reads "no gateway found" and no exception is raised
