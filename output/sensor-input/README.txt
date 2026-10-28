SENSOR INPUT INTEGRATION — 2026-10-29

Reference: Assets/Scenes/Game.unity.
GameController reads Unity Input.GetTouch(...).position for the gameplay raycast.
Its UI uses TouchAreaButton, which reads Input System EnhancedTouch screenPosition.
Both consume the sensor's native touchscreen coordinates. The reference scene
contains no separate UDP, serial, or other custom sensor receiver. The existing
sensor driver therefore remains responsible for exposing contacts to Windows/Unity.

Configured Assets/Scenes/Deepavali_PetalWheelSequence.unity with a
DeepavaliTouchInput component on Deepavali Session. Sensor mode is the saved
default. The adapter uses EnhancedTouch, as Game.unity's UI does, then passes
screen-pixel XY through ScreenRestorationController.SetPointerScreen. That shared
pointer already drives restoration, both hand guidance dwell prompts and inactivity.
Coordinates retain the existing bottom-left screen convention and use the wall
camera's pixel rectangle. Contacts on other camera displays or outside its rect
are ignored; no extra inversion or calibration is introduced.

One contact owns interaction until it ends or is canceled. A replacement contact
clears stroke interpolation and dwell progress, preventing a line between hands
or two visitors sharing a partially completed dwell. Stationary contacts remain
present for painting/dwell but do not count as fresh movement for inactivity.
Release, cancellation and device removal clear the pointer. Mouse-to-touch
simulation is excluded from sensor mode, and there is no automatic mouse fallback.
EnhancedTouch enable/disable calls are balanced so other scene users retain their
own subscription. Existing external XY methods remain available with the adapter
disabled and restoration mouse input off.

Press M in the focused Game view to see the input mode/contact status. The new
"Mouse preview (temporary)" toggle provides the previous mouse-hover controls for
desk testing. Turn it off to return to sensor input. It is deliberately temporary;
Save to ScriptableObject still saves the existing brush, idle and session settings.

Validation
- Unity compilation completed without errors.
- touch-validation.json: 26 passing Play Mode checks using queued Input System
  TouchState events through the actual EnhancedTouch/adapter/session path.
- Covered default sensor mode, screen mapping with camera offsets, stationary
  dwell, multiple contacts, dwell reset on handover, continuous brush strokes,
  release/cancel/device loss, viewport/display filtering, debug-panel blocking,
  mouse preview and return to sensor mode, complete restoration to shimmer,
  second prompt and spin, input activity during spin, and inactivity/reset.
- All temporary devices, camera changes and test timings were restored.
- The initial test attempt missed the existing fresh-entry requirement of the
  second prompt. The harness now moves outside then back in; the corrected run
  passes. Initial evidence is retained in touch-validation-first-run.json.
- This verifies simulated device events, not a physical session with the custom
  sensor. The installation uses the same native touch interface as Game.unity.
- 01-sensor-debug.png shows the new menu status and temporary preview toggle.

Changed files
- Assets/HK_Signature/Deepavali/Restoration/Scripts/DeepavaliTouchInput.cs (+ .meta)
- Assets/HK_Signature/Deepavali/Restoration/Scripts/DeepavaliSessionDebugUI.cs
- Assets/Scenes/Deepavali_PetalWheelSequence.unity

Scene diff only adds/wires the adapter and turns the old mouse-input flag off.
Game.unity, GameController and TouchAreaButton were verified byte-for-byte unchanged.
Before snapshots and SHA-256 hashes are under output/sensor-input/before and
before-hashes.json. Existing unrelated changes were preserved; no commit was made.
