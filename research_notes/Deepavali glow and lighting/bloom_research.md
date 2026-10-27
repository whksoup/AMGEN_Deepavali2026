# Bloom HDR and camera research

## Which settings create the luminous halo?

### Takeaway
Bloom spreads bright image regions into a halo. Camera HDR and post-processing are necessary foundations for selectively blooming HDR emission, but the local investigator reports both are already enabled; the existing bloom is simply restrained.

### Cited Findings
- Bloom threshold selects brightness in gamma space; intensity controls strength; scatter controls extent. Clamp caps the signal used to calculate bloom, not the visible source pixel. Unity's documented defaults are threshold 0.9, scatter 0.7, clamp 65472. — [Unity 6 Bloom reference](https://docs.unity.com/en-us/engine/6000.0/manual/post-processing-and-full-screen-effects/urp/effect-list/bloom)
- Setup requires camera Post Processing, a Volume with Bloom override, and a camera Volume Mask that includes that Volume's layer. — [Unity setup](https://docs.unity.com/en-us/engine/6000.0/manual/post-processing-and-full-screen-effects/urp/add-post-processing)
- URP Asset HDR supports pixel values above 1; per-camera HDR also matters. Rendering into a RenderTexture makes its format relevant. — [URP Asset](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/universalrp-asset.html); [Camera reference](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/camera-component-reference.html)
- Installed URP 17.0.4 code resolves two misleading manual statements: intensity is a MinFloatParameter without a maximum, so values above 1 are supported; HQ filtering uses bicubic upsampling instead of bilinear. Half is the source default downscale; maxIterations defaults to 6 and allows 2–8. — [installed Bloom.cs](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Library/PackageCache/com.unity.render-pipelines.universal@dd24be0d7cf1/Runtime/Overrides/Bloom.cs:42>), lines 64–80; [official source counterpart](https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.universal/Runtime/Overrides/Bloom.cs)

### Inferences
- Based on the coordinator's live-scene finding (intensity .16, threshold 1.3, scatter .35, clamp 8, Quarter resolution, 3 iterations, HQ off), try this artistic starting preset: intensity **0.8** (explore .6–1.2), threshold **1.1** (1.0–1.2), scatter **0.6** (.55–.7), downscale **Half**, max iterations **6**, HQ **on**, tint **white**, dirt **off**. These are recommendations, not Unity defaults or a guarantee of a reference match.
- Keep clamp 8 initially if source peaks stay below it. If stronger source peaks are needed, try 32. Raising this alone does nothing when the source never exceeds 8.
- For thin contour textures, Half and HQ are preferable starting points to Quarter; tune on the actual target resolution. The blur pyramid averages tiny details, so insufficient line coverage can weaken or destabilize their glow.
- Build an emission hierarchy: colored petal body below bloom threshold, strong outline/ornament emission, brightest tiny spark accents. Start intended glowing RGB peaks around 3–8 before tonemapping if the custom shader allows it; tune per palette. This describes shader radiance/multipliers, not the color picker's EV stops.
- Increase source emission until lines cross the threshold, then tune bloom intensity/radius. Lowering threshold globally toward zero risks making ordinary surfaces hazy.

### Gaps
- Actual chosen values need visual comparison at the LED wall's resolution, viewing distance, and calibrated brightness. No scene or shader was changed by this researcher.

## Should exposure or tonemapping provide the effect?

### Takeaway
Use Neutral tonemapping as the first comparison and leave Post Exposure near zero while adjusting emission/bloom. Exposure is a final brightness control, not the way to make more source pixels enter URP bloom.

### Cited Findings
- Neutral compresses range with minimal hue/saturation change; ACES introduces more contrast and changes hue/saturation. — [Unity 6 Tonemapping](https://docs.unity.com/en-us/engine/6000.0/manual/post-processing-and-full-screen-effects/urp/effect-list/tonemapping)
- Post Exposure operates after HDR effects and before tonemapping. — [Unity 6 Color Adjustments](https://docs.unity.com/en-us/engine/6000.0/manual/post-processing-and-full-screen-effects/urp/effect-list/color-adjustments)
- Installed source confirms the order: bloom addition at UberPost.shader lines 204–205, color grading/PostExposure at line 244; PostProcessPass.cs line 1533 computes the exposure multiplier as 2^EV. ColorAdjustments.cs lines 15–18 explicitly describes the ordering. — [UberPost.shader](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Library/PackageCache/com.unity.render-pipelines.universal@dd24be0d7cf1/Shaders/PostProcessing/UberPost.shader:204>); [PostProcessPass.cs](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Library/PackageCache/com.unity.render-pipelines.universal@dd24be0d7cf1/Runtime/Passes/PostProcessPass.cs:1533>); [ColorAdjustments.cs](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Library/PackageCache/com.unity.render-pipelines.universal@dd24be0d7cf1/Runtime/Overrides/ColorAdjustments.cs:15>)

### Inferences
- Current None tonemapping can make strong HDR highlights clip on SDR output; compare Neutral before assuming more emission is improving the look. ACES is a useful alternate look, but blue and orange may change visibly.
- Keep bright cores narrow so colored fills and surrounding darkness preserve the reference's contrast. Turning up all petal emission risks losing the pattern to pale/white regions.
- Practical camera answer: HDR + Post Processing + correct Volume; tune the Volume and source emission. Do not recommend HDRP Exposure overrides or physical-camera ISO/shutter tuning as a URP glow solution. A local Runtime source search found no camera.iso/shutterSpeed references in this URP package; it found explicit postExposure processing.

### Gaps
- No exhaustive physical-camera custom-script audit was performed; a project-specific script could separately map ISO or other settings to exposure.

## Does the LED wall require HDR display output?

### Takeaway
Internal HDR rendering and HDR display output are separate. The glowing image can be rendered internally in HDR, bloomed, tonemapped, and sent to an SDR display.

### Cited Findings
- Unity enables HDR output through additional Player settings beyond URP Asset HDR. HDR output requires a compatible display chain and introduces paper-white and luminance configuration. — [Unity HDR output setup](https://docs.unity3d.com/cn/6000.0/Manual/urp/post-processing/enable-hdr-output-urp.html); [Unity tonemapping HDR-output properties](https://docs.unity.com/en-us/engine/6000.0/manual/post-processing-and-full-screen-effects/urp/effect-list/tonemapping)
- Camera Dithering helps reduce 8-bit banding in gradients and dark areas. — [Unity camera reference](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/camera-component-reference.html)

### Inferences
- Do not switch the installation to HDR output just to obtain bloom; first establish whether the LED processor and playback path support it. Maintain crisp outline pixels and use smooth halos that still read at delivery resolution.
- If large dark glow gradients band on the actual output, test camera dithering. Preserve dark surroundings to make the small bright regions perceptually luminous.

### Gaps
- LED panel nits, color management, processor, input format, pixel pitch, wall resolution and viewing distance were not supplied, so no hardware brightness value is justified.
