SUPERSEDED ARTWORK — 29 OCTOBER 2026
The user subsequently requested symmetric inner geometry, richer ornament
and stiffer resting motion. See ../inner-petal-regularized/README.txt.
This folder preserves the prior pattern revision and its comparisons.

INNER PETAL PATTERN GUIDE REFINEMENT

STATUS: Imported and visually checked in Deepavali_PetalWheelSequence.unity.
The main scene is open in Play Mode/Shimmer. Its saved scene file is unchanged.

Source: the user-supplied patternBasics.svg, archived unchanged here.
Its drawing extends outside its SVG page/viewBox. guide_curves.py reads the
actual vectors and renders the complete geometry in guide-review.png.

The two supplied left-side curves are mirrored about the medallion axis,
forming nested gold teardrop contours. Their staggered tips frame a small
three-leaf accent. The lines curve inward below the medallion instead of
following the flat root boundary. This develops the layered gold borders
seen in the original reference image.

Each outline is fitted to the individual approved mesh silhouette. A smooth
monotone mapping aligns the guide's circle and tip to the existing artwork.
The relocated icon, circular line and dotted ring remain pixel-identical
inside the protected medallion area. No new material or shader property,
mesh, rig, palette or sequence mapping is required.

REPRODUCTION
1. Run python output/inner-petal-pattern-guides/build_patterns.py.
   This writes only staged/Decorated Masks and local review evidence.
2. In Unity Edit Mode, run import-patterns.cs through Unity MCP eval_file.
   It replaces the eight existing source PNGs and rebuilds the existing
   Rootward Inner Medallions atlas in place, retaining its references.
3. Use capture.cs in the main wheel scene for the clean visual review.

This is the current pattern generator. The earlier
output/inner-petal-artwork/build_artwork.py reproduces the preceding simple
contour design; rerunning it directly against Assets would revert this pass.

BACKUP
before/Rootward Inner holds all original artwork, atlas, materials and .meta
files at the start of this revision. before also records the original main
scene, source generator, artwork preview, layout and selected asset hashes.

VALIDATION
pattern-validation.json confirms all eight protected medallions are unchanged
and the new contours retain positive spacing from the dotted circles.
wheel-validation.json: all 27 existing main-scene sequence checks pass.
icon-lighting-validation.json: all eight rendered icon-light alignment checks pass.
The petal shader reports no compilation errors.
unchanged-assets.json verifies the main scene, Blender source, production FBX
and both petal shader files have the same hashes as before this revision.
changed-artwork-files.json contains exactly the eight masks and shared atlas;
their .meta files, materials, symbol set and atlas source remain unchanged.
after.png is the final clean main-scene capture; the hand guide is hidden only
for that review capture and remains enabled in the running experience.

The shared Rootward Inner atlas also updates Deepavali_RestorationTest.unity.
No restoration code, settings, scene or background shader was changed here.

SHAPE INVESTIGATION
At the user's request, a separate chat named Investigate inner petal shape
irregularities was created (01a18346-8b26-7193-bd4b-670f00e6b4ff). It examines
the gold/Diya, green/Coins, cyan/Gift and purple/Lotus silhouettes offline and
prepares smoothing proposals separately. This pattern pass retains the
approved geometry; conforming gold contours can expose its local asymmetry.
