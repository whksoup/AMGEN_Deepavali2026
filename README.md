# AMGEN Deepavali 2026

Unity project for the AMGEN interactive LED wall's Deepavali flower experience.

## Open the project

1. Clone this repository. Source artwork and media are included directly; Git LFS is not required.
2. Open the repository folder in Unity Hub with **Unity 6000.0.55f1**.
3. Allow Unity to import assets and resolve packages (URP 17.0.4 and Input System 1.14.1).
4. Open `Assets/Scenes/Deepavali_PetalWheelSequence.unity` and press Play for the latest wheel/shimmer experience. `Deepavali_PaletteTest.unity` retains the direct per-petal appearance controls.

The repository root is the Unity project root. Unity caches and local editor settings are regenerated locally.

## Current progress

- Refined two-layer flower with 22 petals: 8 individual inner silhouettes and 14 outer leaves, including one merged north leaf. Inner roots are broad and planar, following Image 1 and Petal2.svg.
- Petal motion and mouse/touch selection.
- Independent per-petal palettes, artwork, and emission controls.
- Outward palette waves and independent root-to-tip artwork reveals, finishing the active transition before using the latest queued choice.
- Glowing center rim and six palette-associated icons: lotus, sunburst, star, sprout, diya, and people with a heart.
- Saturated, matte center fills that match their palette colors.
- Initial inner-petal state with eight individual symbols and fills, gold medallion patterns, yellow edge lighting, and a center mandala.
- Wheel, shimmer, lighting sequences and spatial-lighting study scenes use the refined meshes and per-leaf fitted artwork/light maps.
- Dark Yughues soil background, four corner oil lamps with flickering flames, and scattered marigold/burgundy petals. Corner placement adapts to the wall's aspect ratio.

In the review scene, select **All petals** and choose a palette to update the flower and center together. Select an individual petal to adjust it independently. **Initial state** restores the opening appearance; the next palette choice transitions its symbols, patterns, colors, and edge lighting into the selected phase.

![Center palette states](output/flower-center-fill/six-center-colors.png)

## Source and progress records

- Runtime scripts, shaders, models, materials, and prefabs: `Assets/HK_Signature/Deepavali/PetalPrototype/`
- Current Blender source: `output/flower-petal-prototype/Deepavali_InnerPetal_VideoFit.blend`; production export: `Models/Refined Flower/Deepavali_RefinedFlower.fbx` under the runtime folder.
- Integration, root-shape correction, recoverable backups, before/after images and Play Mode evidence: `output/refined-flower-integration/README.txt`.
- Scope: `Documentation/Deepavali-module-scope.md`
- Appearance implementation: `output/flower-appearance/README.txt`
- Artwork transitions: `output/artwork-transition/README.txt`
- Center implementation and icon provenance: `output/flower-center/README.txt`
- Initial inner appearance and verification: `output/flower-initial-state/README.txt`
- Background assets: `Assets/HK_Signature/Deepavali/Background/`; preview and verification: `output/deepavali-background/`
- Unity validation results: `output/flower-center/validation.json` and `output/artwork-transition/validation.json`
- Lighting notes: `reports/` and `research_notes/`

The current save includes the existing wall scenes and media as well as the Deepavali work. Wheel/roulette behavior is validated in the Unity Editor; final Android/device performance validation remains outstanding.
