LATEST REVISION — 29 OCTOBER 2026
The inner eight now use clean symmetric profiles, richer gold ornament and
stiffer resting motion. Current source/FBX/manifest were updated in place.
See ../inner-petal-regularized/README.txt and its validation reports. The
root-correction and integration narrative below records the prior revision.

REFINED FLOWER INTEGRATION - 28 OCTOBER 2026

CURRENT RESULT
8 distinct inner petals and 14 outer leaves are integrated in:
- Assets/Scenes/Deepavali_PetalWheelSequence.unity (primary experience)
- Assets/Scenes/Deepavali_LightSequence.unity
- Assets/Scenes/Deepavali_PetalLightingPrototype.unity
- Assets/Scenes/Deepavali_PaletteTest.unity (direct appearance/picking review)
- PetalPrototype/Prefabs/Deepavali Peacock Flower.prefab
- PetalPrototype/Prefabs/Deepavali Two Layer Flower.prefab

The existing scenes were edited in place. Camera, flower-wide X scaling,
background, lamps, UI, hand guide and event wiring were retained. The wheel
scene's intentionally disabled direct petal pointer remains disabled; the
PaletteTest scene keeps direct mouse/touch selection enabled. The input path
was also temporarily enabled in the wheel scene for verification.

INNER ROOT CORRECTION REQUESTED DURING INTEGRATION
The previous video-fit profiles narrowed around occluded boundaries near the
center, producing pinched necks. Those necks were replaced with broad,
straight-sided roots and a planar lower surface, following Image 1 and the
Petal2.svg reference. Base widths are 72% of each individual shoulder width.
The upper 48% of each petal, its tip, length, placement and rig are unchanged.
The outer ring and center source geometry are unchanged by this correction.
See root-correction/root-shape-change.json, broad-roots-overhead.png,
broad-roots-oblique.png and broad-roots-unity.png.

SOURCE / EXPORT
Authoritative Blender source:
output/flower-petal-prototype/Deepavali_InnerPetal_VideoFit.blend
Production asset:
Assets/HK_Signature/Deepavali/PetalPrototype/Models/Refined Flower/Deepavali_RefinedFlower.fbx
22 skinned petals, 66 accessible deform bones, 17,632 petal triangles;
18,144 triangles including the center. At most two normalized weights.
Both ArtworkUV and PetalCoord_Width_Length are retained. No preview cameras,
lights, ground, helper curves, images or animation clips are in the FBX.
Historical SinglePetal/OuterPetals FBX assets remain for historical test scenes.

MAPPING
Dense runtime indices 0-7 correspond to InnerPetal_01, REF_TopPetal_02 through
REF_TopPetal_08, clockwise from north. All seven REF objects are included.
Indices 8-21 correspond to OuterPetal_01, then OuterPetal_03 through _15.
The broad north leaf is OuterPetal_01. Source IDs 02 and 16 remain absent.
Existing renderer, motion, picking and artwork component identities were kept;
meshes, bind poses, bone placement/lengths and bounds were updated in place.
The outer controller accepts complete unique three-bone chains instead of
requiring 45 bones. Wheel mappings explicitly contain all 14 outer indices.
See integration-map.json and runtime-source-changes.diff (relative to the
actual initial working tree, including the user's pre-existing changes).

ARTWORK
Existing peacock art, symbols, eight initial fills, mandala, palette colors,
ornament, tertiary inlays, emission and edge-light controls are retained.
22 continuous fitted-coordinate textures resample the original artwork to
the new silhouettes. The merged north leaf receives one continuous pattern.
22 physical outline lighting maps and 22 body shading maps were regenerated.
The maps are linear and uncompressed. Coordinate maps use two bytes each for
U and V to avoid visible quantization of fine ornament. Source textures and
symbol atlases are retained. Root-to-tip reveals and radial palette waves
use physical rest coordinates, separately from fitted artwork coordinates.
Per-petal lighting-map overrides preserve all existing sequence APIs.
The added shader controls are disabled by default for historical assets.

MOTION / CLEARANCE
The old pulse of (5,20,8) degrees intersected the shallow inner meshes.
It is now (.08,.35,8): the original free-tip curl is retained and bending at
overlapping roots is reduced. Idle amplitudes/timing, pulse attack/hold/release,
retriggering and neighbor weights (.55/.22/.12) are retained. Measured tip
travel during the input check was approximately 20-34 mm in the scene.
The outer layer retains its coherent .72 lift / 1.35 downward / .5 sway follow.

To separate the overlapping outer joins, fixed downward mounting offsets of
0-1.855 mm in flower model space were applied to their renderers and skeleton
roots in Unity. The Blender outer silhouettes were not resculpted. See
outer-mount-offsets.json. The roots remain fixed throughout animation.
Bounds use the imported local mesh bounds plus 150 mm margin per side;
updateWhenOffscreen is disabled so Unity does not overwrite those conservative
bounds with a previous-frame pose during explicit pose sampling.
The center picking collider now uses the refined center mesh.

VALIDATION
All current reports pass:
- baseline-validation.json: 27 pre-integration wheel checks.
- after-validation.json: the same 27 wheel checks with the 14-leaf mapping;
  includes all 32 target/wind-up/overshoot combinations and all win patterns.
- light-validation.json: 32 checks in the older LightSequence scene.
- study-validation.json: 11 checks in the spatial lighting study, including
  rendered growth, tip, shimmer, gap controls and restoration of original pixels.
- initial-palette-validation.json: 24 initial state, independent palette,
  center, all supported colors, queue, reset and shader checks in PaletteTest.
- artwork-validation.json: 8 actual-frame artwork/reveal/queue/UI checks.
- input-validation.json: real Input System mouse events on all 8 petals,
  a repeated tap and two simultaneous touches; 11 expected events, bounded
  pulses, correct neighbor responses, no bone-position drift or runtime errors.
  Temporary editor focus settings and simulated devices are restored afterwards.
- framing-picking.json: all 8 deformed picks, independent skinning comparison,
  preserved camera and flower scale, viewport coverage and background clearance.
- asset-validation.json: both full-flower prefabs, all exported source IDs,
  66 bones, normalized weights, both UV sets, artwork maps and shader compilation.

clearance-validation.json tests exact vertical triangle separation over clipped
projected overlaps from actual Unity BakeMesh output for 62 rest/idle/pulse/
adjacent/alternating/simultaneous/independent-release poses. It includes both
petal rings and the center top. No intersecting triangle pairs were found.
Minimum scene-space separation: inner neighbors .553 mm; outer neighbors
.200 mm; inner-to-outer 3.244 mm; center 2.020 mm. Maximum root drift is
0.0000641 mm; bounds overflow and local bone position/scale drift are zero.
Lowest sampled petal Y is -.005409 m, above the ground at -.083 m.
This is a sampled envelope for the saved settings, not a continuous collision
solver. Changing geometry or motion settings requires another clearance check.

VISUAL EVIDENCE
baseline-01-shimmer.png / after-01-shimmer.png: original versus final flower
under the same comparison camera and appearance settings.
baseline-04-spin.png / after-04-spin.png: wheel lighting.
baseline-07-result.png / after-07-result.png: completed palette result.
after-game.png: final composited Game view, including controls and hand guide.
Additional stage captures are in the after-, light- and study- image series.

BACKUP / REPRODUCTION
before/PetalPrototype and before/Scenes contain the actual starting assets,
including uncommitted work; before/git-status.txt records the starting checkout.
Do not use git reset to undo this work. Restore specific affected assets and
matching .meta files from these snapshots if rollback is required.
root-correction/before-flat-roots.blend and before-flat-roots.fbx preserve the
sculpt/export before the user's root correction. The source-manifest records
the currently exported source checksum and mesh data.

export_refined.py exports the current merged/corrected Blender scene.
refit_artwork.py rebuilds the fitted artwork, body and lighting maps.
integrate.cs migrates an untouched 23-petal target and deliberately refuses
an already migrated 22-petal target. finalize-settings.cs and apply-mounts.cs
apply the recorded motion/bounds/collider and mounting settings idempotently.
Do not rerun historical build_round.py against the current Blender source.
The validators use Unity MCP eval_file in Play Mode; check_clearance.py runs
on the saved skinned pose snapshots in ordinary Python with NumPy.

REMAINING LIMITS
These are Unity Editor checks, not an Android hardware performance benchmark
or delivered APK. The render budget and extra fitted textures need profiling
on the installation device. The clearance report covers the saved settings
and sampled envelope; arbitrary inspector changes are outside that evidence.
