Camera tilt and rangoli framing - 28 October 2026

Saved in Assets/Scenes/Deepavali_PaletteTest.unity.
Camera: perspective, 35-degree vertical FOV, rotation (50,180,0).
This is 40 degrees away from the previous straight overhead view.
Position: approximately (0,1.417182,1.402771), aimed slightly towards the near edge
to keep the projected flower centred despite the perspective.
Rangoli root: uniform scale 1.686090. Resting vertical coverage increases from
74.82% to 90.00%, with approximately 5% top and bottom margins.
The background exclusion radius scales with the flower to keep petals clear.

PetalPickTarget now uses BakeMesh(..., true) so the picking snapshot accounts
for the enlarged renderer transform. Previously the root scale was counted twice.
Independent tests project source vertices through their bone weights, avoiding
the same mesh-baking path as the picker. All 8 targets pass at scale 1 and 1.686090.
Both 16:9 and the current wide-wall aspect keep the 4 corner lamps in view.
Measured Play Mode height: 90.01%, without clipping.

framing.json: camera, scale and measured bounds.
interaction-validation.json: layout, shader and picking checks.
after-1600x900.png / after-2560x1118.png: Play Mode previews.
before-scene.unity / before-PetalPickTarget.cs: pre-change backups.
apply-framing.cs, capture-framing.cs, validate-interaction.cs: Unity MCP eval_file helpers.
