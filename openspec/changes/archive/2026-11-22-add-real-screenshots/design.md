# Design: Add real screenshots

## Why the first captures were black
Not the renderer: the wrist button shrinks to zero scale whenever the game is in the StartArea scene (and unless looked at), so the menu camera's orthographic size was zero and nothing was drawn. `MenuDump` now pins the button at its full size. Cameras also need `stereoTargetEye = None` so a VR build renders them to the texture.
