# AMGEN Deepavali 2026

Unity project for the AMGEN interactive LED wall's Deepavali flower experience.

## Open the project

1. Clone this repository. Source artwork and media are included directly; Git LFS is not required.
2. Open the repository folder in Unity Hub with **Unity 6000.0.55f1**.
3. Allow Unity to import assets and resolve packages (URP 17.0.4 and Input System 1.14.1).
4. Open `Assets/Scenes/Deepavali_PaletteTest.unity` and press Play.

The repository root is the Unity project root. Unity caches and local editor settings are regenerated locally.

## Current progress

- Two-layer flower with 23 petals: 8 inner and 15 outer.
- Petal motion and mouse/touch selection.
- Independent per-petal palettes, artwork, and emission controls.
- Outward palette waves and independent root-to-tip artwork reveals, finishing the active transition before using the latest queued choice.
- Glowing center rim and six palette-associated icons: lotus, sunburst, star, sprout, diya, and people with a heart.
- Saturated, matte center fills that match their palette colors.
- Initial inner-petal state with eight individual symbols and fills, gold medallion patterns, yellow edge lighting, and a center mandala.
- Dark Yughues soil background, four corner oil lamps with flickering flames, and scattered marigold/burgundy petals. Corner placement adapts to the wall's aspect ratio.

In the review scene, select **All petals** and choose a palette to update the flower and center together. Select an individual petal to adjust it independently. **Initial state** restores the opening appearance; the next palette choice transitions its symbols, patterns, colors, and edge lighting into the selected phase.

![Center palette states](output/flower-center-fill/six-center-colors.png)

## Source and progress records

- Runtime scripts, shaders, models, materials, and prefabs: `Assets/HK_Signature/Deepavali/PetalPrototype/`
- Scope: `Documentation/Deepavali-module-scope.md`
- Appearance implementation: `output/flower-appearance/README.txt`
- Artwork transitions: `output/artwork-transition/README.txt`
- Center implementation and icon provenance: `output/flower-center/README.txt`
- Initial inner appearance and verification: `output/flower-initial-state/README.txt`
- Background assets: `Assets/HK_Signature/Deepavali/Background/`; preview and verification: `output/deepavali-background/`
- Unity validation results: `output/flower-center/validation.json` and `output/artwork-transition/validation.json`
- Lighting notes: `reports/` and `research_notes/`

The current save includes the existing wall scenes and media as well as the Deepavali work. Roulette and final Android/device validation remain outstanding.
