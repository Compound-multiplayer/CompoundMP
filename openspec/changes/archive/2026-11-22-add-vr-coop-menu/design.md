# Design: Add vr coop menu

## Decisions
- Reuse `MenuOption`/`ASCIIText` and clone the template's fonts so the page looks and behaves like every other menu page.
- The page replaces the `Menu` object owned by `MenuButton`, so closing the wrist menu destroys it like any submenu.
- Join addresses are typed on a digit keypad (`ip` or `ip:port`). Hostnames and passwords are config-only for now (see `add-vr-text-entry`).
