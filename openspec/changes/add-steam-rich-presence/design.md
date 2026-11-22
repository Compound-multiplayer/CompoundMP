# Design: Add steam rich presence

## Already in place
- `SteamJoin.Tick` registers the join-request callback once Steam is up and reports `Steam: not running` otherwise.
- `SteamJoin.Publish` sets `connect` and `status` rich presence while hosting (public address when mapped, else this PC's LAN address) and clears it when hosting stops.
- `Plugin.JoinFromSteam` joins the address from a join request.
