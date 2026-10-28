Deepavali tonal refinement - 28 October 2026

Scene: Assets/Scenes/Deepavali_PaletteTest.unity
before.png is a fresh capture of the preceding tonal comparison.
after.png uses identical framing, opening artwork/palettes and petal rest pose.
Both are 1400 x 1400 Unity camera renders, without overlay controls.

Changes:
- Petal rims: 2.6x inner / 3x outer emission multipliers.
- Internal ornament emission: 0.30x inner / 0.24x outer.
- Center icon glow: 2.3 -> 0.45; dotted ring: 1.1 -> 0.30.
- Center rim: 0.95 ring glow x 2.6 rim scale, compared with 1.3 previously.
- Full-strength body masks with broad root-to-tip, cross-petal and bowl shading.
- Center mask includes a broad annular recess beneath its rim.
- Body sheen: 0.16 of previous dielectric reflectance. Ornament retains its sheen.
- Local spill-light estimates follow the new rim/ornament multipliers.
- Bloom intensity 0.80 -> 0.55; scatter 0.60 -> 0.38 to preserve dark recesses.
- Ambient/key/fill light, tone mapping, base palette colors and geometry unchanged.

New assets are in:
Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Tonal Refinement
Previous Tonal Comparison material/mask assets and screenshots remain available.
The shader adds Body Sheen, Rim Glow Multiplier, Interior Ornament Glow Multiplier
controls with neutral defaults for other materials. The specular workflow uses
0.04 default reflectance, matching the previous nonmetallic petal shading.

Verification: all 7 palettes across 23 petals; startup palette restoration;
finite palette-driven spill lighting; center settings; SSAO enabled;
no shader errors; SRP Batcher compatible. See validation.json.

setup.cs generates the linear data masks and configures the scene.
capture-before.cs / capture-after.cs render the fixed comparison in Play Mode.
before-source contains snapshots from the start of this refinement, including
the then-current user changes. These are not repository HEAD snapshots.
