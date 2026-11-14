# Tutorial 2

The scene is fully wired in `Assets/Scenes/Tutorial 2.unity`. Tutorial 1's Next button loads it; both tutorial scenes are registered in Build Settings. Tutorial 2's Next button continues to Level 1.

`Tutorial 2 Checklist` contains the introduction, four objective labels, exit prompt, and `CameraControlsTutorial`. Edit the labels directly in the scene. The Canvas uses a 1920×1080 reference resolution and top-left anchors for the checklist. Its Anton font/material and letter fade timings match Tutorial 1.

The introduction starts with a perpendicular view and camera input locked. After the starting block is detected, the camera snaps to the same block-centered pivot used by F. The depth reveal uses that same focus operation and angle offsets, so it rotates around the block without shifting the pivot. Camera input unlocks after that reveal. Orbit requires 8 degrees of actual camera rotation; pan requires 0.5 world units of actual camera travel. F and R complete only their currently displayed objective. Earlier actions do not skip later objectives. Completed lines remain at 30% opacity.

Keyboard and fall restarts preserve completed objectives for the next load of this scene. Completing the R objective requires an actual reload; the completed checklist and exit appear after loading back in. End-menu Retry clears progress and replays the introduction. This state is session-only and is not written to PlayerPrefs or disk.

Cube interaction and rotation are disabled through scene overrides. The exit starts inactive. After all four objectives it appears on the existing floor, and reaching it plays the same end-menu timeline as Tutorial 1.

## Validation

Runtime C# compiled against the project's Unity assemblies. Static checks cover local references, new asset GUIDs, restart destinations, exit gating configuration, font/animation settings, and build scene entries. Live Unity control was declined, so visual and Play Mode behavior remains to be verified.

## Play Mode checklist

1. Open Tutorial 2 and enter Play Mode. Check that the cube face starts straight-on, the introduction types at the top, and LMB/MMB/F/scroll cannot move the camera during the introduction.
2. Watch the angle reveal. The pivot should remain at the block focus position while only the viewing angle changes. Check that the first objective types in the top left and that cube right-drag produces no rotation or drag indicator. Before orbiting or panning, press F: the camera should already be at its focus position and angle.
3. Click LMB without dragging; it should not complete the objective. Orbit with a drag; that line should dim, then the pan line should type.
4. Pan with an MMB drag. The pan line should dim and F should appear. Press F and check that the camera focuses on the player's current block, then the R line. F falls back to the player's position when no block is detected.
5. Press R. After the real scene reload, all four lines should be dim, the exit instruction should type, and the exit should appear.
6. Repeat R and, where possible, trigger a fall at an intermediate stage. Completed objectives should remain complete. A fall on the R step should not complete that step.
7. Reach the exit, click Retry, and check that the perpendicular introduction and fresh checklist return. Check Home and Next as well.
8. Check the checklist at 16:9 and 4:3, and briefly play Tutorial 1 to confirm its shared letter animation and Next button.
