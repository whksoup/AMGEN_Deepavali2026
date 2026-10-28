Clicked-petal lift and neighboring rustle

Click detection was working in Play Mode, including a physical mouse click on the
sprout petal. The previous steadier-base change had greatly reduced the visible
tap bend. The shared inner-petal prefab now restores its original 5 / 9 / 8 degree
tap bend at the base / middle / tip. Gentle idle motion and the six-degree
shingled mesh remain in place. There is no decorative collar.

For one direct tap, the motion strengths around the upper ring are:
- Clicked petal: 100%.
- Next clockwise petal: 55%.
- Second clockwise petal: 22%.
- Immediate anticlockwise neighbor: 12%.
- Other petals: no pulse; their independent idle continues.

PetalRingPulse follows each direct pulse's actual rise and release. It combines
overlapping influences with max(), rather than adding them or triggering more
pulses, so rapid taps and multiple touches cannot circulate or amplify a rustle.
The clockwise neighbor is confirmed to be the upper surface at the mesh overlap.

Frame order: input picking uses the last displayed pose; PetalMotionController
resolves its own pose in LateUpdate (-200), the ring adds follow-through (-100),
then OuterPetalLayerMotion follows the final upper pose (default order).

The three independent inner-layer prefab roots carry the ring component. The
Two Layer Flower and Peacock Flower inherit it from Palette Inner Layer.
Existing palette selection and pointer-input code are unchanged.

Validation
- Scripts compile without errors.
- 62 Unity BakeMesh pose checks: rest, idle, each single pulse, adjacent taps,
  alternating taps, all petals, startup pulse and independently decaying pulses.
- No upper-neighbor or upper/outer intersections in the sampled poses.
- Minimum gaps: 8.328 mm between upper petals, 4.841 mm above the outer layer.
- All geometry remained within renderer bounds; no bone translation or scaling.
- InputSystem mouse-event tests selected all eight intended petals. A rapid
  retrigger and two simultaneous touches also worked. The reports record tip
  travel and exact agreement between the source and neighboring pulse strengths.
- Final physical Game-view click selected Petal 01 and produced normalized
  strengths 1 / 0.55 / 0.22 clockwise and 0.12 anticlockwise. The captured frame
  is physical-click-peak.png; measured values are in physical-click.json.
- Sampling verifies the current authored arrangement and settings; future mesh
  or amplitude edits require another clearance check.

Reproduction helpers (Unity MCP eval_file unless noted):
setup_rustle.cs: rebuild the prefab configuration in Edit Mode.
snapshot_poses.cs: capture actual skinned poses in Play Mode and restore them.
check_clearance.py: Python/Numpy triangle-overlap separation check.
check_input.cs: real InputSystem mouse/touch Update path, with temporary devices
  and callbacks cleaned up after completion.
clearance-validation.json and input-validation.json: recorded results.

This change supersedes the pulse-amplitude settings in output/flower-top-shingle.
