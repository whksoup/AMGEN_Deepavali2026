Deepavali petal tap feedback and subtle bloom

Open Assets/Scenes/Deepavali_TexturedPetalTest.unity and press Play. Clicking or
tapping a visible petal pulses that petal's bones and briefly turns ALL petal
edge lights rose pink, then fades them back to the current baseline color.
The center artwork remains lit normally. Disc/gap/background presses do nothing.
Two touches can pulse two petals while sharing one flower-wide color effect.

Tune the flower root's FlowerEmissionController:
- Edge Color / Edge Intensity: baseline appearance (default warm gold, 3).
- Flash Color: rose pink by default.
- Flash Attack: 0.05 seconds; Hold: 0.12 seconds; Fade: 0.55 seconds.
- TriggerFlash() starts feedback; SetEdgeColor() updates the baseline even
  during a flash, so the fade returns to the latest baseline.
- Repeated taps restart from the current blend, without a color jump or
  additive brightness. Intensity zero also switches the flash lights off.
- Uses one shared runtime material. No coroutine, material allocation or
  per-renderer property loop on taps. One material color write per active
  animation frame; no material writes during idle frames.
- Disabling restores the original asset material and clears the flash state.

PetalLayerPointerInput's optional Flower Emission reference connects successful
picks to the color effect. The textured scene has it assigned. Earlier plain
test scenes continue to work with this reference empty.

Bloom profile:
Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Petal Subtle Bloom.asset
- Intensity 0.16; gamma-space threshold 1.3; scatter 0.35; clamp 8.
- Quarter-resolution starting buffer; maximum three levels; bilinear filtering.
- No lens dirt or extra lights. Color comes from the petal emission.
- The test camera enables HDR and post-processing. Existing PC and Mobile URP
  assets already support HDR; neither pipeline asset was modified.
- A local None tonemapping override preserves the previous artwork appearance
  instead of inheriting the project's Neutral tonemapping when post is enabled.
- Android's existing Mobile pipeline remains at its 0.8 render scale. The halo
  is an additional postprocessing cost; no device FPS claim is made. Profile on
  the target tablet before setting a final resolution/performance budget.
- Disable the Petal Bloom - Mobile Volume to remove the halo. Edge emission and
  the color feedback still work; camera post-processing can also be disabled
  if no other effects are wanted.

feedback-validation.json records a Play Mode integration run using Mobile_RPAsset:
synthetic mouse/touch input, isolated bone pulses, uniform color across all eight
petals, fade completion, rapid retriggers, return to a changed baseline, ignored
misses, lifecycle cleanup, stable roots and no runtime errors. Rendered bloom-on
and bloom-off images use identical geometry and tone settings; changed background
pixels verify that a real halo is present. Screenshots are validation artifacts.

setup_unity_feedback.cs and validate_unity_feedback.cs run through Unity MCP
eval_file and are intentionally outside Assets. Temporary input devices, render
targets and callbacks are removed after testing, and the original pipeline is
restored. Android hardware has not been benchmarked or an APK built.
