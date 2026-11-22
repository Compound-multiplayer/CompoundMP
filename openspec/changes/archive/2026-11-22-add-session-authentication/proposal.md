# Change: Add session authentication

## Why
The first password check sent the password in clear text and nothing authenticated packets, so anyone on the path who knew the address could join, kick players or inject damage events.

## What Changes
- Hello and Welcome carry HMAC proofs over fresh nonces; both sides must know the password, so a host also proves itself to the joiner.
- A per-client session key is derived from the password and both nonces; every packet after the handshake carries a 16-byte HMAC-SHA256 tag.
- Untagged or wrongly tagged packets are dropped by the receive thread and counted; handshake packets are the only exception.
- Hello carries a protocol version byte; a host on another version rejects with a clear message.
- A client with a password refuses a host without one (no silent downgrade); a host with a password rejects a client without one.

## Impact
- Affected specs: `session-authentication`, `join-handshake` (modified)
- Affected code: `src/Protocol.cs (Auth, Transport)`, `src/Plugin.cs`
