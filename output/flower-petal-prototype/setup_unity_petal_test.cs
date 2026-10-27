// Executed once through Unity MCP eval_file. Builds assets in the active Unity project.
var assetRoot = "Assets/HK_Signature/Deepavali/PetalPrototype";
var scenePath = "Assets/Scenes/Deepavali_PetalTest.unity";
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Exit Play Mode before scene assembly.");
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
        throw new System.InvalidOperationException("Open scene has unsaved edits; preserve them before scene assembly.");
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(scenePath) != null)
    throw new System.InvalidOperationException("Petal test scene already exists; refusing to overwrite it.");

foreach (var folder in new[] { "Materials", "Prefabs" })
    if (!UnityEditor.AssetDatabase.IsValidFolder(assetRoot + "/" + folder))
        UnityEditor.AssetDatabase.CreateFolder(assetRoot, folder);

UnityEngine.Material MakeMaterial(string name, string html, float smoothness)
{
    var material = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
    material.name = name;
    UnityEngine.ColorUtility.TryParseHtmlString(html, out var color);
    material.SetColor("_BaseColor", color.linear);
    material.SetFloat("_Smoothness", smoothness);
    material.SetFloat("_Metallic", 0f);
    UnityEditor.AssetDatabase.CreateAsset(material, assetRoot + "/Materials/" + name + ".mat");
    return material;
}

var petalMaterial = MakeMaterial("Petal Berry", "#B71353", 0.25f);
var discMaterial = MakeMaterial("Center Purple", "#2D163F", 0.25f);
var groundMaterial = MakeMaterial("Test Ground", "#272B3B", 0.1f);
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
scene.name = "Deepavali_PetalTest";

var wrapper = new UnityEngine.GameObject("Deepavali Single Petal");
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(assetRoot + "/Models/Deepavali_SinglePetal.fbx");
var model = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source, scene);
model.transform.SetParent(wrapper.transform, false);
foreach (var animator in model.GetComponentsInChildren<UnityEngine.Animator>(true))
    UnityEngine.Object.DestroyImmediate(animator);
var all = model.GetComponentsInChildren<UnityEngine.Transform>(true);
UnityEngine.Transform Bone(string name) => System.Linq.Enumerable.Single(all, t => t.name == name);
var skin = model.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();
skin.sharedMaterial = petalMaterial;
skin.quality = UnityEngine.SkinQuality.Bone2;
skin.updateWhenOffscreen = false;
skin.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
skin.receiveShadows = true;
// Root-centred conservative sphere-box covers the 0.263 m spine throughout its motion.
// Baked imported bounds cover only the neutral pose and would clip procedural animation.
skin.localBounds = new UnityEngine.Bounds(UnityEngine.Vector3.zero, UnityEngine.Vector3.one * 0.62f);
var disc = Bone("CenterDisc").GetComponent<UnityEngine.MeshRenderer>();
disc.sharedMaterial = discMaterial;

var motion = wrapper.AddComponent<Amgen.Deepavali.PetalMotionController>();
var motionData = new UnityEditor.SerializedObject(motion);
motionData.FindProperty("baseBone").objectReferenceValue = Bone("Petal_01_Base");
motionData.FindProperty("middleBone").objectReferenceValue = Bone("Petal_01_Mid");
motionData.FindProperty("tipBone").objectReferenceValue = Bone("Petal_01_Tip");
motionData.ApplyModifiedPropertiesWithoutUndo();
var prefabPath = assetRoot + "/Prefabs/Deepavali Single Petal.prefab";
UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(wrapper, prefabPath, UnityEditor.InteractionMode.AutomatedAction);

var inputObject = new UnityEngine.GameObject("Petal Test Input - Tap Click Space");
var input = inputObject.AddComponent<Amgen.Deepavali.PetalPulseInput>();
var inputData = new UnityEditor.SerializedObject(input);
inputData.FindProperty("petal").objectReferenceValue = motion;
inputData.ApplyModifiedPropertiesWithoutUndo();

var cameraObject = new UnityEngine.GameObject("Main Camera", typeof(UnityEngine.Camera), typeof(UnityEngine.AudioListener));
cameraObject.tag = "MainCamera";
var camera = cameraObject.GetComponent<UnityEngine.Camera>();
camera.transform.position = new UnityEngine.Vector3(0f, 0.87f, -0.112f);
camera.transform.rotation = UnityEngine.Quaternion.LookRotation(UnityEngine.Vector3.down, UnityEngine.Vector3.back);
camera.orthographic = false;
camera.fieldOfView = 35f;
camera.nearClipPlane = 0.01f;
camera.farClipPlane = 2f;
camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
camera.backgroundColor = new UnityEngine.Color(0.025f, 0.03f, 0.045f);
camera.allowHDR = false;
var cameraData = cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
cameraData.renderPostProcessing = false;

var keyObject = new UnityEngine.GameObject("Main Light");
var key = keyObject.AddComponent<UnityEngine.Light>();
key.type = UnityEngine.LightType.Directional;
key.transform.rotation = UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(-0.45f, -1f, 0.25f));
key.color = new UnityEngine.Color(1f, 0.96f, 0.92f);
key.intensity = 1.05f;
key.shadows = UnityEngine.LightShadows.Hard;
key.shadowStrength = 0.65f;
key.shadowBias = 0.01f;
key.shadowNormalBias = 0.02f;
keyObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
UnityEngine.RenderSettings.sun = key;
UnityEngine.RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
UnityEngine.RenderSettings.ambientLight = new UnityEngine.Color(0.3f, 0.32f, 0.4f);
UnityEngine.RenderSettings.skybox = null;
UnityEngine.RenderSettings.fog = false;

var fillObject = new UnityEngine.GameObject("Overhead Fill - No Shadows");
var fill = fillObject.AddComponent<UnityEngine.Light>();
fill.type = UnityEngine.LightType.Point;
fill.transform.position = new UnityEngine.Vector3(-0.2f, 0.3f, -0.15f);
fill.range = 1.1f;
fill.intensity = 0.35f;
fill.color = new UnityEngine.Color(0.8f, 0.85f, 1f);
fill.shadows = UnityEngine.LightShadows.None;
fillObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();

var ground = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Plane);
ground.name = "Ground";
ground.transform.position = new UnityEngine.Vector3(0f, -0.008f, -0.1f);
ground.transform.localScale = UnityEngine.Vector3.one * 0.15f;
ground.GetComponent<UnityEngine.Renderer>().sharedMaterial = groundMaterial;
UnityEngine.Object.DestroyImmediate(ground.GetComponent<UnityEngine.Collider>());

var canvasObject = new UnityEngine.GameObject("Test Instructions", typeof(UnityEngine.Canvas), typeof(UnityEngine.UI.CanvasScaler));
var canvas = canvasObject.GetComponent<UnityEngine.Canvas>();
canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new UnityEngine.Vector2(1280, 800);
scaler.matchWidthOrHeight = 1f;
var textObject = new UnityEngine.GameObject("Instructions", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Text));
textObject.transform.SetParent(canvasObject.transform, false);
var label = textObject.GetComponent<UnityEngine.UI.Text>();
label.font = UnityEngine.Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");
label.text = "SINGLE PETAL TEST\nTap / click anywhere or press Space to pulse";
label.fontSize = 20;
label.lineSpacing = 1.2f;
label.alignment = UnityEngine.TextAnchor.MiddleCenter;
label.color = new UnityEngine.Color(0.9f, 0.9f, 0.95f);
label.raycastTarget = false;
var rect = textObject.GetComponent<UnityEngine.RectTransform>();
rect.anchorMin = new UnityEngine.Vector2(0f, 0f);
rect.anchorMax = new UnityEngine.Vector2(1f, 0f);
rect.pivot = new UnityEngine.Vector2(0.5f, 0f);
rect.anchoredPosition = new UnityEngine.Vector2(0f, 12f);
rect.sizeDelta = new UnityEngine.Vector2(0f, 64f);

UnityEditor.Selection.activeGameObject = wrapper;
if (UnityEditor.SceneView.lastActiveSceneView != null)
    UnityEditor.SceneView.lastActiveSceneView.LookAt(new UnityEngine.Vector3(0f, 0f, -0.112f), camera.transform.rotation, 0.38f);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
UnityEditor.AssetDatabase.SaveAssets();
return new { scene = scenePath, prefab = prefabPath, bones = skin.bones.Length,
    petalVertices = skin.sharedMesh.vertexCount, camera = "Fixed overhead perspective", trigger = "Tap / click / Space" };
