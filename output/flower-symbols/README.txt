Flower symbol assets - stage 1
=============================

Eight side symbols and one separate initial-center mandala.

Review:
  symbol-review.png      All nine symbols, gold tint for legibility only.
  unity-00..07.png       Actual inner-petal mesh and shader, one icon per render.
  unity-08.png           Actual center-disc mesh and shader with the mandala.

Project assets:
  Assets/HK_Signature/Deepavali/PetalPrototype/Symbols/Initial State/
    Icons/                         Nine individual 512 x 512 coverage masks.
    Petal Masks/                   Eight 224 x 480 packed artwork masks.
    Initial Petal Symbol Source.asset  Rebuildable atlas source.
    Initial Petal Symbols.png       Shared 1024 x 2048 petal atlas.
    Initial Petal Symbols.asset     FlowerSymbolSet with eight stable IDs.
    Initial Center Mandala.asset    Separate one-entry FlowerSymbolSet.

Petal symbol IDs (these are asset IDs, not clockwise placement assignments):
  0 Lotus
  1 Sunburst
  2 Star
  3 Sprout
  4 Diya
  5 People and Heart
  6 Gift Box
  7 Stacked Coins

Provenance
----------
IDs 0-5 are pixel-identical tile extractions from Center/Center Icons.png.
The original phase atlas and six-phase center mapping are unchanged.
The gift, coins and mandala were generated as one source atlas with the built-in
image_gen tool, using the phase atlas as a style reference and the user's flower
render as a subject reference. generation-prompt.txt records the complete prompt;
gift-coins-mandala-generated.png preserves the generated source. Only those three
new glyphs are centered and resized to match the existing icon scale.

Preparation and rendering
-------------------------
Icons are white coverage on black, with no baked palette or glow. The petal atlas
uses the existing shader contract: R coverage, G=255 main ornament, B=222 neutral
pigment, A=255 erase old artwork. The shader retains its existing protected edge.
Each icon is placed inside a physical 0.146 m square at ArtworkUV (0.5, 0.62).
The source image is resampled to compensate for the inner mesh's 0.182 x 0.263 m
UV bounds; this preserves shape proportions despite rectangular source textures.

The petal atlas has extruded 16 px gutters, linear sampling, mipmaps capped at 3,
BC7 desktop compression, and ETC2_RGBA8 Android import settings. Individual icon
masks use BC4 on desktop and ETC2_RGB4 on Android. Colors and light come from the
material at runtime. Gold/purple in the review captures is a neutral review setup,
not the final flower layout.

Stage boundary and next integration
-----------------------------------
This stage creates and imports assets; it does not assign a new starting layout
to the existing scene or prefabs. The palette scene already had separate ongoing
lighting changes, which this work preserves.

For the next stage, assign Initial Petal Symbols.asset to the flower's Symbol Set
and use IDs above for the eight inner petals. Outer petals can keep symbol -1.
Placement/orientation, eight different colors, yellow edges and purple back petals
remain part of the initial-state setup stage.

Initial Center Mandala.asset contains one entry, ID 0. It is an independent mask
catalog, NOT a replacement for the controller's six-entry Center Icon Set. The
current runtime maps Base to no icon and phase states to the six existing icons.
Add an initial/Base mandala selection when wiring the initial-state behavior;
retain the original six-phase center mapping. The center render here supplies the
single mandala tile directly to a temporary material to verify shader compatibility.

Rebuild from the Unity project root:
  python output/flower-symbols/prepare_symbols.py
Then run import_symbols.cs using Unity MCP eval_file in Edit Mode. This uses the
existing FlowerSymbolAtlasBuilder, and preserves generated asset GUIDs on rebuild.
render_symbols.cs renders copies of the actual meshes in a disposable preview
scene without modifying the open scene, controller, or source materials.

Validation
----------
source-validation.json: six exact original-tile matches; nine nonempty glyphs
inside safe margins; eight fully opaque erasure masks with the expected channels.
import-validation.json: eight valid petal atlas rects and one valid center rect;
loaded BC7 petal atlas and BC4 center mask; open scene remains clean.
render-validation.json: all nine Unity renders contain visible ornament; shader
diagnostics empty. Proportions and placement checked visually on the real meshes.
Android import settings are configured; no Android device/build test was run.
