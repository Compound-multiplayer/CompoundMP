# Change: Add network metrics

## Why
Bandwidth and the effect of packet loss were unknown.

## What Changes
- Per-packet-type byte and packet counters in `Transport`.
- The 5-second status line reports transmit and receive throughput, the busiest packet types and the number of rejected packets.
- `SimulatedLossPercent` debug option drops a share of outgoing packets.

## Impact
- Affected specs: `network-metrics`
- Affected code: `src/Protocol.cs`, `src/Plugin.cs`
