# Change: Add join handshake

## Why
Hello was sent once, so one lost packet made a join silently fail. Anyone who knew the address could join, and crashed peers were never cleaned up.

## What Changes
- Client re-sends Hello every second until the host answers Welcome, giving up after 15 s.
- Optional `Password` config; a host with a password replies Reject to a wrong one.
- Host drops a client that has been silent for 10 s; a client drops a host that has been silent for 15 s.
- Stop writing `AutoStart` back to the shared config (several instances raced on it).

## Impact
- Affected specs: `join-handshake`
- Affected code: `src/Plugin.cs`
