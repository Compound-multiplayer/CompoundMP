# Design: Add udp session transport

## Decisions
- **UDP, not TCP.** Poses and enemy positions are re-sent continuously, so a lost packet is replaced by the next one. Events that must not be lost are covered separately by `reliable-delivery`.
- **Star topology.** Clients talk only to the host; the host relays poses. This keeps NAT handling to one open port.
- **Receive thread owns the socket.** `UdpClient.Close()` blocks while `Receive` is running, so the thread uses a 200 ms receive timeout and closes the socket itself when `Close()` clears the running flag.
