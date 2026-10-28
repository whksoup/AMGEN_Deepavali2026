Deepavali first tonal comparison - 28 October 2026

before.png / after.png are actual Unity camera renders, 1400 x 1400.
Both use the opening palettes/artwork, identical orthographic framing, and the
same restored petal rest pose. Overlay review controls are excluded.

Applied in Assets/Scenes/Deepavali_PaletteTest.unity:
- Flat ambient RGB: (0.30, 0.32, 0.40) -> (0.10, 0.107, 0.133).
- Overhead Fill - No Shadows intensity: 0.15 -> 0.04.
- Three linear RG masks: inner petal, outer petal, center. R shades the body;
  G attenuates indirect lighting in recesses. Petal masks follow mesh boundaries.
- Body strength 0.85; occlusion strength 0.70. Masks exclude ornament coverage.
- Custom forward shader now compiles _SCREEN_SPACE_OCCLUSION variants.
- Scene overrides use three dedicated Tonal materials in
  Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Tonal Comparison.
  Existing source materials are unchanged; new shading defaults to zero elsewhere.

Held constant: palette colors, artwork, all emission expressions/parameters,
center icon/ring emission, directional light, palette spill lights, bloom,
tone mapping, camera exposure and the center color-space behavior.

Verification:
- before-emission.json and after-emission.json match exactly across 24 surfaces.
- Post-processing profiles/settings match exactly.
- No shader compilation errors; SRP Batcher compatible.
- SSAO on/off render test confirms an effect on the flower itself, not only ground.
- validation.json contains the results.

capture.cs reproduces a fixed-framing opening-state screenshot in Play Mode.
setup.cs builds the masks and assigns scene overrides in Edit Mode.
before-source contains the pre-comparison scene/shaders, including the user's
pre-existing work; these snapshots are not the repository's HEAD revision.
