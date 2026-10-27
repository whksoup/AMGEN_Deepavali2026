Deepavali seven-state palette prototype

Open Assets/Scenes/Deepavali_PaletteTest.unity and press Play. The seven buttons
select Base, Gold, Pink, Purple, Green, Blue or Orange immediately. Base uses
the existing burgundy/gold color family; the six additional palettes follow
the supplied reference images. Petal taps still test independent bone pulses.
The chosen palette stays selected, with no timed flash or color transition.

Palette authoring
Assets/HK_Signature/Deepavali/PetalPrototype/Palettes/Deepavali Seven States.asset
contains the seven presets in enum order. Each has five sRGB colors:
- Fill: petal substrate.
- Ornament: main decorative strokes.
- Accent: the secondary ornament color (for example gold details in Blue).
- Edge: the emitted border light, plus its HDR intensity.
- Center: center-disc surface color.

Edit these fields to tune a state, then select its button again in Play Mode.
FlowerPaletteController.Selected State is also editable on the flower root;
its Apply Selected State context menu reapplies the current preset.
For future interaction code:
    controller.SetState(FlowerPaletteState.Blue);
SetState returns false for an invalid state. SelectState(int) is the UnityEvent
button entry point. No transition policy or animation clock is part of this API.

Rendering architecture
One packed 512 x 1024 RGBA texture is shared by every petal and every palette:
Assets/HK_Signature/Deepavali/PetalPrototype/Textures/Petal_Regions_RGBA.png
    R = ornament coverage
    G = main/accent blend within the ornament
    B = neutral pigment detail
    A = edge emission mask
It is linear data (sRGB off, alpha is data, not transparency), mipmapped and
clamped. Desktop import uses BC7; Android uses ETC2_RGBA8. Compressed texture
storage with mipmaps is approximately 0.67 MiB shared by all seven states,
excluding engine/driver overhead. No seven-image texture set is loaded.

The region map is extracted once in the Editor from the existing burgundy/gold
artwork and exact UV edge mask. Hue ratios are used only during authoring, not
per frame. If the ornament source is replaced with a different palette/layout,
rebake or author the region channels directly; do not assume the original
warm-color extraction thresholds fit unrelated artwork. Source images remain
in the earlier prototype assets. The mask preserves linework and pigment detail,
while removing the original fixed colors for palette assignment.

Petal Palette Lit is an opaque URP shader using URP's PBR lighting, shadow,
depth, depth-normal and meta passes. It takes one texture sample for artwork
regions and edge emission; lighting/shadow/reflection sampling is separate.
Its shared UnityPerMaterial layout passes Unity's SRP Batcher compatibility
check. It retains the existing skinning and conservative mesh bounds. There
are still eight skinned petals, 24 bones and 3,104 flower triangles.

FlowerPaletteController creates one shared petal material plus one center-disc
material when enabled, then changes only color constants on state selection.
It performs no Update loop, texture swap, texture rebuild or per-tap material
creation. Disabling restores the original asset materials. Use it as the sole
color owner on the new prefab; the earlier FlowerEmissionController belongs
to the earlier edge-only flash prototype scene.

Reusable prefab:
Assets/HK_Signature/Deepavali/PetalPrototype/Prefabs/Deepavali Palette Inner Layer.prefab
The existing subtle bloom settings are reused. Earlier textured/flash and
single-petal scenes remain available. The palette test has its own camera
framing and overlay buttons so color review and petal movement can coexist.

Validation
render-validation.json: seven distinct rendered states, all five palette colors
applied consistently, one stable shared petal material and texture, unchanged
meshes, rejected invalid states, SRP Batcher compatibility and no shader/runtime
errors. Renders used Mobile_RPAsset in the Windows Editor with the same pose,
camera and lighting; PNGs 00-base through 06-orange are review artifacts.
input-validation.json: real synthetic mouse/touch events through the UI, UI
press exclusion from petal picking, selected palette retained during petal taps,
independent pulses, root stability and correct disable/reenable cleanup.

Android device performance and a native Android shader/build have not been
measured. setup_unity_palettes.cs, render_and_validate_palettes.cs and
validate_palette_input.cs are editor-only Unity MCP helpers outside Assets.
