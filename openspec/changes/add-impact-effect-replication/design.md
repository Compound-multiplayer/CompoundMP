# Design: Add impact effect replication

## Open questions
- Impact objects are instantiated from several places (`HitObject`, `HitObjectPooledID`, the target's own `HitObject`, crit markers). Prefer one hook on `SpawnImpactObjects` and call it on a throwaway bullet copy, as `ReceiveTrail` already does for `CreateTrail`.
- Replaying on a living enemy needs the target `Damageable`; send its enemy index and fall back to the bullet's generic effect if the index is unknown.
- `IsCriticalArea(collider)` may need a real collider; guard with try/catch and skip the crit marker if it is missing.

## Risks
- Extra traffic with six players; budget it in `add-network-load-metrics`.
- Sounds from pooled sources may overlap badly when many events arrive together.
