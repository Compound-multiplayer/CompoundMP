# Change: Add upnp port mapping

## Why
Manual port forwarding is the main barrier to internet play.

## What Changes
- Minimal UPnP IGD client: SSDP discovery on every IPv4 interface, device description parsing, SOAP `AddPortMapping`, `GetExternalIPAddress`, `DeletePortMapping`.
- Runs on its own thread; the menu reads its status and public IP.
- Prefers a 2-hour lease renewed at half-life and falls back to a permanent mapping on error 725; the mapping is removed on stop.
- `UseUPnP` config option (default on).

## Impact
- Affected specs: `upnp-port-mapping`
- Affected code: `src/Upnp.cs`
