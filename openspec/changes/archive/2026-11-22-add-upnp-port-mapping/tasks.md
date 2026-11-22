## 1. Implementation
- [x] Implement `Upnp` (discovery, SOAP, lease renewal, cleanup)
- [x] Fake-router tests (SSDP + SOAP over loopback)
- [x] Prefer leased mappings so a crash cannot leave a port open forever
- [x] Observe a successful mapping and removal against a real router
