REGULARIZED INNER PETALS + ORNAMENT — 29 OCTOBER 2026

IMPLEMENTED
The user accepted the video-fit proportions but requested a concession to
symmetry and clean edge curvature, confirmed INNER EIGHT ONLY, and requested
stiffer base-state motion. This revision replaces the earlier investigation
proposals with an implemented, checked version.

All eight inner petal outlines are now symmetric around their own root-to-tip
axis. A smooth common profile is scaled to each existing petal's individual
length, maximum width and broad base width. The lower root flanks are straight;
the shoulder transition is smooth through second derivative, with one clean
rise and taper. The broad planar lower surface and shallow 0.45-degree shingle
remain. Tip locations, root locations, all object transforms, 3-bone chains,
weights, topology and skinning layout are retained. The length/width variation
and irregular radial directions of the complete flower are intentional.

The 14 outer leaves and center have identical complete manifest mesh records
(vertices, UVs, topology, transforms and bone names). All 168 backed-up outer
texture/material files retain their hashes. The center artwork is unchanged.

ORNAMENT
Eight new 640x640 RGBA masks are packed into the existing 2048x2048 startup
atlas. They retain the approved symbol identities and palette roles. New
details include layered chased gold rails, inset bead chains, paired botanical
scrolls and leaves, double medallion hairlines, 48 large beads with interleaved
small beads, and upper/lower diamond accents. Decorative geometry is mirrored;
symbols are rotated to the actual individual petal direction for an upright
appearance from the installation camera.

The supplied screenshot is archived as ornament-reference.png. The earlier
patternBasics.svg guides inform the nested teardrop structure. These are clean
vector-derived masks, preserving the shader's animated palette, emission and
root-to-tip artwork controls. They do not bake the reference's perspective,
shadows or apparent edge irregularity into the texture.

Physical-UV artwork fitting, body shading and border-lighting maps were rebuilt
for the eight new profiles. Atlas references are retained. The visible medallion
and its dedicated lighting region are centered together. Fine detail remains
subject to the final LED wall's pixel density and viewing distance.

STIFFER RESTING MOTION
Existing rotation-based rig; no cloth simulator was added. Vectors below are
base / middle / tip bone degrees. Timing, phases, neighbor response, input APIs
and the outer follow controller's settings remain unchanged.

                 Before                 Now
liftBias         .30 / 1.00 / 2.00       .120 / .350 / 1.00
idleLift         .15 /  .60 / 1.80       .035 / .120 /  .65
idleSway         .05 /  .30 / 1.50       .010 / .060 /  .40
pulseLift        .08 /  .35 / 8.00       .035 / .160 / 5.00

This reduces base/middle idle oscillation by about 77%-80%, while retaining
gentle tip motion. Actual input-test tip travel was 11.1-16.9 mm in the scene,
compared with approximately 20-34 mm in the previous integration. The outer
ring still follows the inner layer, so its live response becomes gentler too;
its own geometry, artwork, controller settings and mounting offsets are retained.

INTEGRATION
Updated the existing authoritative Blender source and production FBX:
output/flower-petal-prototype/Deepavali_InnerPetal_VideoFit.blend
Assets/HK_Signature/Deepavali/PetalPrototype/Models/Refined Flower/Deepavali_RefinedFlower.fbx
output/refined-flower-integration/source-manifest.json matches that source.
output/inner-petal-artwork/layout.json is the current atlas/medallion layout.

The two complete flower prefabs and five integrated scenes receive the new
inner meshes, UV dimensions, startup atlas and stiffer motion settings:
PetalWheelSequence, LightSequence, PetalLightingPrototype, PaletteTest and
RestorationTest. Restoration behavior/code and wheel behavior/code are retained.
Main scene: Assets/Scenes/Deepavali_PetalWheelSequence.unity.

VALIDATION
geometry-validation.json: exact planar bilateral symmetry, unchanged dimensions
within floating-point precision, broad root plane, preserved transforms/topology/
bones and identical outer/center manifest records.
asset-validation.json: 8+14 skins, 66 bones, two normalized weight influences,
both UV channels, both complete prefabs, maps, bounds and shader compilation.
after-validation.json: all 27 wheel checks pass, including target/result cases.
artwork-validation.json: all 8 real-frame reveal/palette/queue/motion checks pass.
icon-lighting-validation.json: lighting aligns with all 8 rendered medallions.
framing-picking.json: camera retained, flower fits, all 8 deformed picks pass.
input-validation.json: mouse on all 8 petals, repeated tap and two simultaneous
touches; all 11 expected events, bounded pulses, no root/bone drift or errors.
The main scene's intentionally disabled direct pointer was temporarily enabled
for these input checks, then restored to disabled.
clearance-validation.json: 62 rest/idle/pulse/neighbor/independent-release poses;
zero intersecting triangle pairs. Minimum sampled gaps: inner neighbors .347 mm,
outer neighbors .206 mm, inner-to-outer 3.159 mm, center 2.191 mm. Root drift
below .000065 mm; bounds overflow and local bone position/scale drift zero.
This is a sampled Unity Editor envelope, not a continuous collision guarantee
or a hardware performance benchmark. No arbitrary new motion is validated.

PREVIEWS
artwork-review.png: all eight designs at their physical aspect ratios.
before.png / after.png: matched close review captures.
after-full.png: complete flower from the installed camera with review UI hidden.
The UI is hidden only during captures; the normal hand guide remains enabled.

REPRODUCTION
All scripts in this folder are specific to this revision and write staged
outputs before integration. Do not rerun historical build_round.py, revise_roots
or previous artwork generators on the active source to reproduce this result.
1. Run Blender 5.1 in background on the BEFORE blend, using regularize.py.
2. Run build_ornament.py and refit_maps.py with ordinary Python/NumPy/Pillow.
3. Run integrate.cs through Unity eval_file in Edit Mode on a clean saved scene.
4. Synchronize the staged blend to the authoritative source, and update its path
   and SHA-256 in the source manifest; synchronize staged/layout.json as above.
5. Run the supplied validators and compare saved previews.

RECOVERY
before/ contains 300 exact pre-change files with original relative paths,
including source blend, manifest, FBX/materials, maps, atlas/masks/materials,
the five scenes and two prefabs. before/hashes.json records their hashes.
before/motion.json records the original controller parameters.
changed-files.json lists differences against that backup. Restore affected
files together with matching metadata; do not use git reset on this shared
checkout, which has unrelated ongoing work.
