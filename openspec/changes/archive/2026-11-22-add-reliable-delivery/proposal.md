# Change: Add reliable delivery

## Why
EnemyDamage and PlayerDamage are one-shot events sent over UDP. A dropped packet lost a hit.

## What Changes
- Reliable packets carry a sequence number after the type byte; the receiver answers with Ack and drops duplicates.
- Unacknowledged packets are re-sent every 200 ms up to 10 times.
- EnemyDamage and PlayerDamage use it; State, Enemies and projectiles stay unreliable.

## Impact
- Affected specs: `reliable-delivery`
- Affected code: `src/Protocol.cs`, `src/Plugin.cs`
