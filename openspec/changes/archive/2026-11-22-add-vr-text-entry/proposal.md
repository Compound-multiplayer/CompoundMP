# Change: Add vr text entry

## Why
The digit keypad could not type hostnames, and the password could only be set in the config file.

## What Changes
- KEYBOARD page: digits, letters with a one-shot SHIFT, `. - : _`, DEL, CLEAR, DONE, for the join address or the password.
- PASSWORD button on the co-op page (shows ON/OFF); the password is shown masked.
- FIND GAMES page lists LAN hosts and the last five hosts joined (`RecentHosts`, recorded when a join succeeds).

## Impact
- Affected specs: `vr-text-entry`
- Affected code: `src/CoopMenu.cs`, `src/Plugin.cs`
