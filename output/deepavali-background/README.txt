Deepavali background - 28 October 2026

Scene: Assets/Scenes/Deepavali_PaletteTest.unity
Reusable prefab: Assets/HK_Signature/Deepavali/Background/Prefabs/Deepavali Earthen Background.prefab

The background uses the supplied Yughues SoilGravel04 albedo and normal textures,
with a dark earth tint, matte relief, darker margins and warm pools around the lamps.
Four nested instances use the supplied cgtrader_optimized_Oil+Lamp.fbx mesh.
Its referenced texture images were absent, so dedicated URP clay, brass, wick and
oil materials supply the appearance. Procedural camera-facing flames flicker gently.
Shared cupped flower/petal meshes supply orange and burgundy border dressing.

Select Deepavali Earthen Background to adjust the corner insets or flower clearance.
DeepavaliBackground projects the decorations onto the floor when camera framing changes.
Its background geometry has no colliders, preserving the flower's click/touch targets.
The original Ground object is retained, inactive. Existing flower assets are unchanged.

Review: after-1600x900.png and after-2560x1118.png (Play Mode, initial flower appearance).
Validation: validation.json checks both aspect ratios, the 4 original FBX mesh references,
all 8 inner-petal picks, center clearance, finite mesh data and shader errors.
before-scene.unity is a snapshot including the scene's previously unsaved edits.

Rebuild in Edit Mode through Unity MCP eval_file: build-background.cs
Capture in Play Mode: capture-background.cs
Validate in Play Mode: validate-background.cs
Rebuild replaces only the generated background root/assets and disables Ground.
