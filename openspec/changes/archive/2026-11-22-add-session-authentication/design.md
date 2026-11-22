# Design: Add session authentication

## Decisions
- **Authentication only, not encryption.** Poses and damage are not secret; what matters is that nobody can inject or alter them. `net472` has no AES-GCM, so encrypting would mean hand-rolling authenticate-then-encrypt for no gameplay benefit.
- **PBKDF2 (5000 iterations) password key**, then HMAC proofs and a session key bound to both nonces, so a recorded handshake cannot be replayed into a new session.
- A repeated Hello with the same client nonce receives the same host nonce, so a late duplicate cannot change a live session key.

## Limits
- A packet captured inside a live session can be replayed within that session. Reliable damage is protected by sequence numbers and a duplicate window; poses and projectiles are harmless to repeat.
- Without a password the session is open, as before.
