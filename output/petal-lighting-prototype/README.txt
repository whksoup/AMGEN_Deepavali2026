Two-petal spatial lighting prototype - 28 October 2026

Open Assets/Scenes/Deepavali_PetalLightingPrototype.unity and press Play.
The scene is a copy of the existing palette-review scene. The original scene was
not edited. Only petal indices 0 (first inner) and 8 (first outer) receive the effect.

Right panel:
- Inner / Outer tabs select independent settings.
- A/B button compares current lighting with the spatial-light prototype.
- Focus isolates and frames the pair; turn it off to see them in the full flower.
- Shimmer amount and speed control smooth variation within fixed bright pools.
- Dark gap coverage changes how much of the outline is dark. Depth controls how
  far those gaps dim. At zero depth the gaps disappear.
- Growth progress scrubs the side-root to tip arrival field. Turn growth off for
  full coverage, or use Loop both to grow, hold at the tip, and fade back.
- Tip brightness is independent of the edge and deliberately overrides gaps at
  the tip. Set it to zero to inspect the shimmer alone.
- Edge brightness and soft wash width tune the sharp rim and inward glow.
- Copy to other copies the look while preserving each petal's random seed.
- Reload saved restores the saved asset. Save look as scene defaults writes the
  current two looks to Two Petal Study.asset, including during Editor Play Mode.
  Unsaved slider adjustments last for the current play session only.

Default asset: Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Spatial Lighting/Two Petal Study.asset
Additional Inspector settings on this asset: light color, wash strength and seed.
Cycle duration is on the scene's Petal Lighting Study component.

Implementation:
- 512 x 512 linear, uncompressed RGBA data maps generated from each mesh's UV
  boundary. R is inward outline distance / 0.15 petal lengths; G is arc length
  root-to-tip on the nearer side; B is persistent gap variation; A is arrival.
- Original artwork UVs make the lighting follow mesh skinning. No mesh changes.
- PetalSpatialLighting.hlsl replaces the selected petals' rim emission and
  modulates existing ornament emission near the rim so gold lines do not erase
  dark gaps. Tip brightness and inward wash are separate from shimmer.
- FlowerPaletteController retains sole ownership of the runtime materials.
  The preview component supplies parameters via SetSpatialLighting.
- Shader feature defaults to off on existing materials and all other petals.
- Camera bloom remains unchanged. The full-flower spill-light rig is temporarily
  disabled in focused view and restored in full-flower view/on component disable.
  Full-flower spill remains the existing palette proxy; it does not simulate
  spatially accurate light transport from the new shimmer.
- This is a visual prototype, not a final asset polish or target-device benchmark.

Validation: validation.json records 11 passing checks, including visible effects
from growth/shimmer/gaps/tip controls, exact A/B pixel restoration, only two active
materials, no modifications to the other 21 materials, cleanup on disable, map
import settings and no shader errors. Fixed-clock renders show progress at
0, 0.35, 0.65 and 1. Setup and capture helpers are kept alongside the report.
01-original.png and 05-growth-100.png are directly comparable camera captures.
The user reference is included in the panel for visual comparison.
