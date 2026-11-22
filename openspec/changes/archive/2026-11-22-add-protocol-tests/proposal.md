# Change: Add protocol tests

## Why
The networking code is Unity-free on purpose so it can be tested and run in CI.

## What Changes
- Console test project `tests/` compiling `Protocol.cs` and `Upnp.cs` directly.
- Loopback checks: enemy chunking under the MTU, handshake, seed delivery, relay, damage routing, junk tolerance, reliable delivery under loss.
- A fake router (SSDP responder plus HTTP/SOAP server) for UPnP.

## Impact
- Affected specs: `protocol-tests`
- Affected code: `tests/`
