Petal wheel lighting adaptation
==============================

Scene: Assets/Scenes/Deepavali_PetalWheelSequence.unity
Copied from: Assets/Scenes/Deepavali_LightSequence.unity
Reference: reference.html (the supplied petal-wheel.html, copied without edits)

Open the new scene and enter Play Mode. It starts in Shimmer.
Use Enter wheel for Pulse -> Settle -> Ready, or Spin to go directly through
Pulse -> Settle -> Spin. Play full sequence loops a demonstration. The right
panel has timing controls and all eight result previews. Press Save to
ScriptableObject beneath Play full sequence to persist all light, point-light,
and timing controls to the assigned Petal Wheel Settings asset. Unsaved slider
changes stay temporary. The asset loads automatically when Play Mode starts.
The preset can also be edited directly in the Inspector outside Play Mode.

Preserved appearance
--------------------
Existing geometry, materials, palette assets, spatial-coordinate maps, bloom
profile, camera, and lighting rig are retained. The old sequence and preview
components are disabled only in the copied scene. Existing petal motion is
untouched; mockup scaling, colors, artwork, and optional audio were not ported.

The old Flower Sequence style asset supplies brightness, color, gap coverage,
tip reach, and icon emission. Inner wheel motion uses the existing tip/edge mask
and icon channel: whole-edge brightness = 0 and wash strength = 0 during spin.
The outer ring lights its full authored pink/blue surface, ornament and edges,
modulated by the existing animation levels. Surface glow preserves artwork hue.
Saved screenshot settings display inner/center shimmer intensity 2.99, outer
spin intensity 1.90, outer shimmer intensity .36, and outer surface glow 3.
Colored shimmer point lights are enabled: strength .12, height .25, range .85.
The precise live slider values are retained in the preset.
These controls appear at the top of the preview panel and in the Inspector.
Their boost fades back to the existing result look during the palette bloom.
The existing center rim and icon use the mockup's separate level envelopes.

The actual flower has 8 inner and 15 outer petals. Breathing runs independently
on all 15 outer petals. Chase and win patterns wrap over the actual outer count.

Timing transferred from the mockup's default Soft preset
------------------------------------------------------
Shimmer: 5.5-second clock, level .6, variation .5, cosine raised to power 1.8.
Entry: .8-second sin-squared pulse, then 1.6-second cubic settle.
Wheel: 1.2 idle steps/s; .9-second acceleration to 16 steps/s; at least 1.2
seconds cruising; 3.6-second cubic slowdown. Cruise distance is extended to
land exactly on the preselected result. Optional .45-second wind-up and
.45-second overshoot hold plus .4-second settle are available.
Light levels: linear 160 ms attack, 450 ms inner / 420 ms outer release.
Outer breathing: 4.5-second base period, .4 variation, drifting phases and
new randomized peaks at dark points. Spin energy rises over 1 second and
falls over 1.8 seconds; the energized breath runs 25% faster. Spin floods
the outer ring over .5 seconds, holds .4, then releases over 1.6 seconds.
Result: 1.1 Hz winner pulse; 1.4-second delay; 1.8-second cubic palette bloom;
3.5-second hold; return to Ready by default. Result fades out over 1.4 seconds
at rest or .6 seconds when spinning again. Other win patterns and return
destinations are exposed in the preview and Inspector.

Implementation
--------------
PetalWheelPattern is the material-independent light/timing model, with a local
random seed and bounded substeps. PetalWheelLightSequence maps its normalized
levels onto the existing renderer. PetalWheelSequencePreview provides controls.
FlowerPaletteController.Pattern adds opt-in control of the existing radial
palette wave. Two shader scalars allow smooth handoff from spatial lights to
palette emission and fading of the result colors. Their neutral defaults are
handoff=0 and palette alpha=1, preserving the original shader paths elsewhere.

Verification
------------
validate.cs runs in Unity Play Mode on the copied scene and writes validation.json
plus eight rendered stage captures. Checks include timings and linear fades,
entry blocking, all 32 winner/wind-up/overshoot combinations, level bounds,
all six win patterns on 15 petals, tip-only shader settings, outer breathing,
palette handoff, return to opening artwork, repeat spins, and changing results.
Rendered shimmer, spin, intermediate bloom and final result were visually reviewed.
Unity compilation succeeded and the petal shader reported no compilation errors.

The original scene was separately run after the changes: both new shader
controls had neutral values; inner spin remained tip-only; outer emission
remained off during its original roulette stage; the controller initialized.
Original scene SHA256 remains:
90AEF87BC334FA34CB9E974C4EA0A89AD94E5E66700288B33AC7AAF0A538FE06
