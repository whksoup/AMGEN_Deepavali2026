# Deepavali scenes and operation

Inspected 29 October 2026. All scenes below are under [Assets/Scenes](../Assets/Scenes). These are roles inferred from source, scene references and implementation records, not a new Play Mode validation.

| Scene | Role and useful entry points |
| --- | --- |
| [Deepavali_PetalWheelSequence.unity](../Assets/Scenes/Deepavali_PetalWheelSequence.unity) | Main integrated session: restoration, prompts, wheel, eight outcomes, scroll, touch input and inactivity. Begin feature work here. |
| [Deepavali_RestorationTest.unity](../Assets/Scenes/Deepavali_RestorationTest.unity) | Standalone restoration study, `ScreenRestorationController`, `RestorationLampResponse`, `RestorationPreview`; no integrated `DeepavaliSessionController`. |
| [Deepavali_PaletteTest.unity](../Assets/Scenes/Deepavali_PaletteTest.unity) | Direct per-petal appearance/artwork review with `FlowerAppearancePreview` and `FlowerPaletteController`. |
| [Deepavali_LightSequence.unity](../Assets/Scenes/Deepavali_LightSequence.unity) | Earlier sequence study using `FlowerLightSequence` and `FlowerSequencePreview`. |
| [Deepavali_PetalLightingPrototype.unity](../Assets/Scenes/Deepavali_PetalLightingPrototype.unity) | Spatial lighting study using `PetalLightingPrototype`. |
| [Deepavali_InnerLayerTest.unity](../Assets/Scenes/Deepavali_InnerLayerTest.unity) | Inner-layer pointer/picking study. |
| [Deepavali_TexturedPetalTest.unity](../Assets/Scenes/Deepavali_TexturedPetalTest.unity) | Textured petal, emission and pointer study. |
| [Deepavali_PetalTest.unity](../Assets/Scenes/Deepavali_PetalTest.unity) | Early motion/pulse prototype with `PetalMotionController` and `PetalPulseInput`. |
| [Deepavali.unity](../Assets/Scenes/Deepavali.unity) | Legacy `DeepavaliPlaceholderInteraction` prototype; the short filename is not the current integrated experience. |

Scene references can include disabled review components and inherited prefab components. Presence in YAML alone does not establish runtime ownership; check enable flags, prefab overrides and the session controller.

## Run the integrated experience

1. Open the repository in Unity Hub using **6000.0.55f1**; allow package resolution and asset/video import. Source media is stored directly in Git, without requiring LFS.
2. Open `Deepavali_PetalWheelSequence.unity` and enter Play Mode. Keep the Game view visible/focused, especially for video checks.
3. Native touchscreen input is the saved default. For desktop testing, press **M** in the Game view and enable **Mouse preview (temporary)**. There is no automatic mouse fallback. Close the menu with M when observing the full frame.
4. Wait for the center hand guidance to finish appearing, then dwell over it (default 0.65 s). Painting begins after the prompt dismisses. Sweep the image, including all four corner lamps.
5. Completion requires at least 80% coverage **and** all four lamps ignited. The full reveal finishes and shows the spin prompt over the continuing shimmer.
6. Move outside the prompt and back inside, then dwell (default 0.15 s). A stationary pointer left at center cannot consume the second prompt. Subsequent sessions also require a fresh entry.
7. The wheel lands, the selected flower color/symbol finishes revealing, then the matching scroll opens, holds and closes, then the transparent Happy Deepavali title plays over the selected rangoli and fades out before reset. Inactivity can return interaction/wheel/reveal phases sooner; the scroll and greeting presentation finishes uninterrupted.

## Inspect and tune

Use the [parameter and tooltip guide](parameter-guide.md) for each M-menu control's effect, saved default, slider range, and when changes take effect.

The **Deepavali Session** object contains the session, restoration, lamp response, debug UI and touch adapter. The controller references the flower, wheel, hand prompt, center light, scroll overlay and saved settings. Use [architecture](architecture.md) to choose the correct owner.

| Settings | Location / persistence |
| --- | --- |
| Session dwell, coverage, brush, inactivity, result/scroll timings | [Deepavali Session Settings.asset](../Assets/HK_Signature/Deepavali/Restoration/Deepavali%20Session%20Settings.asset); loaded at session `Start()`. Inspector or M-menu **Save to ScriptableObject** writes the asset in the Editor. |
| Wheel timing and lighting | [Petal Wheel Settings.asset](../Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Light%20Sequence/Petal%20Wheel%20Settings.asset), `PetalWheelLightSequence` and its settings partial. |
| Shared result mapping | [Deepavali Outcomes.asset](../Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Light%20Sequence/Deepavali%20Outcomes.asset); eight clockwise entries. |
| Flower artwork and palettes | [PetalPrototype](../Assets/HK_Signature/Deepavali/PetalPrototype), especially `Palettes/`, `Symbols/`, `Rendering/`, prefabs and scene overrides. |
| Result media | [Diwali Scroll Canvas.prefab](../Assets/DiwaliScroll/Prefabs/Diwali%20Scroll%20Canvas.prefab), `Assets/DiwaliScroll/Videos/`. |

Runtime changes in a build are temporary. Mouse preview is not captured by the session settings save. Do not use the standalone scroll package's click-to-toggle/random-selection instructions for the integrated session: the session owns exact variant selection and playback.

### Presentation controls (29 October 2026)

The M-menu now includes **Camera perspective** and **Spin animation** above the existing session controls:

- Pitch offset slider (-45 to +20 degrees), 2-degree up/down buttons and **Authored view** (zero offset). The camera orbits around the flower while keeping its distance and authored orientation relative to that pivot. Closing the menu keeps the selected view; disabling the session restores the authored camera transform.
- Spin speed in inner-petal slots/second, minimum cruise duration and slowdown duration. Changes affect the next spin; an active spin retains its planned motion. Total spin time also includes 0.9 seconds acceleration and landing alignment (up to one extra revolution), plus windup/overshoot if those are enabled in the wheel configuration.
- Logo fade duration after the scroll is fully open.

These values use the existing **Save to ScriptableObject** / **Load saved** buttons and the **session settings** asset. They do not edit the shared Petal Wheel Settings asset. Defaults retain the authored view and existing wheel speed/cruise/slowdown (16 / 1.2 s / 3.6 s). For long spin experiments, adjust **Idle time before return** too: the existing inactivity timer continues through the animation.

The integrated scene adds `Diwali Scroll Canvas/Diwali Scroll/AMGEN Logo Placeholder`, a noninteractive screen-space Image and CanvasGroup. It fades in over one second after the opening video finishes and hides when closing starts, on playback fallback, reset or session disable. The rectangle sits below the scroll artwork, anchored to the fitted video rectangle. To install the approved logo later, assign the Image sprite (preserve aspect is enabled), use white tint for unchanged artwork colors, and review its RectTransform on the target wall. The shared scroll prefab is unchanged. Changes to the existing symbols remain deferred. The Happy Deepavali finale is described below.

## Happy Deepavali finale (29 October 2026)

After the scroll is fully hidden, the selected rangoli stays visible behind the supplied transparent gold title. The title writes on for about 5.6 seconds, holds for 4 seconds (one idle loop), then fades out over about 1.4 seconds before restoration resets. **Happy Deepavali hold (seconds)** in the M-menu adjusts the fully written hold and persists through the existing session settings Save/Load buttons.

`Happy Deepavali Canvas/Happy Deepavali` owns the three clips under `Assets/HappyDeepavali/Videos/`. Its `Logo` child shows the supplied AMGEN text placeholder; replace its RawImage texture with approved artwork later. This is separate from the existing rectangle below the result scroll. Startup and click toggles are disabled in the integrated scene. Reset or session disable cancels both overlays immediately; video errors/timeouts also return safely to restoration.

## Host and deployment

[EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset) currently starts at `Startup` and contains eight existing wall scenes, none named Deepavali. Opening the main scene directly bypasses launcher navigation. Build inclusion, launcher entry/exit, target display/resolution, video performance and physical sensor calibration still require deployment work and checks. Do not infer these from the Game view or historical screenshots.
