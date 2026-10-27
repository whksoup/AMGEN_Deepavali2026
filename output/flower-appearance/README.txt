Deepavali flower appearance implementation
=========================================

This records the initial eight-petal appearance work. The shared review scene
now contains a two-layer, 23-petal flower. Artwork transitions and validation for
that current layout are documented in output/artwork-transition/README.txt.

Open Assets/Scenes/Deepavali_PaletteTest.unity and press Play.
- Initially, All petals is selected and every petal uses Base / Original artwork.
- Tap a petal to select it and pulse its bones. Palette buttons then affect that
  petal only. All petals switches subsequent controls back to the whole flower.
- Mixed colors requests one palette per petal (seven states repeat around eight).
- Next artwork cycles Original, Blank, Diamond, Ring, Rosette, Original.
  Changes now reveal radially from each petal's root to its tip. Each petal has
  an independent artwork clock and finishes its reveal before the latest choice.
  These four neutral sample masks demonstrate replacement; they are not final
  event artwork. Blank is useful for checking complete removal of the old pattern.
- Glow scales emission for the selection. Zero disables emission, not albedo.
  The slider does not interrupt an active palette transition.
- The review UI is optional. Application code calls FlowerPaletteController.

Implemented in order: independent palettes/emission, radial palette transition
and latest-request queue, then symbol erasure/atlas authoring and review controls.
Roulette and the later twelve-petal layer are outside this change.

Palette and emission ownership
------------------------------
FlowerPaletteController is the ONLY material writer. It creates eight stable
petal materials and one center material on enable. Textures remain shared.
Disabling restores original materials and destroys owned runtime materials.
Re-enabling applies the latest requested layout, scales and symbols immediately;
it does not resume an interrupted wave. Source material assets are never edited
by the runtime controller. The earlier FlowerEmissionController remains absent
from this scene and disconnected from its pointer input.

Each Petal Appearances entry matches the corresponding serialized Petals renderer
index: 0 = Inner Petal 01 at the top, increasing clockwise in this camera.
Each entry stores its requested palette, emission scale, symbol ID and artwork
coordinate mapping. Selected State controls the center disc and is the default
for newly added petals; it is not a summary of a mixed flower.

Emission RGB is the interpolated FILL color in linear space, multiplied by
interpolated palette edgeIntensity and the per-petal emissionScale. There is no
independent emission color override. FlowerPalette.edge remains serialized only
for compatibility with previous assets. Preset intensities were adjusted so each
fill hue produces the same peak linear emission of 3 at scale 1, preserving glow
for dark fills. Fill/ornament/accent/center colors have not been changed.

Runtime API examples (Amgen.Deepavali)
--------------------------------------
flower.SetState(FlowerPaletteState.Base);             // immediate whole-flower palette reset
flower.SetPetalState(0, FlowerPaletteState.Blue);      // immediate one-petal change
flower.SetPetalEmissionScale(0, 1.5f);                 // independent brightness
flower.TransitionTo(FlowerPaletteState.Purple);       // outward whole-flower wave
flower.TransitionPetalTo(0, FlowerPaletteState.Green);// outward wave on one petal
flower.TransitionToLayout(states, centerState);       // atomic mixed layout
flower.TransitionSymbol(0, 1);                        // root-to-tip Diamond reveal
flower.TransitionSymbol(0, -1);                       // root-to-tip original restoration
flower.SetSymbol(0, 1);                               // immediate Diamond, cancels artwork queue

Mutation methods return false for invalid indices, states, layouts or scales.
TransitionToLayout requires exactly PetalCount entries and copies them into
preallocated storage. GetPetalState returns the settled palette;
GetRequestedPetalState returns the latest requested destination.
IsTransitioning, TransitionProgress and HasPendingTransition expose wave status.
SetState explicitly cancels active/pending PALETTE waves. SetPetalState resets
only its petal's palette. Both preserve independent artwork reveals.
SetSymbol is the explicit immediate artwork API; normal UI and per-petal
PetalArtworkController.SetArtwork(int) use TransitionSymbol. GetSymbol returns
the latest requested artwork; GetSettledSymbol returns the last completed one.
SelectState(int), the legacy UnityEvent entry
point, now requests a wave rather than applying an immediate change.

Each of the eight petal wrappers now has PetalArtworkController, bound to its
renderer and flower. Its Inspector exposes an artwork dropdown, reveal duration
and softness, backed by the matching Petal Appearances entry (no duplicate
artwork state). In Edit Mode these set starting appearance; in Play Mode artwork
changes animate. SetArtwork(int), RestoreOriginal(), NextArtwork(), and
SetArtworkImmediate(int) are available for UnityEvents and application code.
The root-relative radial shader transition replaces the interior behind its
curved front and keeps the previous artwork ahead of it. Border and emission
remain intact. Artwork coordinates are rest-space mesh XY, independent of bones.
See output/artwork-transition/README.txt and validation.json for the extension.

Wave semantics and coordinates
------------------------------
Default duration 1.2 seconds; half-width softness 0.025 flower-local units.
All surfaces share one advancing radius, from the center beyond the farthest
rest-space bounds. Fill, both ornament colors, center tint and edge intensity
interpolate together. The center is a mode of the same lit shader and does not
sample either artwork texture or emit light.

During a wave, requests update ONE pending layout. The active wave finishes
before another starts. Whole-flower requests replace every pending destination;
single-petal requests replace only that petal's pending destination. Selecting
the active destination again clears that slot's obsolete pending request.
Already settled destinations do not start another wave.

The actual imported mesh has 191 entries in both UV0 (ArtworkUV) and UV1
(PetalCoord_Width_Length). Runtime wave coordinates use UV0, which is planar.
Per-petal Artwork Min/Size map UV0 into the imported mesh's XY plane; renderer
placement is captured relative to the flower root on enable. The radius uses
flower-local XZ and never animated vertices or current world positions.
Bone curl/sway and whole-flower transforms cannot make the wave crawl.
UV0 texture tiling stays identity; atlas placement has its own independent rect.
For a later layer, add renderers and author their planar mapping dimensions.
They use the same clock/radius as the inner layer; there is no layer restart.
Structural renderer/mapping edits during Play require disabling/re-enabling.

Textures and symbol authoring
----------------------------
Texture 1 remains Petal_Regions_RGBA.png:
  R original ornament coverage; G main/accent role (1 = main, 0 = accent);
  B neutral pigment detail; A geometric edge emission mask.
Texture 2 is one shared Sample Symbols.png atlas (512 x 1024):
  R new symbol coverage; G main/accent role;
  B replacement pigment detail (222/255 is approximately neutral);
  A ERASURE region, including the blank spaces between symbol strokes.

Use the full original petal ArtworkUV layout for each source tile. A source PNG
is packed DATA, not a colored symbol image. Keep alpha 255 throughout the interior
to remove all previous ornament and pigment detail, including where R is zero.
The shader protects the geometric edge separately using the original mask.
Alpha zero intentionally keeps original artwork in that region. Surface alpha
always remains opaque. Symbols take their colors from each petal's live palette.

To author a new atlas:
1. Prepare equally sized RGBA PNG masks. The samples are 224 x 480 pixels.
2. Create > Deepavali > Symbol Atlas Source, or edit Symbols/Sample Symbol Atlas
   Source.asset. Assign tiles and labels in stable ID order.
3. Click Build Shared Symbol Atlas in its Inspector. The editor-only builder
   pads/extrudes tiles, writes the PNG and FlowerSymbolSet, and configures linear
   data import, mipmaps, BC7 on desktop and ETC2_RGBA8 on Android.
4. Assign Generated Set to the flower's Symbol Set. Use its tile array indices.

The atlas is bounded to 2048 pixels per dimension. Default gutters are 16 pixels;
sampling is capped at mip 3 to avoid reading adjacent tiles. Opaque erase values
and unlit border-mask values have small compression-noise dead zones: this is
necessary to prevent old artwork leaking through BC7 rounding at 254/255 alpha.
Keep source PNGs; rebuilding replaces generated files while retaining their GUIDs.
No runtime texture baking, recolored symbol copies, mesh overlays or real lights.
At the current sizes, the two ETC2 RGBA textures together occupy approximately
1.33 MiB of compressed texels including mipmaps, excluding engine/driver overhead.

Validation and limits
---------------------
integration-validation.json is the final end-to-end Editor validation, using
Mobile_RPAsset. It covers all 28 sample-symbol/palette combinations, GPU erasure
regression (changing old coverage/color/detail must produce ZERO changed output
when erased), palette/brightness isolation, live queue timing, mouse/touch UI,
simultaneous pulses, pinned roots, stable radial mapping, geometry, bloom,
shadows, SRP compatibility, zero managed allocations during transition updates,
and three disable/re-enable cycles with nine live owned materials.
wave-validation.json checks precise queue/retrigger/immediate-reset semantics.
independent-validation.json records the first implementation-stage check; the
final integration test supersedes it. Tests/scripts are outside Assets.

Sample renders: 01-mixed-palettes.png, 02-wave-000..100.png,
03-mixed-symbols.png, 05-blank-erasure.png and 06-review-controls.png.
Historical output/flower-palettes reports describe the previous shared-material,
instant-palette implementation and should not be read as current architecture.

Android native build/shader compilation and actual tablet profiling remain
unverified. Measure CPU material updates, skinning, symbol sampling, bloom,
texture memory and touch-time mesh baking at the final twenty-petal scale.
The SRP Batcher reduces CPU setup; it does not merge these skinned draws into one.
