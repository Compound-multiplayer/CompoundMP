## ADDED Requirements

### Requirement: Hosting binds the configured UDP port
A host SHALL listen on the configured UDP port (default 27777). A client SHALL use an ephemeral local port.

#### Scenario: Host listens
- **WHEN** a host is started with port 27777
- **THEN** a client can send a Hello to that port and the host receives it

### Requirement: Packets are typed and tolerate garbage
Every packet SHALL be `[type byte][body]`. Packets with an unknown type or a malformed body SHALL be ignored without ending the session.

#### Scenario: Junk packet
- **WHEN** a packet of unknown type 99 arrives at the host
- **THEN** the host keeps running and still receives later valid packets

### Requirement: Host relays poses
The host SHALL forward each client's State packet to every other client and SHALL NOT echo it back to its sender.

#### Scenario: Relay
- **WHEN** client A sends a State packet
- **THEN** client B receives it
- **AND** client A does not receive it back

### Requirement: Shutdown does not block
Closing a session SHALL release the socket without blocking the game thread.

#### Scenario: Leave
- **WHEN** the player leaves a session while the receive thread is waiting
- **THEN** the call returns immediately and the socket is closed shortly after
