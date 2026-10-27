# Surrounding light and the Deepavali look

## What actually makes the reference feel luminous?

### Takeaway
The reference combines sharp bright contours, narrow halos, broader colored spill, and dark textured surroundings. Recreating those layers is more effective than making every part of the petal material equally bright.

### Cited Findings
- Unity defines bloom as an image effect spreading light outward from bright areas to suggest a camera overwhelmed by brightness. This is the visible halo layer. — [Unity 6.0 Bloom](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/post-processing-bloom.html)
- URP Lit supports an emission texture and HDR emission color/intensity. The texture determines where emission exists; it need not cover the entire surface uniformly. — [Unity 6.0 Lit shader](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html)
- The supplied reference visibly has bright outlines and dots, darker petal interiors and center medallions, restrained colored pools around the mandala, and warm diya highlights distinct from the blue/green states. These are visual observations of the provided image, not evidence of the original image's rendering technique. — [User reference](C:/Users/whkso/AppData/Local/Temp/codex-clipboard-b1123dfb-efd2-4125-91a5-429076ff234c.png)

### Inferences
- Preserve a narrow bright core in contours and dots; allow a halo around those cores, while the petal fill and central badge remain appreciably darker. Making the entire petal emit strongly will flatten the sculpted appearance and erase contour hierarchy.
- Reduce the scene's broad neutral/cool lighting enough that emission becomes the brightest visual feature. Retain subtle background texture and readable marigolds instead of crushing everything to black. A dim directional key can preserve shape while colored local lights supply the apparent spill.
- Keep bloom tint neutral so green, blue, and orange emitters naturally retain their own halo colors. Keep diya flames and their local illumination warm through all seven palette states.

### Gaps
- A single still reference cannot establish whether its spill was physically rendered, painted, composited, or generated. These recommendations target the visible result.

## How should the changing flower illuminate its surroundings?

### Takeaway
Use a few realtime light sources driven by the existing palette transition for actual shading on the floor and flowers. Bloom and material emission alone are not a substitute for that lighting setup.

### Cited Findings
- URP Lit's Baked emission option contributes through baked lightmaps; its Realtime option only affects other objects when Enlighten realtime GI is configured. None disables emissive light received by other objects. Merely changing the visible material emission does not establish such a GI system. — [Unity 6.0 Lit shader](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html)
- Realtime lights can change during gameplay and supply direct illumination; baked lighting is computed in advance. Mixed lights retain baked components when their runtime properties change. — [Unity 6.0 Light modes](https://docs.unity.com/en-us/engine/6000.0/manual/lighting-overview/lighting-light-sources/components/configuring/light-modes/introduction)
- In ordinary Forward rendering, URP permits at most one main light plus eight additional lights per object; the project's configured additional-light limit can be lower. Forward+ removes the per-object limit, with a per-camera limit still applying. — [URP light limits](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lighting/light-limits-in-urp.html), [Forward and Forward+](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/rendering/forward-rendering-paths.html)

### Inferences
- Suggested starting rig, subject to scene scale and visual testing: three or four broad shadowless realtime point lights distributed around the flower, slightly above the receiving floor. Aim their combined range at the nearby ground and decorations rather than the whole frame. These counts are an artistic recommendation, not a documented requirement.
- The local scene investigator reports that the project already uses Forward+, so the Forward per-object cap is explanatory background, not a current obstacle or reason to change rendering paths. The same investigator reports realtime GI disabled and no usable baked lightmaps/static receivers despite baked GI being enabled; treat their scene notes as the authority for exact file evidence.
- Interpolate the spill lights' hue and intensity with the same seven-state palette blend that drives the petal emission. Give spill its own intensity control so increasing contour emission does not automatically overlight the floor.
- Keep the existing fixed fill subordinate to the palette spill. If the current cool fill remains relatively strong, the blue state may look plausible while green/orange states feel disconnected from their surroundings.
- A broad, soft, palette-tinted floor halo mesh can supplement the light rig for a controlled pool of color. It is an inexpensive artistic layer for a fixed camera, not actual illumination: it cannot supply correct shading on raised flowers. A separate emission term in the floor shader is another option. Use a soft mask and limited brightness so the floor texture remains legible.
- Use actual lights for three-dimensional decorations, and the optional halo for broad continuity. Avoid one light per petal: it creates unnecessary overlap, complexity, and potential Forward light-limit behavior. Do not switch renderer solely for the initial three/four-light experiment.
- The receiving floor and decorations must use shaders that respond to the chosen lights. An unlit receiver or custom shader that only evaluates the main directional light will not acquire point-light spill simply because point lights exist.

### Gaps
- Exact intensity and range require scene scale, receiver roughness/albedo, and target display validation. The values cannot be determined from the image alone.
- Current receiving-shader support should be confirmed by the local scene investigator before implementation.

## Should this use baked GI, Enlighten, or a larger rendering change?

### Takeaway
Baked emission can look excellent for a fixed palette, but realtime light proxies are a more direct fit for the interactive seven-state flower. There is no need to migrate to HDRP to achieve the reference's visible effect.

### Cited Findings
- Unity 6.0's official pipeline table lists precomputed Enlighten realtime GI as supported in URP, with support ending after Unity 6. It lists screen-space GI as unavailable in URP. Therefore a categorical claim that URP has no realtime GI would be inaccurate for this version. — [Unity 6.0 pipeline comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/render-pipelines-feature-comparison.html)
- Enlighten precalculates visibility and updates indirect lightmaps over multiple frames. Unity recommends it for slowly changing, significant lighting and explicitly advises against rapidly changing special-effect lights because of latency and CPU cost. — [Unity 6.0 Enlighten introduction](https://docs.unity3d.com/6000.0/Documentation/Manual/realtime-gi-using-enlighten.html)
- Unity's decal system projects surface materials onto meshes and does not project onto transparent surfaces. It is a surface treatment rather than a general replacement for lighting calculations. — [Unity 6.0 decals](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/renderer-feature-decal.html)

### Inferences
- For fixed decorative diya lighting or a single static presentation, bake suitable indirect lighting. For the flower's palette-responsive color spill, use realtime lights or a purpose-built palette-driven floor effect. A single baked result will not naturally follow seven changing emissive colors.
- Enlighten would require deliberate scene preparation and compatible shader/GI data; it is not a camera toggle or an emission-strength fix. Its latency, setup, and stated lifecycle make it a poor first investment for this particular effect.
- The most practical stack is selective HDR emission + bloom + a few palette-driven realtime lights + dim textured surroundings. Optionally add a soft floor halo if the art direction calls for a larger pool than the lights produce cleanly.

### Gaps
- Official Unity 6.0 decal documentation is inconsistent: the feature comparison says no emissive decals, while the custom decal page describes an emission mask. Avoid promising stock emissive decal behavior without testing the installed URP 17.0.4 implementation; a transparent mesh or floor-shader term is the straightforward alternative.
