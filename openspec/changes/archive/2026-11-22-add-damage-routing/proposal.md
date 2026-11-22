# Change: Add damage routing

## Why
Enemies only exist on the host, so a hit on an avatar is detected there but must hurt the real player somewhere else.

## What Changes
- Harmony prefix on `Damageable.TakeDamage`: on the host, a hit on an avatar is not applied locally but forwarded as PlayerDamage to the owner.
- The owning client applies it through the game's own `TakeDamage` while an `Applying` flag stops the patch from re-forwarding it.
- Log each forwarded hit and each applied hit with health before and after.

## Impact
- Affected specs: `damage-routing`
- Affected code: `src/Patches.cs (TakeDamagePatch)`, `src/Plugin.cs`
