# Design: Add gpu screenshot capture

## What was tried
- `-force-d3d11-warp` gives a real Direct3D 11 device under CrossOver (without it the renderer is `Null Device`, and `-force-vulkan` / `-force-glcore` fail to initialise graphics).
- With the device up, `Camera.Render` into a `RenderTexture` and an enabled camera rendered through the frame loop both return pure black, while `GL.Clear` into the same texture reads back correctly. The emulated device does not draw scene geometry.
- The wrist button also shrinks to zero scale unless looked at; `MenuDump` now holds it open with `GrowWhenFacing.SetOverrideBig(true)`.
- Steam's own screenshot key needs the Steam overlay and a rendering game, so it has the same blocker.
