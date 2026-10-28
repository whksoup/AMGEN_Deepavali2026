# Deepavali scroll and eight-state synchronization plan

Prepared and implemented 29 October 2026 after the wheel integration. **The scroll and shared eight-outcome mapping are now wired into `Assets/Scenes/Deepavali_PetalWheelSequence.unity`.** The sections below retain the original audit and design rationale; see the implementation record at the end for the delivered files and validation.

## Agreed behavior

- Select one outcome for the round. The wheel's landed petal, final rangoli color and symbol, and scroll color must all represent that same outcome.
- Finish the wheel and its flower color transition, wait approximately **2 seconds**, then play the matching scroll's opening animation.
- The user confirmed that **15 seconds means time fully open**, excluding opening and closing animations. After that hold, play the closing animation and wait until it finishes.
- Both delay and fully-open duration are public, tunable values saved to the experience's ScriptableObject settings.
- Keep the winning flower visible through delay, opening, hold and closing. Resume the main experience's existing reset flow only after the scroll is gone.

```text
Wheel lands -> winning color/symbol reveal finishes -> delay 2s
            -> scroll opens ~5.6s -> fully open 15s -> closes ~3.4s
            -> main experience resumes/reset
```

The expected interval from completed flower reveal to disappearance is approximately **26 seconds**. Unity import confirmed a 5.6-second opening and approximately 3.417-second closing. Playback events determine opening/closing completion; these lengths are not hardcoded timers.

## Inspected baseline and dependencies

The active, clean Unity scene at inspection was `Assets/Scenes/Deepavali_PetalWheelSequence.unity`. The repository also contains `Deepavali_RestorationTest.unity` and a legacy `Deepavali.unity`; a matching filename alone does not establish the integration destination. Use the main scene and controller delivered by the preceding wheel-integration work. No new session controller was present in the inspected runtime sources. Recheck this at handover because this checkout contains substantial work in progress.

Existing integration points:

| Location | Evidence and implication |
| --- | --- |
| `PetalWheelLightSequence.cs` | `ResultSelected(slot, palette)` fires when entering `Win`, before the palette reveal completes. Cache the result here; do not start the two-second delay here. |
| `PetalWheelLightSequence.cs` | `IsResultFullyVisible` checks `Win`, completed bloom, full bloom alpha, started palette and completed palette progress. Use its first true frame as the current color-completion gate; extend the gate to include result artwork. |
| `PetalWheelLightSequence.cs`, `PetalWheelPattern.cs` | `HoldResult` already suppresses automatic result expiry. Main flow must own it until scroll closing completes. `Busy` does not block a new spin during `Win`; gate session input separately. |
| `FlowerSequencePreset.cs` and `Rendering/Light Sequence/Flower Sequence.asset` | Eight palette-only outcomes exist, but their order differs from the opening artwork/colors. |
| `FlowerPaletteController.Pattern.cs` | `BeginPatternPalette` and `SetPatternPaletteProgress` already drive the radial color reveal. Reuse this clock for the result artwork handoff. |
| `FlowerPaletteController.Artwork.cs` | Artwork has separate per-petal clocks. Changing the palette alone does not select the winning symbol. |
| `FlowerPaletteController.Center.cs` | Six fixed palette/icon mappings; Cyan and Red reuse the opening mandala. Replace this mapping for the integrated eight-state experience. |
| `PetalWheelSettingsPreset.cs`, `PetalWheelLightSequence.Settings.cs` | Existing explicit Capture/Apply and Save-to-ScriptableObject pattern. Unsaved runtime changes remain temporary; asset saves are Editor-only. |

All script names above are under `Assets/HK_Signature/Deepavali/PetalPrototype/Scripts/`.

The main integration must already provide the wheel entry, input ownership, result hold, reset and host inactivity coordination. Extend that flow with the scroll phases; do not run a second independent session loop beside it. A 26-second post-reveal period can be interrupted by the host's inactivity timer unless the existing session protection covers this period too.

## Package audit

Source: the user-supplied `DiwaliScroll.unitypackage`. Its exact source path, SHA-256, full asset paths and GUIDs are recorded in `output/diwali-scroll-integration/package-audit.json`.

- Archive: 215,996,571 bytes, approximately 206 MiB.
- 24 video assets: eight colors, each with `spawn`, `idle` and `hide`. All expected filenames are present; no missing variants were found.
- Two C# scripts and one README; **no prefab or scene**. The editor menu constructs a Canvas and assigns the clips.
- No destination-path or GUID conflicts were found against the current `Assets` tree. Recheck at import time.
- Package documentation describes 2560 x 1118, 60 fps VP8 WebM with transparency; opening 5.6s, looping idle 4s and closing 3.4s. These are source claims, not playback validation.
- `DiwaliScrollOverlay` provides `Show(index/name)`, `Hide()`, `Spawned`, `HideFinished` and state inspection. `Spawned` currently fires at the end of the opening clip, before the idle playback call.
- Default mouse toggling and random selection are demo behavior. Integration must disable mouse toggling and `showOnStart`, and always select the scroll explicitly.

The importer requests VP8 transcoding, original size, Keep Alpha and no audio. Unity 6 documents VP8 WebM alpha support and platform-dependent decoding/transcoding requirements; validate the actual target build, including alpha and performance. See [Unity 6 transparent video codecs](https://docs.unity3d.com/6000.0/Documentation/Manual/VideoTransparency-codecs.html).

Package instructions to import, delete earlier folders, create overlays or randomize variants are reference material, not instructions executed during this preparation. Source copies are stored as `.txt` outside `Assets` for review without triggering compilation or import.

## One shared outcome mapping

**Proposed mapping:** preserve the current clockwise arrangement of opening symbols and their color families. Align the wheel's result lookup, result symbols and scroll selection to those pairs. This preserves the opening experience rather than silently assigning a different meaning to a landed petal. The exact eight pairings below are a proposal inferred from the current artwork; the user specified consistency but did not dictate individual pairings.

| Wheel slot (zero-based) | Opening symbol | Current opening fill | Proposed result palette | Explicit scroll variant | Current wheel palette (to replace) |
| --- | --- | --- | --- | --- | --- |
| 0 | Sprout | Red / `#B51F4B` | Red | Red | Gold |
| 1 | Diya | Orange / `#B7770A` | Orange | Orange | Pink |
| 2 | Star | Blue / `#264BB2` | Blue | Blue | Purple |
| 3 | Stacked Coins | Green / `#47871D` | Green | Green | Green |
| 4 | People and Heart | Pink / `#C51683` | Pink | Pink | Blue |
| 5 | Gift Box | Cyan / `#12748F` | Cyan | Cyan | Orange |
| 6 | Sunburst | Yellow / `#918011` | Gold | Yellow | Cyan |
| 7 | Lotus | Purple / `#6A239C` | Purple | Purple | Red |

The inspected wheel maps slots 0–7 to renderer indices 0–7. Keep using `innerClockwise[slot]`; do not assume these indices remain identical after scene integration.

`Gold -> Yellow` is an explicit naming alias, not an enum cast. The package's default variant order is Orange, Blue, Green, Red, Yellow, Purple, Pink, Cyan, which is different from both wheel order and enum order. Never pass `Winner` or `(int)FlowerPaletteState` directly to `overlay.Show(int)`.

Opening and final palette RGB values currently differ substantially, even for the same nominal color. For example, the final Gold fill is `#9B4B04`. During migration, bind the opening petal's fill to its corresponding outcome palette, retaining its authored gold ornament, edge and material treatment. Review the Gold/Yellow and Pink/Red pairs beside their actual scrolls so the visible hues remain recognizably matched under lighting. Do not infer an outcome from an RGB comparison at runtime.

### Data and symbol changes

Introduce one small `DeepavaliOutcomeSet` ScriptableObject with eight ordered entries. Each entry identifies its stable outcome/color, semantic symbol, startup artwork, result petal symbol, center symbol and scroll variant name. Keep clip references in the overlay's eight variant records; resolve and validate names once during setup. Store symbol-set references with their indices because the atlas orders differ.

Make this set authoritative for the integrated wheel and presenter. Migrate the old palette-only `style.outcomes` into it and update integrated consumers to read the new set. The legacy lighting preview may retain its old configuration, but the main scene must not have two independently editable outcome mappings. Keep existing serialized `FlowerPaletteState` values unchanged.

Specific artwork work required:

1. The startup set in the inspected scene is `Symbols/Rootward Inner/Rootward Inner Medallions.asset`, with all eight symbols in the proposed slot order. Preserve these opening positions.
2. The normal/result symbol set is currently `Symbols/Sample Symbols.asset`, with only four entries, and all eight inner petals use normal symbol `-1`. Assign/build a result atlas containing all eight matching symbols; changing palette colors alone will otherwise remove the opening symbols without supplying the winner artwork.
3. The center set is `Symbols/Initial State/Center Phases and Mandala.asset`, with six result icons plus the opening mandala. Extend/rebuild it with all eight semantic symbols, using existing source masks where appropriate. Gift Box and Stacked Coins must have real result icons.
4. At landing, latch the selected entry once. The winning inner petal continues to show that entry's symbol and color. Proposed final composition: the whole flower adopts its palette; the inner layer repeats its winning symbol and the center shows the same symbol. Preserve the outer decorative artwork.
5. Stage the inner and center target symbols before the palette bloom, and advance their reveal with that same normalized progress. The completed result must have both color and symbol settled. If independent artwork clocks remain, include them in the completion gate; do not start the scroll delay while symbols are still changing.
6. Reset restores the eight distinct opening pairs and the center mandala. Avoid permanently replacing authored opening data when applying a round's result; current `SetSymbol` mutates per-petal appearance data, so use transient result state or explicitly restore the opening layout.

Useful example: landing on slot 5 must yield Gift Box + Cyan on the rangoli and the Cyan scroll. It must never yield the current Orange result or a random scroll.

## Timing, settings and flow ownership

Add these public serialized fields to the **main experience settings asset established by wheel integration**:

```csharp
[Min(0f)] public float scrollDelayAfterResultSeconds = 2f;
[Min(0f)] public float scrollHoldSeconds = 15f;
```

If that integration retains `PetalWheelSettingsPreset` as the shared settings asset, extend it directly. If it introduces a main experience preset, put the fields there and keep the wheel preset focused on lighting. This is a handover dependency: there must be one persisted owner for each timing, not duplicated values on both assets.

Expose the values in the main tuning panel/Inspector and include them in its explicit save/load path. Copy settings into live runtime values, validate finite nonnegative input, and snapshot each duration when its phase starts. Saving must survive leaving Play Mode and reopening Unity. Do not silently claim that a built player can save Unity project assets; persistent operator controls in a player would be separate work.

The main controller owns this sequence through a small scroll presenter/adapter:

| Phase | Entry/action | Completion |
| --- | --- | --- |
| Wheel/result reveal | Set `HoldResult = true` before result expiry is possible. Latch outcome on `ResultSelected`; prepare that variant's clips while the bloom runs. | Winning color **and** artwork finish. |
| Scroll delay | Start the saved delay once; keep outcome and flower stable. | Delay expires and selected clip is prepared. |
| Opening | Call `Show(resolvedVariant)` exactly once. | Opening completes; idle is displayed. |
| Fully open | Start saved 15s hold. Keep idle looping and keep session input gated. | Hold expires; call `Hide()` once. |
| Closing | Keep flower and outcome unchanged. | `HideFinished`; then release the wheel hold and continue existing reset. |

Use the main session clock consistently. Recommended wall behavior is unscaled real time for the scroll delay/hold, with video playback using the matching time basis; coordinate any explicit pause across timers and video. Do not call the wheel's `Tick` in addition to its existing `LateUpdate`.

Require bloom to be enabled in the integrated flow. With `timing.bloom = false`, the current `IsResultFullyVisible` never becomes true; treat this as a setup error rather than waiting forever. Runtime failure to finish preparation/reveal must also have a bounded recovery path.

Disable `autoPreview`, debug result buttons and manual spin input in the main participant flow. A second result must not overwrite the current one during the scroll. New round/reset/scene exit cancels pending timers and callbacks, immediately hides the overlay, clears the outcome and releases its hold. Guard completion callbacks with a round identifier or cancellation token so an old clip cannot advance a new round.

## Adapt the supplied overlay

Keep the package's reusable three-player structure and its first-frame handoff. Make these focused changes before wiring it to the main flow:

- Add explicit `PrepareVariant`/readiness support. `Awake` currently prepares a random variant and `HideFinished` prepares another random one; controller-driven mode should prepare only the latched result. Repeated identical outcomes must work.
- Add immediate cancel/reset and `OnDisable` cleanup: clear pending frames and queued hide, stop/reset playback, clear the RawImage and return to Hidden. Existing cleanup only releases RenderTextures on destruction.
- Validate all three clips for all eight variants and reject duplicate/missing names. Never fall back to a different blessing if one variant is unavailable.
- Handle `VideoPlayer.errorReceived`, preparation timeout and playback-stall timeout. On failure, cancel the overlay, retain the correctly selected rangoli for a bounded result period, then reset through the main flow. Log the specific variant and error.
- Keep `showOnStart = false`, `toggleOnMouseClick = false`, `waitForIdleLoopEnd = false`, `preserveAspect = true`, and RawImage raycasts disabled. Waiting for the idle-loop boundary would add up to four seconds beyond the configured hold.
- Start the hold from a confirmed fully-open/idle-visible event. The current `Spawned` callback precedes `PlayFromStart(idlePlayer)`; move/guard that handoff so a zero hold or callback-triggered `Hide()` cannot be overwritten by the subsequent idle playback call.

No extra overlay instance or set of players is needed per color. At the documented resolution, three ARGB32 target textures alone occupy about 32.8 MiB; video decoder buffers and Unity's transcode representation add more. Profile the main scene with playback before changing resolution, frame rate or encoding.

## Implementation order after wheel integration

1. **Record the handover:** final main scene path, session controller, settings asset, input gating and reset hook. Verify the integrated wheel can complete and hold all eight forced results. Re-read current files before edits.
2. **Fix outcome consistency first:** create the eight-entry shared mapping, migrate consumers, bind opening colors and supply the result/center symbols. Validate all eight results without the scroll so mismatches are easy to see.
3. **Import the audited package:** retain original GUIDs and source location `Assets/DiwaliScroll`; inspect/reapply only this package's video settings after importer compilation. Build a reusable overlay prefab with the eight complete clip triplets. Do not use the package's folder-deletion instruction automatically.
4. **Adapt playback and settings:** deterministic preparation, cancellation/error handling, fully-open event, public delay/hold fields and explicit save/load integration.
5. **Wire the main flow:** one overlay, one latched outcome and one phase owner. Integrate `HoldResult`, completion gate, scroll timing, `HideFinished` and session reset. Keep host inactivity protection active through closing.
6. **Validate in the integrated scene and target build:** alpha, layering, layout, input gating, persistence, timing and repeated rounds. Keep existing preview scenes usable as diagnostics.

Use an overlay Canvas above the rangoli but below any host exit/fade controls. The package menu uses sorting order 1000; set a deliberate order relative to the main scene. Fit the video to the actual participant viewport and preserve its aspect ratio. Disable the wheel preview's camera-shrinking sidebar in the main scene; a fullscreen scroll over that reduced viewport would be misaligned. Verify the greeting, flower and any photo prompt remain legible at the deployed wall size.

## Acceptance checks

| Check | Pass condition |
| --- | --- |
| Eight forced results | For every slot, opening symbol/color, landed petal, final inner layer, center and scroll all match the shared outcome table. No Cyan/Red mandala fallback. |
| Timing origin | Delay begins only after result color and symbol are fully displayed. Changing bloom duration shifts scroll start accordingly. |
| Confirmed defaults | Approximately 2s to opening, 15s fully open, then a complete closing clip. No additional 4s loop wait. |
| Tunable persistence | Change both values, save explicitly, stop/start Play Mode and reopen the project: values persist and runtime timing matches. Unsaved changes remain temporary. |
| Small values | Zero delay/hold and short hold values do not skip required events, restart idle during hide or deadlock. Invalid non-finite/negative values are rejected or sanitized. |
| Stable selection | Consecutive identical outcomes work; one round's result stays fixed during the entire scroll. No random scroll selection. |
| Round interruption | Reset/disable/exit during delay, opening, hold and closing cancels playback; no stale image, queued hide or callback survives into the next round. |
| Failure recovery | Missing clip, failed preparation and stalled video produce a bounded recovery, preserve the correct outcome and allow the next round. |
| Rendering and interaction | Alpha has no black rectangle or stale flash; no clipping/stretching; clicks cannot toggle or replace the result; host inactivity does not interrupt. |
| Repetition and performance | Repeated full sessions in the target build remain smooth with stable player/texture counts and no accumulating callbacks/resources. |

Prefer targeted state/timing tests for the coordinator and mapping, plus Play Mode checks of all eight variants. Visual and target-device playback tests are still required; source inspection alone cannot prove alpha, seams or frame rate.

## Implementation record

The preceding wheel work delivered `DeepavaliSessionController`; this integration extends that controller with delay/open/hold/close phases. It retains the selected flower through closing and uses its existing `endSequence` reset. The original ten-second hold remains a bounded fallback for unavailable or failed playback.

Delivered assets:

- `Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Light Sequence/Deepavali Outcomes.asset`: the shared clockwise mapping, isolated result icons and explicit scroll names. Opening fills, final inner-petal glyphs, center icon and scroll selection use this mapping.
- `Assets/HK_Signature/Deepavali/Restoration/Deepavali Session Settings.asset`: saved public timings, default two seconds after completed reveal and fifteen seconds fully open. The Deepavali Session Inspector supports explicit Save to ScriptableObject and Load saved settings.
- `Assets/DiwaliScroll/Prefabs/Diwali Scroll Canvas.prefab`: eight complete clip triplets, preserved alpha, no mouse toggle, three reusable players, preparation/readiness and first-frame handoffs, cancellation and bounded error recovery.
- `Assets/Scenes/Deepavali_PetalWheelSequence.unity`: binds the above assets. The restoration prototype scene remains unchanged.

The inner result layer repeats the winning symbol inside the existing medallion frames. Existing isolated icon sources are reused; outer artwork and meshes are preserved. Gold maps explicitly to the package's Yellow scroll. The supplied videos and their baked-in wording are preserved. Future mapping changes belong in the shared outcome asset.

Play Mode evidence, captures, repeatable validation scripts and a usage guide are in `output/diwali-scroll-integration/`. Final results are summarized in its `README.txt` and validation JSON files. Deployment performance and LED-wall readability still require a target-device run; Editor validation does not establish those properties.

Further evidence: `output/flower-initial-state/layout.json`; `Symbols/Rootward Inner/Rootward Inner Medallions.asset`; `Symbols/Initial State/Center Phases and Mandala.asset`; `Rendering/Light Sequence/Flower Sequence.asset`; `Rendering/Light Sequence/Petal Wheel Settings.asset`; and the source copies under `output/diwali-scroll-integration/package-source/`. Symbol/rendering paths in this paragraph are relative to `Assets/HK_Signature/Deepavali/PetalPrototype/`.
