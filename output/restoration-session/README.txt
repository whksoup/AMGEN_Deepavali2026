MAIN WALL RESTORATION SESSION — 29 OCTOBER 2026

Scene: Assets/Scenes/Deepavali_PetalWheelSequence.unity
Select the new Deepavali Session object to configure the experience.

FLOW
Opening darkness, all candles off, flower motion/wheel held, center hand visible.
Hover the hand for .65 seconds. Leaving it or losing tracking clears partial dwell.
The existing loading ring advances during dwell, then the existing .85-second
disintegration plays. No area is painted until the prompt has disappeared.
A soft center reveal and warm point light start the freeform restoration phase.
Move the mouse around the wall without clicking. Progress persists and candles
ignite individually. Completion requires BOTH all four candles and 80% coverage.
The existing 1.8-second final reveal fills the gaps and resumes motion/Shimmer.
The center hand returns. Leave the badge and enter it again; a .15-second hover
consumes it, then its disintegration leads into the existing entry pulse/settle.
The controller waits for Ready before calling the existing randomized Spin().
The existing winner and palette bloom play. Once the colored result is fully
visible, hold it for 10 seconds, then return to the opening darkness and hand.
After a reset, a pointer held at center must leave and return before starting.

CONFIGURATION
DeepavaliSessionController.openingDwellDuration: .65 seconds
DeepavaliSessionController.spinDwellDuration: .15 seconds
DeepavaliSessionController.requiredCoverage: .8 (80%)
DeepavaliSessionController.coloredHoldDuration: 10 seconds
ScreenRestorationController: existing dark look, brush, input timeout and reveal.
HandHereInteraction: existing disintegrateDuration (.85 seconds).
The restoration component's autoComplete is off in this scene; Required Coverage
on Deepavali Session is the main experience's completion threshold.

EXTERNAL INPUT / RESET
Set ScreenRestorationController.useMouseInput=false for sensor input.
Refresh SetPointerViewport(normalized bottom-left XY, present) or
SetPointerScreen(bottom-left pixel XY, present). Prompt dwell and painting use
this same sampled input. External samples expire after .25 seconds by default.
Absence, invalid coordinates, and gaps between valid samples cancel dwell;
existing stroke continuity and large-jump handling are retained.
Call DeepavaliSessionController.endSequence() (also available as EndSequence())
for an immediate external reset from any phase. It clears coverage, candles,
input, dwell, result hold, pending prompt animation and wheel state, restores
the opening palettes/artwork and dormant poses, and respawns the prompt.
There are no coroutine waits or delayed invokes to survive a reset.

IMPLEMENTATION
DeepavaliSessionController owns explicit phases: AwaitingRestoration,
DismissingRestorationPrompt, Restoring, FinalReveal, AwaitingSpin,
DismissingSpinPrompt, EnteringWheel, Spinning, RevealingResult, HoldingResult.
ScreenRestorationController adds opt-in paint/automatic-completion gates,
input-continuity tracking, ClearPointer and RevealPoint; prototype defaults stay
enabled. Its mask, shader, UV orientation and URP renderer pass are unchanged.
The reusable hand retains click behavior in other scenes. This scene opts into
external dwell control and a screen overlay so darkness never hides the badge
or requires revealing coverage beneath it. Its disintegration shader is reused.
PetalWheelLightSequence exposes result visibility and runtime HoldResult;
PetalWheelPattern suppresses automatic return only while that flag is set.
Shared presets, outcome mapping, artwork and palette/bloom logic are retained.
Resetting a disabled wheel no longer creates shimmer lights. Teardown tolerates
already-destroyed references. The existing material owner stays enabled.
The 8 inner motions, outer motion and ring pulse retain their cached enabled
states. Main-scene direct petal picking remains disabled. Review controls are off.

VALIDATION / EVIDENCE
Final checks: 31/31 full-session assertions, 16/16 tracking/reset lifecycle
assertions, 27/27 wheel regression checks, 10/10 mask checks, 34/34 prototype
render/runtime checks and 7/7 prototype mouse checks. Final measured colored
holds were 10.004 and 10.006 seconds. Two complete mouse sessions produced
exactly two restoration completions, four prompt dismissals and two results.
No runtime errors were reported; Play Mode teardown was also checked clean.
The restoration shader is supported and reports no compiler messages.

session-validation.json: frame-observed, button-free Input System MouseState
events exercise two full sessions at production timings, both sides of the
coverage/candle gate, dwell cancellation, prompt rearming, full-result hold,
reset, lights, motion and callback counts. Screenshots 01 through 12 capture
the actual framebuffer including the overlay prompt. validate-session.cs runs
through Unity MCP eval_file with the main scene playing and Game view focused.
lifecycle-validation.json: external XY/presence, stale/invalid/inter-frame gap
handling, screen-pixel conversion, resets across all active phases and re-enable.
This test uses RevealPoint only to reach interruption targets quickly; the full
cycle test above reaches the gates by mouse painting.
wheel-regression/validation.json: the existing 27 wheel checks, including all
32 winner/windup/overshoot combinations and original automatic return behavior
when no session owns the hold. Captures are isolated in this output folder.
mask-validation.json: the existing 10 mask checks.
prototype-regression/runtime-validation.json: existing restoration render/runtime
suite rerun in its separate review scene, with outputs isolated in this folder.
prototype-regression/mouse-validation.json: the existing seven mouse-route
checks, confirming the prototype still paints immediately and excludes its UI.

Validation is in Unity Editor 6000.0.55f1, URP Render Graph, D3D11. This does not
claim deployment-hardware performance or sensor-device integration testing.
Editor handoff: the saved main wheel scene is open in Play Mode, waiting in
AwaitingRestoration with zero coverage, zero lit candles and mouse input on.
The center hand is visible. editor-handoff.json records the final runtime state.

RECOVERY / SCOPE
before/ contains exact pre-change scene and affected source/UI/restoration files;
before-hashes.json and changed-files.json identify the changes. New runtime source
is Assets/HK_Signature/Deepavali/Restoration/Scripts/DeepavaliSessionController.cs
and its .meta. No mesh, texture, atlas, UV, material, shared preset, renderer asset
or pipeline asset was changed. The separate RestorationTest scene is preserved
byte-for-byte; prototype-preservation.json verifies its hash.
Restore only the related backed-up files if needed; do not reset the shared
checkout, which contains substantial unrelated work.
