Flower center ring and palette icons
===================================

Open Assets/Scenes/Deepavali_PaletteTest.unity and press Play. Select All petals,
then a palette button to change the whole flower and its center together.
Individual petal palette/artwork changes continue to leave the center alone.

Center mapping:
  Base   : no icon; ring and dots remain
  Gold   : lotus
  Pink   : sunburst
  Purple : four-point star
  Green  : sprout
  Blue   : diya lamp
  Orange : three people with a heart

The six icons are clean redraws based on the user's two reference images,
generated as one monochrome atlas with the built-in imagegen tool. The exact
prompt is saved in icon-generation-prompt.txt, and reference copies accompany
this README. Project asset:
  Assets/HK_Signature/Deepavali/PetalPrototype/Center/Center Icons.png
Runtime tile catalog:
  Assets/HK_Signature/Deepavali/PetalPrototype/Center/Center Icons.asset

Controls and behavior
---------------------
FlowerPaletteController has a Center ring and palette icons section:
  Center Icon Set   : six entries in Gold/Pink/Purple/Green/Blue/Orange order
  Center Icon Scale : icon size relative to disc diameter (default 0.65)
  Center Ring Radius: radius relative to the disc (default 0.92)
  Center Ring Width : half-width relative to disc radius (default 0.012)
  Center Ring Glow  : rim brightness multiplier (default 1.3)
  Center Icon Glow  : independent icon brightness (default 1.3)
  Center Dot Glow   : subtle dotted ring brightness (default 0.5; zero hides it)

The palette center color is the disc fill, passed as authored sRGB so Unity
converts it to linear exactly once. The center uses matte diffuse lighting;
neutral dielectric reflections no longer wash out the saturated palette colors.
Direct lights, shadows, baked GI, and ring/icon emission still apply. Petals keep
their existing PBR lighting. The rim emission uses
the same palette fill hue and edge intensity as petal edges. The icon and dotted
ring use the palette's main ornament color. No real lights or extra meshes were
added. Both inner-layer and two-layer prefabs reference the shared icon set.

Center artwork uses the SAME outward rest-space radius as its palette color.
The next icon appears behind the circular wavefront, preserving the old icon
ahead of it. A palette change during a wave updates the latest pending choice;
the active wave finishes before the next starts. Brightness edits do not reset
the active wave. Original per-petal artwork clocks remain independent.

Application API (Amgen.Deepavali):
  flower.TransitionTo(FlowerPaletteState.Blue);        // whole flower + diya
  flower.TransitionCenterTo(FlowerPaletteState.Green);// center only + sprout
  flower.SetCenterState(FlowerPaletteState.Gold);     // immediate center-only lotus
  flower.CenterState;                               // last completed center state
  flower.SelectedState;                             // latest requested center state
  flower.GetCenterIcon(state);                      // 0..5; -1 for Base/invalid

FlowerPaletteController.Center.cs implements center settings, mapping, and API.
The main controller remains the sole material writer. Disabling restores the
original center material; re-enabling applies the latest requested appearance.

Texture and rendering details
-----------------------------
The source atlas is 1536x1024, 3 columns by 2 rows, read left-to-right/top-to-bottom.
Unity imports it at 2048x1024 to allow block compression, using normalized tile
rectangles so icon proportions stay unchanged. Desktop: BC4, confirmed loaded.
Android: ETC2_RGB4 configured. Both use linear data, mipmaps, Clamp/Trilinear,
texel-center rectangles, and a safe maximum mip of 4. Empty black margins prevent
neighbor tile bleed. Only the red coverage channel is sampled; near-black noise
is suppressed while preserving antialiased line edges. No runtime CPU texture
access or baking is required.

The rim and 48 dots are shader shapes with derivative-based antialiasing. They
remain smooth without a separate ring mesh or mask texture. The existing center
mesh, material ownership, opaque lighting/shadows, and SRP Batcher are preserved.

Validation
----------
validation.json records passing Unity Play Mode checks on Mobile_RPAsset:
  - Six distinct mapped icons and seven rendered states including Base.
  - Single center-fill color conversion and saturated rendered fills in all six
    palettes (measured linear RGB saturation 0.85 to 0.98 in an empty fill region).
  - Exact old/target image agreement outside the radial transition band:
    4,344 inner and 28,752 outer tested pixels, zero mismatches.
  - Ring emission changes only near the rim; interior difference is zero.
  - Independent icon/ring brightness and preserved active-wave timing.
  - Latest-request queue, center-only changes, simultaneous petal artwork.
  - Zero managed allocations in 256 combined transition updates.
  - No shader/runtime errors, SRP compatibility, unchanged mesh, no material leak.
  - Compressed atlas, correct cleanup and latest state on re-enable.

The existing 23-petal artwork regression also passes after the shared shader
change (output/artwork-transition/validation.json).
six-center-states.png is a contact sheet of direct Unity renders in reference
order. flower-*.png shows each state in context; center-*.png is its close-up.
Android native shader compilation/build and device profiling remain unverified.
