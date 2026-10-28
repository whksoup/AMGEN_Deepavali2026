SESSION POLISH — 2026-10-29

Implemented in Assets/Scenes/Deepavali_PetalWheelSequence.unity.

Restoration reveals the running shimmer from its first brush stroke. Completion
keeps the same wheel pattern and clock, removing the bright-to-dim state switch.
The other held idle motion components still resume at restoration completion.

Restoration brush radius is 0.32 viewport heights, doubled from 0.16. Press M in
the focused Game view to open/close session controls. Brush size, inactivity
timeout and return fade duration are editable there. Save to ScriptableObject
persists them in Assets/HK_Signature/Deepavali/Restoration/Deepavali Session
Settings.asset; that asset is loaded on Play. Runtime build changes are temporary.
The panel blocks restoration/dwell input and does not alter the camera viewport.

After 30 seconds without meaningful movement, tracking acquisition, or an input
action, any active session phase returns to the dark opening. A two-second
fade dims through black, clears restoration, lamps, wheel and scroll at black,
then fades the base back in and assembles the hand prompt. An already idle base
does not repeatedly fade. Stationary mouse/hand samples and tiny sensor jitter
do not keep the session alive. The existing launcher timeout yields while this
session owns inactivity. Manual EndSequence cancels any pending return.

Hand guidance appearance was implemented in the requested separate chat:
01a183b2-9134-78f2-a8c0-250d39ea2fe1
HandHereInteraction reverses the existing dissolve, from 1 to 0, using the same
0.85-second duration as despawn. Dwell waits for appearance to finish. Reset,
disable and cancellation clear pending requests; an early dismissal waits for
appearance and fires once. See output/hand-guidance-appearance/README.txt.

Validation: 90 passing checks, no captured runtime errors; Unity compilation
completed without errors. All temporary test timings/devices were restored.

- flow-validation.json: 27 checks using real Input System mouse and M-key
  events, a full restoration/spin/result-video cycle, ScriptableObject disk
  save/load, and an actual 30.005-second inactivity wait.
- interruption-validation.json: 39 checks covering all 13 active phases,
  including every scroll phase and fallback result hold. Public input reaches
  the phases with shortened runtime timings, then verifies fade, reset and
  absence of stale callbacks. No phase is forced through reflection.
- hand-validation.json: 14 checks on an isolated prompt with deterministic
  unscaled animation clocks, real enable/disable lifecycle, shader values,
  queued dismissal, cancellation, independent materials and event counts.
- idle-input-validation.json: 7 checks for stationary input, jitter, movement,
  tracking expiry/reacquisition, and explicit reset during a fade.
- host-validation.json: 3 isolated checks for timeout ownership and release.

Captured Game frames 01–08 show assembly, partial restoration, the M menu,
completion, shimmer, the matching result scroll, fade and returned base.
The menu, restored shimmer and fade captures were visually inspected.

Changed existing files:
- PetalPrototype/Scripts/PetalWheelLightSequence.cs (pause lighting during fade)
- Restoration/Scripts/DeepavaliSessionController.cs
- Restoration/Scripts/DeepavaliSessionSettings.cs
- Restoration/Editor/DeepavaliSessionControllerEditor.cs
- Restoration/Deepavali Session Settings.asset
- UI/HandHereInteraction.cs
- Assets/Scenes/Deepavali_PetalWheelSequence.unity
- Assets/Scripts/Game/LaunchSceneController.cs
Paths above without Assets prefix start at Assets/HK_Signature/Deepavali/.
New runtime files: Restoration/Scripts/DeepavaliSessionController.Inactivity.cs
and DeepavaliSessionDebugUI.cs, with Unity metadata.

Before snapshots and hashes are in output/session-polish/before and
before-hashes.json. The hand source has its own before snapshot under
output/hand-guidance-appearance/. Scene diff is limited to brush size, removing
the wheel from held behaviours, idle defaults and the debug component.
Existing outcome mapping, scroll content, artwork and unrelated working changes
were preserved. No commit was made.
