# Change: Add session encryption

## Why
The join password is sent in clear text and packets are unauthenticated, so anyone on the path who knows the address can read or inject packets, including damage events.

## What Changes
- Replace the clear-text password with a challenge-response (HMAC over a host nonce).
- Derive a per-session key and authenticate (and optionally encrypt) every packet after the handshake.
- Reject packets from unregistered endpoints.

## Impact
- Affected specs: `session-encryption`
- Affected code: n/a
