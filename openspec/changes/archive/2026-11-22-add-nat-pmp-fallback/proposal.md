# Change: Add nat pmp fallback

## Why
UPnP does not work on every router and never behind carrier-grade NAT; the only advice was to use a VPN.

## What Changes
- When UPnP finds no gateway, ask the default gateway for a NAT-PMP mapping (2 h lease, renewed, removed on stop).
- Status explains carrier-grade NAT (100.64.0.0/10) and double NAT instead of failing silently.
- Tests use a fake gateway; the no-router test never touches the real router.

## Impact
- Affected specs: `nat-traversal`
- Affected code: `src/NatPmp.cs`, `src/Upnp.cs`
