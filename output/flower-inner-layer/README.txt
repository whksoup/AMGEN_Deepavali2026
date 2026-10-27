Deepavali eight-petal inner layer

Open Assets/Scenes/Deepavali_InnerLayerTest.unity and enter Play Mode.
Click or tap the visible body of a petal to pulse it. Multiple simultaneous
touches can pulse different petals. The center disc, gaps and background ignore
presses. The earlier Deepavali_PetalTest scene remains available.

Assets
- Prefabs/Deepavali Inner Layer.prefab: eight nested petal prefabs and one disc.
- Prefabs/Deepavali Inner Petal.prefab: one three-bone petal without the disc.
  Both are under Assets/HK_Signature/Deepavali/PetalPrototype/.
- Eight petals share the imported FBX mesh and material. Each has its own bones
  and PetalMotionController pulse state. 3,104 triangles, 24 bones in the flower.
- Spacing: 45 degrees around the center; idle phase offsets: 0, 0.35, 0.70,
  1.05, 1.40, 1.75, 2.10, 2.45 radians. Edit each controller's Phase Offset
  to adjust timing; edit the inner-petal prefab to adjust shared motion defaults.

Input and motion
- PetalLayerPointerInput is on the scene's Inner Layer Input object. Its camera,
  eight PetalPickTargets and center-disc occluder are assigned in the Inspector.
  When integrating the layer prefab into another scene, add this input component
  and assign those scene references. Do not also use the old PetalPulseInput.
- PetalPickTarget bakes a small mesh snapshot only on pointer presses and tests
  the actual deformed triangles. It chooses the closest visible petal and respects
  the center disc. No per-frame collision mesh updates or physics bodies.
- Input supports the project's Input System (including multiple touches), with
  a legacy-input fallback when that package backend is disabled.
- PetalMotionController.TriggerPulse() remains available for scripted events.
  Motion uses local bone rotations and restores the rest pose when disabled;
  it never translates/scales the attachment or accumulates rotations.
- Fixed overhead perspective camera. Existing lit materials and lighting reused.

Validation
unity-validation.json records the Play Mode integration result: eight isolated
mouse picks, two simultaneous touches, 19 ignored disc/gap/background positions,
deformed-surface re-picking, independent idle phases, fixed skin roots, fixed
center disc, bone transforms and conservative renderer bounds. No runtime errors.
The tests used Mobile_RPAsset in the desktop Editor, then restored PC_RPAsset.
Android hardware performance and an APK build have not been measured.

setup_unity_inner_layer.cs and validate_unity_inner_layer.cs are Unity MCP
eval_file helpers, deliberately outside Assets. The setup refuses to overwrite
the created assets. The validator removes its temporary devices and callbacks.
