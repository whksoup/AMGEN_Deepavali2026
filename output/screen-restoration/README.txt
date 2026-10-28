SCREEN RESTORATION INTERACTION PROTOTYPE — 29 OCTOBER 2026

TRY IT
Open Assets/Scenes/Deepavali_RestorationTest.unity and press Play.
Move the mouse over the Game view without clicking. A soft circular brush
gradually restores color and brightness; restored areas persist. The four
lamps ignite individually when their positions have sufficient coverage.
At 72% coverage of the inset screen area, a 1.8-second completion reveal
fills remaining gaps, then starts the existing flower motion and Shimmer.

R: restart from darkness. F: finish the reveal. H: hide/show test controls.
After completion, the controls offer Enter wheel, followed by Spin.
The control panel excludes mouse painting over its own area.
Select Pre-idle Screen Restoration to adjust the prototype in the Inspector.
Play Mode adjustments are temporary; make persistent changes in Edit Mode.

DEFAULTS
Dark brightness .24 (linear), saturation 0. Brush radius .16 viewport heights,
softness .65, .38 seconds of dwell for full center coverage. The brush remains
circular on the wide wall. Mask width is 384, height follows camera aspect.
Completion measures average mask coverage inside Rect(.025,.04,.95,.92).
Lamp ignition threshold .55; flame and light fade-in .55 seconds.
These values are review settings, not locked experience timings.

INPUT / INTEGRATION
ScreenRestorationController.useMouseInput is true in the saved review scene.
For the future sensor adapter, set useMouseInput=false and continually call
SetPointerViewport(Vector2 normalizedXY, bool present), bottom-left origin;
or SetPointerScreen(Vector2 pixelXY, bool present), bottom-left screen pixels.
The latter respects the camera's viewport offset/size. No input device protocol
is assumed. Stale external samples expire after .25 seconds. Presence gaps,
invalid coordinates and large tracking jumps break the stroke. Loss of input
never erases prior progress. Call ResetRestoration for a new session.

The low-resolution persistent linear R8 mask is updated on the CPU, allowing
progress and lamp responses to sample the same data without GPU readbacks.
A camera-local URP pass composites after bloom/post-processing; it is fully
bypassed in Idle. No shared renderer feature or pipeline asset is modified.
The mask is independent of petal textures, UVs, artwork progress, palette
waves, and the startup atlas. The palette/material owner remains enabled.
Idle behaviours cache their enabled states; the existing disabled direct
petal input stays disabled. Completion calls PetalWheelLightSequence.ResetPreview.

The background shader additions are optional per-renderer controls:
- DiyaFlame.shader: _Restoration defaults to 1.
- FestivalSoil.shader: _LampRestoration defaults to (1,1,1,1).
The response component controls flame alpha, point-light intensity and the
four soil light pools. Other scenes retain their original fully-lit defaults.

SCOPE / COORDINATION
This is a separate review scene copied from the accepted flower/wheel scene
after the rootward inner-artwork revision. The main wheel scene was not changed:
SHA256 3D7B76F15915E451A9BC88EFCBEA30C23A3C872CA614CB43F83ABEEAAB810979.
Geometry, rig, 8+14 petal mapping, UVs and petal shaders were not edited here.
The copied scene shares the Rootward Inner startup atlas/materials, so the
coordinated texture chat's next atlas revision will also appear in this scene.
The hand guidance canvas and old review panels are hidden only in this copy.
No host navigation, timeout, blessing logic or device integration was added.

VALIDATION
Unity 6000.0.55f1 / URP 17.0.4, Render Graph enabled, Direct3D 11.
34/34 frame-observed runtime/render checks pass (runtime-validation.json):
all four corners align visually and light only their corresponding lamps;
dormant rendering is monochrome; coverage persists; invalid input, gaps and
jumps are handled; external input expires; completion fires once per session;
all lamps and motion return; Shimmer and wheel entry work; reset and re-enable
start cleanly; runtime textures are released on disable; no runtime errors.
The white-mask/bypass comparison averages .076 byte levels per RGB channel,
with a maximum 3/255 difference at isolated pixels from the additional HDR
render pass. Finished gameplay bypasses the pass completely.

7/7 actual Input System hover checks pass (mouse-validation.json), including
button-free restoration, UI exclusion, leaving and re-entering the viewport.
10/10 pure-mask checks pass (mask-validation.json), including wide-screen
brush shape, stroke continuity, reset and 30/120 Hz dwell consistency.
The restoration shader compiles without errors. The compatibility-mode code
path is included but has not been visually exercised in this prototype.

CPU paint arithmetic averaged .586 ms at 384x168 over 1000 repeated strokes
in this editor session (paint-timing.json). That excludes upload and GPU work;
it is not a target-hardware performance guarantee. Actual wall/sensor testing
and a standalone Player build remain for later integration.

EVIDENCE / REPRODUCE
01-dormant.png, 02-partial.png, 03-restored-live.png show the visual progression.
corner-0.png through corner-3.png verify candle/mask alignment independently.
validate-runtime.cs and validate-mouse.cs run through Unity MCP eval_file in
Play Mode in this review scene. validate-mask.cs can run in either editor state.
create-preview.cs is the one-time scene builder and refuses to overwrite it.
deferred-implementation contains the historical inactive draft; the current
implementation is exclusively Assets/HK_Signature/Deepavali/Restoration.

Editor released to the texture chat in Edit Mode with the review scene saved,
mouse input and visible controls enabled. Temporary test devices and input
settings were restored, as was the Run In Background override.
