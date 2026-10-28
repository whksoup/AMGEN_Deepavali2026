Horizontal rangoli scaling - 28 October 2026

Scene: Assets/Scenes/Deepavali_PaletteTest.unity
The user's camera is preserved at (0,2.16,0.938), Euler (69.271,180,0), FOV 35.
Its screen horizontal axis is world X, so the flower root's X scale is increased
by 30%: (2.191916,1.686090,1.686090). Both inner and outer rings widen together.
Vertical coverage remains 92.43%. Wide-wall horizontal coverage goes from 43.04%
to 55.96%; 16:9 coverage is approximately 72.07%.

This is a reversible scene transform override. Meshes, rigs and animations are
not resculpted. All ornament, including the center disc and symbols, widens with
the flower. Adjust the root Transform X scale to refine the width.
DeepavaliBackground now supports an elliptical clearance area: X/Z multipliers
(1.3,1) match this wider shape without removing extra dressing above and below it.

validation.json checks all eight petal picks using independently skinned vertices,
both visible petal rings, nonuniform mesh baking, camera preservation, animation
components and scatter clearance. Previews include 16:9 and the current wall ratio.
before-scene.unity and before-DeepavaliBackground.cs preserve the previous state.
