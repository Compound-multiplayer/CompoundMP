# Change: Add vr coop menu

## Why
Keyboard shortcuts are unusable for a VR player.

## What Changes
- Harmony postfix on `BaseMenu.Start` adds a CO-OP MULTIPLAYER line between PERFORMANCE OPTIONS and END RUN.
- The line opens a page built from the game's own ASCII-text menu options: HOST GAME, UPNP on/off, status lines, a keypad (0-9 . :), DEL, CLEAR, JOIN GAME, DISCONNECT, Back.
- Options are `MenuOption` subclasses on the `Menu` layer with box colliders so the game's `MenuSelector` can highlight and trigger them.

## Impact
- Affected specs: `vr-coop-menu`
- Affected code: `src/CoopMenu.cs`
