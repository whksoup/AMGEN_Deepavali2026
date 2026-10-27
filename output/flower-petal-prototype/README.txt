DEEPAVALI SINGLE INNER PETAL PROTOTYPE

Open Deepavali_SinglePetal.blend. The active Deepavali_PetalPrototype scene
contains one inner petal, a separate static center disc, and a three-bone rig.
The original Blender scene is preserved separately.

SOURCE
02-inner-petals.svg / inner-petal-01: the upward-pointing berry petal.
03-center-disc.svg / center-disc: the purple circular flower head.
Exact editable SVG Bezier references are in 02_SVG_References_Hidden.
The petal's surface uses a lengthwise quad grid sampled from the SVG outline,
with 3 mm of subtle camber and no subdivision modifier.

ASSET BUDGET
Petal: 191 vertices, 324 triangles, 3 deform bones.
Disc: 258 vertices, 512 triangles, closed beveled solid.
Total: 836 triangles before any Unity import vertex splitting.
No image textures, animation clips, physics, cloth, or runtime subdivision.
Each petal vertex has at most 2 normalized bone influences.

RIG
FlowerRoot
  CenterDisc
  Petal_01_Rig
    InnerPetal_01 (Armature modifier)
    Petal_01_Base -> Petal_01_Mid -> Petal_01_Tip (connected bone chain)

The disc remains static. Petal vertex 0 coincides with the base bone head.
Rotating the bones keeps that point fixed. Bone translation and scale are
locked in Blender. Twist is locked; bend and sideways sway remain available.
Blender local bone X bends up/down, local Z sways sideways. Check the imported
bone bases before mapping these axes in Unity, because FBX converts axes.
Always apply procedural rotations relative to the imported bind/local rotation.
The root object and bone translations must remain unchanged during animation.

No clips, drivers, or custom Blender rig constraints are required in Unity.
The saved blend and FBX are in the neutral pose. Bend preview is a temporary
validation pose only; it is not included as an animation.
The downward test verifies skinning and anchoring, not ground collision.
Runtime angular limits and final layer/ground clearance remain Unity work.

SCALE AND COORDINATES
SVG nominal mm converted to metres. Flower center is (0,0,0).
Blender flower plane is XY, facing +Z. Petal spine runs along +Y.
Petal root: (0, 0.082, 0.004) metres; length: 0.263 m.
Center disc diameter: approximately 0.242 m; top: Z 0.018 m.
Uniformly scale FlowerRoot if a different installation scale is needed.
ArtworkUV: planar texture coordinates.
PetalCoord_Width_Length: U across width, V root=0 to tip=1.

UNITY HANDOFF
Deepavali_SinglePetal.fbx contains only FlowerRoot, the two asset meshes,
and the three-bone rig. No camera, lights, ground, reference curves or leaf bones.
Export: -Z forward, Y up, centimetre/unit metadata handled by FBX exporter,
no animation baking. Do not add an Animator merely to drive these bones.
Import as a Generic rig if an Animator workflow is subsequently needed.
Keep all three bones accessible; do not optimize away bones needed by scripts.
Use a URP lit material. Petal is a single surface; configure two-sided rendering
and backface normals if its underside needs to be visible. The current top
camera only requires the upper face. Shadow casting is a separate setting.
Material colours originate from the SVG. Recreate/tune the URP materials in Unity;
Blender node graphs, preview lighting and render appearance are not portable.
Set skinned-renderer bounds to cover procedural movement during integration.
Target tablet hardware and frame budget have not been profiled.

PREVIEW
03_Preview_Only contains a fixed overhead perspective camera, ground and two
studio lights. The 900 x 1000 preview resolution is unrelated to the eventual
Android application output resolution. These objects are excluded from FBX.

REBUILD / VALIDATION
build_petal_prototype.py builds a new scene through bpy; refuses name collisions.
validate_and_export.py checks topology, normalized weights and fixed attachment
under lift, downward bend, sway and combined motion, then renders and exports.
validation.json records the results. Both scripts were run through Blender MCP.

UNITY SINGLE-PETAL TEST (IMPLEMENTED)
Open Assets/Scenes/Deepavali_PetalTest.unity and enter Play Mode.
Tap/click in the Game view or press Space to trigger a pulse.
The petal idles automatically. Select Deepavali Single Petal to tune the
PetalMotionController component: lift bias, lift/sway amplitude per bone,
idle frequency/phase, pulse angles, and pulse attack/hold/release timing.

Reusable prefab:
Assets/HK_Signature/Deepavali/PetalPrototype/Prefabs/Deepavali Single Petal.prefab
Runtime scripts:
Assets/HK_Signature/Deepavali/PetalPrototype/Scripts/PetalMotionController.cs
Assets/HK_Signature/Deepavali/PetalPrototype/Scripts/PetalPulseInput.cs

PetalMotionController.TriggerPulse() is the integration point for future
gameplay or proximity events. PetalPulseInput belongs only to the test scene;
the prefab itself has no mouse, keyboard, or touch dependency.
Motion only replaces bone rotations relative to the imported rest rotations.
Bone positions/scales and the center disc stay unchanged. Disabling the
controller restores the neutral pose; enabling it fades idle motion back in.
Retriggering extends the pulse without resetting the current rotation or
adding unbounded displacement. All three bones remain accessible to scripts.
The skinned renderer has conservative bounds and two-weight skinning.

The test scene uses a perspective camera above the flower, a ground receiver,
one shadow-casting main light and one overhead fill with shadows disabled.
No flower duplication or changes to the main Deepavali scene/build list.

validate_unity_petal.cs is a temporary Editor Play Mode integration check,
executed through MCP. It samples actual baked skinned vertices, tests keyboard,
mouse and synthetic touchscreen input, repeated pulses, renderer bounds,
disable/re-enable recovery, and the project's mobile URP asset. It removes
its input devices/callbacks and restores the previous pipeline afterwards.
Results are written to unity-validation.json. This is Editor validation on
the desktop, not an Android device benchmark or APK delivery.
