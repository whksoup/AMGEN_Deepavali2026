// Unity MCP eval_file helper: update the existing textured test and its reusable prefab.
var rootPath = "Assets/HK_Signature/Deepavali/PetalPrototype";
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEditor.EditorApplication.isPlaying || scene.isDirty || scene.path != "Assets/Scenes/Deepavali_TexturedPetalTest.unity")
    throw new System.InvalidOperationException("Open the saved textured petal test outside Play Mode first.");
var prefabPath = rootPath + "/Prefabs/Deepavali Textured Inner Layer.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var settings = new UnityEditor.SerializedObject(prefab.GetComponent<Amgen.Deepavali.FlowerEmissionController>());
    settings.FindProperty("flashColor").colorValue = new UnityEngine.Color(1f, 0.18f, 0.48f, 1f);
    settings.FindProperty("flashAttack").floatValue = 0.05f;
    settings.FindProperty("flashHold").floatValue = 0.12f;
    settings.FindProperty("flashFade").floatValue = 0.55f;
    settings.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(prefab); }
var driver = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerEmissionController>();
var input = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalLayerPointerInput>();
var inputSettings = new UnityEditor.SerializedObject(input);
inputSettings.FindProperty("flowerEmission").objectReferenceValue = driver;
inputSettings.ApplyModifiedPropertiesWithoutUndo();

var folder = rootPath + "/Rendering";
if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder(rootPath, "Rendering");
var profilePath = folder + "/Petal Subtle Bloom.asset";
var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath);
if (profile == null)
{
    profile = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
    profile.name = "Petal Subtle Bloom";
    UnityEditor.AssetDatabase.CreateAsset(profile, profilePath);
}
if (!profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))
{
    bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
    UnityEditor.AssetDatabase.AddObjectToAsset(bloom, profile);
}
bloom.active = true;
bloom.threshold.Override(1.3f);
bloom.intensity.Override(0.16f);
bloom.scatter.Override(0.35f);
bloom.clamp.Override(8f);
bloom.tint.Override(UnityEngine.Color.white);
bloom.downscale.Override(UnityEngine.Rendering.Universal.BloomDownscaleMode.Quarter);
bloom.maxIterations.Override(3);
bloom.highQualityFiltering.Override(false);
bloom.dirtTexture.Override(null);
bloom.dirtIntensity.Override(0f);
UnityEditor.EditorUtility.SetDirty(bloom);
// Enabling post-processing would otherwise inherit Neutral tonemapping from the
// project's default volume and change the existing artwork's appearance.
if (!profile.TryGet<UnityEngine.Rendering.Universal.Tonemapping>(out var tone))
{
    tone = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
    UnityEditor.AssetDatabase.AddObjectToAsset(tone, profile);
}
tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.None);
UnityEditor.EditorUtility.SetDirty(tone);
UnityEditor.EditorUtility.SetDirty(profile);
var volumeObject = UnityEngine.GameObject.Find("Petal Bloom - Mobile");
if (volumeObject == null) volumeObject = new UnityEngine.GameObject("Petal Bloom - Mobile");
var volume = volumeObject.GetComponent<UnityEngine.Rendering.Volume>();
if (volume == null) volume = volumeObject.AddComponent<UnityEngine.Rendering.Volume>();
volume.isGlobal = true;
volume.priority = 10f;
volume.weight = 1f;
volume.sharedProfile = profile;
var camera = UnityEngine.Camera.main;
camera.allowHDR = true;
var cameraData = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
cameraData.renderPostProcessing = true;
cameraData.volumeLayerMask = cameraData.volumeLayerMask.value | (1 << volumeObject.layer);
UnityEngine.GameObject.Find("Test Instructions").GetComponentInChildren<UnityEngine.UI.Text>().text = "DEEPAVALI PETAL TEST\nTap a petal to pulse it and light the flower";
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.Selection.activeGameObject = driver.gameObject;
return new { scene = scene.path, profile = profilePath, hdr = camera.allowHDR,
    postProcessing = cameraData.renderPostProcessing, intensity = bloom.intensity.value,
    threshold = bloom.threshold.value, downscale = bloom.downscale.value.ToString(),
    maxIterations = bloom.maxIterations.value, highQualityFiltering = bloom.highQualityFiltering.value,
    flashSeconds = 0.72f, inputConnected = inputSettings.FindProperty("flowerEmission").objectReferenceValue == driver };
