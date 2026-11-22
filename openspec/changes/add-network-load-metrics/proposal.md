# Change: Add network load metrics

## Why
Six players were tested on one machine. Real bandwidth, packet loss and frame-rate effects over a network are unknown.

## What Changes
- Count bytes and packets sent and received per type and log them in the periodic status line.
- Add a simple artificial latency and loss option for testing.
- Publish measured numbers for 2, 4 and 6 players.

## Impact
- Affected specs: `network-metrics`
- Affected code: n/a
