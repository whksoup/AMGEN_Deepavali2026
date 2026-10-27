# Existing Deepavali scene diagnosis

## Is the camera or bloom pipeline disabled?

### Takeaway
No. Live read-only inspection on 28 October 2026 confirmed that the active clean scene is Deepavali_PaletteTest, with PC quality, PC_RPAsset, camera HDR, and post processing enabled. The principal post-processing limitation is the deliberately restrained Petal Subtle Bloom profile.

### Cited Findings
- Camera post processing is on and HDR is on; the active Global Volume references Petal Subtle Bloom. These saved settings agree with live Unity MCP inspection. [Scene camera and volume](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/Scenes/Deepavali_PaletteTest.unity:2987>)
- PC_RPAsset supports HDR and render scale 1. Live Unity reports PC quality and this current pipeline. [Pipeline](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/Settings/PC_RPAsset.asset:26>)
- Bloom values: threshold 1.3, intensity 0.16, scatter 0.35, clamp 8, High Quality Filtering false, Downscale Quarter, Max Iterations 3. Live enum decoding confirms Quarter. [Profile](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Petal Subtle Bloom.asset:74>)
- Tonemapping is overridden to None (mode 0). [Profile](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Petal Subtle Bloom.asset:16>)
- URP 17 source states threshold is gamma-space; PostProcessPass converts it through GammaToLinearSpace. Thus threshold 1.3 is approximately 1.77 in linear rendering space, with a soft knee. Clamp limits brightness used for bloom, not the displayed source surface. [Bloom source](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Library/PackageCache/com.unity.render-pipelines.universal@dd24be0d7cf1/Runtime/Overrides/Bloom.cs:35>), [Processing source](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Library/PackageCache/com.unity.render-pipelines.universal@dd24be0d7cf1/Runtime/Passes/PostProcessPass.cs:1348>)
- Current camera capture shows bright narrow ornament lines with little broad halo and an essentially uniform dark ground. [Current scene capture](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/research_notes/Deepavali glow and lighting/current_scene.png>)

### Inferences
- Turning on HDR or post processing again will not solve this scene; both already work. A sensible tuning trial is higher bloom intensity and scatter, Half resolution, and Max Iterations 5–6, with threshold around 1 as a starting point. Exact values require visual testing at target output resolution.
- Preserve small bright cores and increase halo width rather than simply brightening every surface. Tonemapping Neutral/ACES deserves an A/B comparison once stronger emission is present; None currently permits abrupt clipping on an SDR output.
- The serialized skipIterations property appears in the profile but is absent from this installed Bloom.cs/PostProcessPass implementation; do not recommend adjusting it as the primary control. Max Iterations is the implemented control.

### Gaps
- No settings were changed and no A/B tuning was performed. The screenshot and live inspection were in edit mode, not a playing deployed build. Display/LED wall calibration is unknown.

## What is the material shader currently emitting?

### Takeaway
There is already HDR-capable masked emission. Inner petals primarily emit at their border; their main and accent ornaments have zero emission. Outer ornaments have modest emission, so the reference's many luminous lines and dots require additional targeted ornament brightness as well as stronger bloom.

### Cited Findings
- The shader constructs emission as `(edge mask * fill * edge intensity + main ornament mask * main color * main intensity + accent mask * accent color * accent intensity) * emission scale`. It does not clamp the final emission to 0–1. [Emission calculation](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Shaders/PetalPaletteInput.hlsl:122>)
- Palette application explicitly converts authored sRGB colors to linear before passing them into the shader, and applies each palette's intensity. [Controller](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Scripts/FlowerPaletteController.cs:320>)
- `SetPetalEmissionScale` accepts any finite nonnegative scale; no upper cap exists in that method. The scene's visible Glow slider limits input to 0–3. [Controller](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Scripts/FlowerPaletteController.cs:208>), [Slider](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/Scenes/Deepavali_PaletteTest.unity:1759>)
- Inner main/accent intensity defaults to zero in shader and palette type; the inner palette asset only authors edgeIntensity. Live material reads confirmed inner `_MainPatternIntensity = 0`, `_AccentPatternIntensity = 0`, `_EmissionScale = 1`. [Defaults](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Shaders/PetalPaletteLit.shader:9>), [Palette](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Palettes/Deepavali Seven States.asset:16>)
- Outer petal materials have main ornament intensity 1.15, accent 0.55, scale 1. Both outer palettes use these levels. [Outer material](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Materials/Outer Petal Palette Tone 1.mat:36>)
- The `edge` palette color is explicitly legacy; current border emission uses fill color. Tuning the legacy edge color will not change the border emission. [Palette type](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Scripts/FlowerPaletteSet.cs:15>)

### Inferences
- Current base-state inner edge maximum is roughly 3 linear in its strongest channel (0.266 linear red × 11.263 intensity); outer edges approximately 1.6. Actual mask pixels reduce those values. This makes increasing bloom and targeted ornament intensity more productive than assuming emission is not HDR.
- Raise `mainPatternIntensity` and possibly `accentPatternIntensity` on the inner palette, and tune outer values, to make selected decorative details act as luminous sources. Keep fill darker so the result reads as light rather than a bright flat print.
- Current center disc is a non-emitting URP Lit material, so luminous center symbol/rim would require an emission mask or separate luminous geometry.

### Gaps
- No runtime palette transition was executed. Existing file contents and currently loaded edit-mode materials were inspected. Precise exposure-matched intensity ranges should come from a visual test, not be presented as guaranteed production settings.

## Why is there no colored light on the surroundings?

### Takeaway
The scene does not currently have dynamic emissive GI or colored spill lights. Bloom can add a visible screen-space halo, but the ground and nearby geometry need an explicit lighting contribution for the reference's colored pools of light.

### Cited Findings
- Live scene contains only Main Light (Directional, intensity 1.05, warm near-white) and Overhead Fill - No Shadows (Point, intensity 0.35, cool near-white, range 1.1). No palette-colored local light exists. Saved scene agrees. [Main light](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/Scenes/Deepavali_PaletteTest.unity:527>), [Fill light](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/Scenes/Deepavali_PaletteTest.unity:2739>)
- Realtime GI is disabled. Baked GI is enabled in settings, but live inspection found zero lightmaps, no static renderers, no assigned LightingSettings asset, and only builtin lighting data. [Saved GI settings](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/Scenes/Deepavali_PaletteTest.unity:47>)
- Petal material `m_LightmapFlags: 4` resolves live to EmissiveIsBlack. Therefore current material setup is not configured to contribute emissive GI. [Inner material](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Materials/Petal Palette Lit.mat:16>), [Outer material](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Materials/Outer Petal Palette Tone 1.mat:16>)
- Custom shader DOES have a Meta pass using stock LitMetaPass; stock implementation forwards `surfaceData.emission` to MetaInput.Emission. Do not diagnose missing Meta support. [Custom shader](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Shaders/PetalPaletteLit.shader:117>), [Installed LitMetaPass](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Library/PackageCache/com.unity.render-pipelines.universal@dd24be0d7cf1/Shaders/LitMetaPass.hlsl:15>)
- PC renderer mode is ForwardPlus, confirmed by live enum, not standard Forward. [Renderer](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/Settings/PC_Renderer.asset:56>)
- The ground is URP Lit, nonmetallic, smoothness 0.1, very dark blue base color (0.0203, 0.0242, 0.0437), no base/normal texture assigned. It can receive lights, but has little surface texture to reveal them. [Ground material](<F:/Freelance_Work/amgen-LEDWall/Amgen Interactive Wall/Assets/HK_Signature/Deepavali/PetalPrototype/Materials/Test Ground.mat:117>)

### Inferences
- For changing palette states and animated petals, a small set of realtime point/spot lights colored from the palette is the straightforward starting point. Place them close above the ground, with controlled range, and drive their color/intensity alongside the visual glow. A few lights can represent sectors of the rangoli; one shadowed light per petal is unnecessary.
- A soft colored ground mask/quad/decal is a cheap artistic alternative or supplement for this fixed presentation. It should be described as painted/faked light spill, not geometric light transport.
- Fixed baked emissive lighting could help static decorative lamps/environment if configured, but cannot automatically follow changing petal colors. Avoid categorical statements that Unity 6.0 URP never supports any realtime GI; this scene has it disabled and unconfigured.
- Reduce ambient/key light selectively to increase contrast, and give the dark ground subtle texture/normal variation so local illumination has visible structure. Pure exposure increase brightens the surrounding scene too.

### Gaps
- Performance has not been benchmarked on target hardware. Additional light counts and shadows must be tuned there. Shader Meta compatibility with any proposed new baking workflow has not been tested.
