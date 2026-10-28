Deepavali center pad and broad petal roots
2026-10-28

Saved in Assets/Scenes/Deepavali_PaletteTest.unity and the Deepavali Peacock Flower prefab.

Center disc: top lowered from approximately +0.0303 to -0.04586 world Y.
The underside reaches the soil at -0.08093 (soil Y -0.08), so the center reads as a grounded pad.
Final disc local position Y -0.0506; local scale (1,1,1.3).

Both rings now use custom meshes with a broad, shallow concave root edge:
two base corners and the existing pointed outer tip. Full unscaled base widths:
inner 0.11 units, outer 0.09 units. Original imported meshes remain available.
Rest incline: inner 9 degrees, outer 3.3 degrees, rising away from the center.
Sideways shingling keeps overlapping broad bases separated.
Inner wrapper offset uses local (0,-0.031,-0.027) rotated by its wrapper rotation.
Outer layer offset Y -0.0215.

The user's camera and flower scale (2.19191647,1.68608963,1.68608963) are preserved.
build-center-pad.cs regenerates the two mesh assets and updates the prefab in Edit Mode.
before-scene.unity and before-flower.prefab are pre-change snapshots.

Validation:
- 13 rest, breeze, pulse, and opposing-motion poses; 23 skinned petals.
- No petal-to-petal triangle intersections in those sampled poses.
- Minimum inner neighbor separation 0.763 mm, outer neighbor 3.075 mm,
  and inner-to-outer layer separation 1.691 mm at the current flower scale.
- Lowest petal point across sampled poses -0.07905, above the soil.
- All 8 inner petal touch targets passed; active animation and screen bounds passed.
- Both meshes: finite vertices, no zero-area triangles, valid normalized bone weights,
  and both UV channels retained.
- Camera pose preserved, no generated scatter inside the flower exclusion area.

Preview images: after-1600x900.png, after-2560x1118.png, petal-shapes.png.
Detailed reports: validation.json, mesh-health.json, clearance-validation.json.
Unity returned to Edit Mode and the scene was saved.
