# Change: Add nat fallbacks

## Why
UPnP does not work behind carrier-grade NAT, double NAT or routers with UPnP disabled, and the only advice is to use a VPN.

## What Changes
- Try NAT-PMP/PCP after UPnP fails.
- Detect CGNAT (private or shared-range external IP) and say so in the menu.
- Evaluate UDP hole punching with a small rendezvous service, or a relay, as the last resort.

## Impact
- Affected specs: `nat-traversal`
- Affected code: n/a
