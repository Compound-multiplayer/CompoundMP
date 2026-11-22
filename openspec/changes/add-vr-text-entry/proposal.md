# Change: Add vr text entry

## Why
The keypad only types digits, `.` and `:`. Hostnames (for example dynamic DNS) and the join password can only be set in the config file.

## What Changes
- Add letter keys (a layout of A-Z and a shift/number toggle) to the co-op page, or a second page of keys.
- Add a password field next to the address field and send it in Hello.
- Keep a short list of recent hosts that can be picked with one poke.

## Impact
- Affected specs: `vr-text-entry`
- Affected code: n/a
