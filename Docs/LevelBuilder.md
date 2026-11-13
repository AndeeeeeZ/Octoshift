# Octoshift Level Builder

Open **Tools > Octoshift > Level Builder** in the Unity Editor. Keep the window open while editing in the Scene view. The tool edits the active scene and does not run in Play Mode.

## Start a level

1. The included `Level Template.unity` retains Level 1's shared camera, UI, audio, events, player, exit, and gameplay controllers. It starts with one unconnected Cube at the center of the `Puzzle` root. The player and exit are inactive until placed. **Create Clean Template** creates this scene if it is missing, or adds the center cube if an older template has none.
2. Click **New Level** and choose a scene name. The tool copies the template, opens the new scene, updates its restart action to use the new name, and adds the scene to Build Settings.
3. Set **Next Scene** to the scene that should load after the exit.

The template is created only when it is missing. To change the shared setup later, edit `Level Template.unity` directly. Existing levels can be opened and edited with the same window.

## Build in the Scene view

- **Cube:** Choose the layer axis and layer number. Left-click to place a cube on the 3D grid. The cyan wire cube shows the placement position. `Cube Spacing` defaults to 6, matching the center-to-center spacing in Levels 1–5. Cubes are parented under `Puzzle` when that root exists, and the grid origin follows its position.
- **Connect:** Left-click one cube, then a neighboring cube along X, Y, or Z. The tool places a Pole prefab halfway between them and records the pole in both cubes' connection lists. Click the first cube again to cancel.
- **Player / Exit:** Left-click a visible, unconnected cube face. The tool moves the existing player or exit onto that face and activates it. If one is absent, it creates the assigned prefab; a new player is wired to the level's message, rotation, bound, and camera components.
- **Floor / One-way:** Press on a visible, unconnected cube face, drag to the opposite corner of a rectangle, and release. The rectangle stays within that face. The tool sizes the matching prefab's collider and sprite together and parents it to the cube, so it follows cube rotations. A horizontal drag makes a narrow ledge; dragging both directions makes a larger surface. One-way platforms keep their built-in trigger wiring.
- **Erase:** Left-click a placed floor or one-way platform to remove it, a pole to remove its connection, or a cube to remove it and its attached platforms and poles. The player and exit are detached and preserved when their cube is erased.

Alt-drag and other non-left-click Scene view navigation still work. Turn off **Enable Tool** to use normal Scene selection and transform tools. Every placement and deletion supports Unity Undo.

For an existing level whose grid is offset, select one of its cubes in the Hierarchy and click **Use Selected Cube as Grid Origin**. Adjust `Cube Spacing` if that level uses a different spacing.

## Check and play

Click **Validate Level** to check for a missing or inactive player or exit, empty or overlapping cube cells, broken or duplicate connections, off-grid cubes, missing controller and one-way platform references, and Build Settings inclusion. Save the scene, then use Unity Play Mode to test movement, rotation, platforms, exit, and restart. The tool does not alter player-facing controls.

Other props and presentation can still be added with Unity's ordinary scene tools. Face placement uses a small surface offset; adjust the transform in the Inspector when a particular visual needs fine positioning.
