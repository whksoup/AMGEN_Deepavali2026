LATEST ARTWORK — 29 OCTOBER 2026
Current generator and validated symmetric profiles/ornament are in
../inner-petal-regularized/. layout.json here is synchronized with that
revision for existing consumers. The older build_artwork.py is historical.

INNER PETAL ARTWORK REVISION - 28 OCTOBER 2026

LATEST PATTERN PASS: The nested gold contour refinement now lives in
output/inner-petal-pattern-guides. Its build_patterns.py and import-patterns.cs
are the current pattern rebuild workflow. The rootward medallion placement
described below is retained. This folder's original generator reproduces the
earlier simple outline design and should not overwrite the newer masks.

Saved in Assets/Scenes/Deepavali_PetalWheelSequence.unity.
Authoritative scene flower root: Deepavali Peacock Flower.

All eight opening medallions move from V=.57 to V=.43-.47, toward the roots.
The change is 23.4-44.0 mm in the source model, depending on the petal.
The existing eight symbols and their upright camera orientation are retained.
Each leaf has an individual physical-UV mask: a circular medallion, 44 dots,
a stronger gold leaf contour 9 mm inside the edge, a separate fine contour
at 16.5 mm, and a small tip flourish. The approved geometry is unchanged.

References inspected: the original inner-petal-reference-image1.png and
reference-15-027.5s.png from the supplied 2026-10-28 21-57-14.mp4 recording,
plus the user's annotated rootward-placement screenshot.

ASSETS
Assets/HK_Signature/Deepavali/PetalPrototype/Symbols/Rootward Inner contains
eight source masks, one padded 2048x2048 atlas and symbol set, and eight
scene-specific materials. Existing glyph source images were reused.
build_artwork.py reproduces the masks; apply-artwork.cs imports/builds the
atlas and applies it only to the main wheel scene.

The Petal Palette shader adds _StartupMeshUV (default zero). When enabled,
startup artwork and its icon-lighting crop sample physical mesh UVs instead
of the legacy fitted coordinates. Each material's _PetalLightIconRegion is
recentered/resized to its new medallion. Other artwork still uses the existing
fitted maps. Palette waves, root-to-tip artwork reveals, lighting settings,
wheel timing, motion and renderer indices retain their existing behavior.

Scene changes comprise eight material assignments on the renderers and
matching appearance overrides, plus the new startup symbol set. Prefabs and
other review scenes were not changed. See scene-changes.diff and the initial
material-mapping.json for exact original/new material paths.

VALIDATION
27/27 existing main-scene wheel checks pass, including shimmer, startup,
entry pulse, all 32 target/windup/overshoot combinations, all win patterns,
result palette reveal, subsequent result changes and return to startup.
8/8 rendered icon-light checks pass: toggling only each winner's icon light
produces illumination centered inside that petal's relocated medallion.
These compare rendered changes to the corresponding deformed mesh location.
The shader reports no compilation errors. Blender and FBX checksums match
the snapshots made before this artwork revision.

before.png / after.png show the artwork change with the comparison camera.
The after.png review capture temporarily hides the hand guide for visibility;
the guide remains enabled in the actual scene. wheel-*.png retain the guide.
artwork-review.png shows individual templates at their physical proportions.
The saved main scene is ready for review; Unity was left in Play Mode/Shimmer.

RESTORATION COORDINATION HANDOFF
The independent screen-space restoration work can use the stable scene above.
No renderer, mesh, rig, UV channel, palette, symbol ID or wheel mapping was
renamed. Indices 0-7 are the eight clockwise inner petals; 8-21 are the outer
ring. Only the eight material assets and the startup symbol set are replaced.

FlowerPaletteController owns all runtime materials. Keep it enabled.
ResetToInitialState() restores opening palettes and medallions.
PetalWheelLightSequence on "Petal Wheel Light Sequence" owns shimmer/wheel.
ResetPreview() resets to startup/Shimmer and reapplies lighting after Start.
autoPreview=false stops the automatic demo cycle; it does NOT pause shimmer.
Disabling the sequence clears its stage overrides and releases shimmer lights;
it is not a freeze-current-frame API. Re-enabling resets to Shimmer if already
initialized. Explicitly ResetPreview() at the restoration completion handoff.

To hold flower motion at rest, cache and disable the eight PetalMotionController
components plus PetalRingPulse and OuterPetalLayerMotion. OnDisable restores
rest pose. Restore their prior enabled states when awakening; the inner idle
motion fades in from rest. Do not disable the whole flower object or material
owner. Cache/restore any preview UI and pointer states separately.

A post-bloom color/dimming overlay is compatible with this artwork. It will
deliberately attenuate the existing emissive outline, glyphs and shimmer while
dormant. At a fully revealed mask, return the original camera color without
extra exposure, tint or clamping so the established idle rendering is exact.
Keep that screen mask independent of _ArtworkProgress, _WaveProgress,
_PatternPaletteAlpha, _StartupAppearance and the fitted UV textures.

This revision does not activate or modify the deferred restoration draft.
Shared-scene editing and visual verification are complete for this revision.
The restoration chat can take the Unity Editor for its next authorized work.
