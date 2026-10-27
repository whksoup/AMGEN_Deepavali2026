// Unity MCP eval_file helper. Adds a textured test while preserving earlier test assets.
var root = "Assets/HK_Signature/Deepavali/PetalPrototype";
var scenePath = "Assets/Scenes/Deepavali_TexturedPetalTest.unity";
var prefabPath = root + "/Prefabs/Deepavali Textured Inner Layer.prefab";
var materialPath = root + "/Materials/Petal Deepavali Lit.mat";
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new System.InvalidOperationException("Preserve unsaved scenes first.");
foreach (var path in new[] { scenePath, prefabPath, materialPath })
    if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) != null) throw new System.InvalidOperationException("Asset already exists: " + path);
UnityEngine.Texture2D ConfigureTexture(string name, bool srgb)
{
    string path = root + "/Textures/" + name + ".png";
    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Default;
    importer.sRGBTexture = srgb;
    importer.alphaSource = UnityEditor.TextureImporterAlphaSource.None;
    importer.isReadable = false;
    importer.mipmapEnabled = true;
    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    importer.filterMode = UnityEngine.FilterMode.Trilinear;
    importer.anisoLevel = 1;
    importer.maxTextureSize = 1024;
    importer.npotScale = UnityEditor.TextureImporterNPOTScale.ToNearest;
    importer.textureCompression = UnityEditor.TextureImporterCompression.CompressedHQ;
    importer.SetPlatformTextureSettings(new UnityEditor.TextureImporterPlatformSettings {
        name = "Android", overridden = true, maxTextureSize = 1024,
        format = UnityEditor.TextureImporterFormat.ETC2_RGB4, compressionQuality = 100
    });
    importer.SaveAndReimport();
    return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
}
var art = ConfigureTexture("Petal_Deepavali_BaseColor", true);
var mask = ConfigureTexture("Petal_EdgeEmission", false);
var material = new UnityEngine.Material(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(root + "/Materials/Petal Berry.mat"));
material.name = "Petal Deepavali Lit";
material.SetTexture("_BaseMap", art);
material.SetColor("_BaseColor", UnityEngine.Color.white);
material.SetTexture("_EmissionMap", mask);
material.SetColor("_EmissionColor", new UnityEngine.Color(1f, 0.72f, 0.28f).linear * 3f);
material.EnableKeyword("_EMISSION");
// URP's material validator requires an emissive flag to retain _EMISSION.
// Petals are non-static; this does not create real-time lighting on other objects.
material.globalIlluminationFlags = UnityEngine.MaterialGlobalIlluminationFlags.BakedEmissive;
material.SetFloat("_Smoothness", 0.22f);
UnityEditor.AssetDatabase.CreateAsset(material, materialPath);
if (!UnityEditor.AssetDatabase.CopyAsset("Assets/Scenes/Deepavali_InnerLayerTest.unity", scenePath)) throw new System.InvalidOperationException("Could not copy scene.");
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
var layer = UnityEngine.GameObject.Find("Deepavali Inner Layer");
UnityEditor.PrefabUtility.UnpackPrefabInstance(layer, UnityEditor.PrefabUnpackMode.OutermostRoot, UnityEditor.InteractionMode.AutomatedAction);
layer.name = "Deepavali Textured Inner Layer";
var petals = layer.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
foreach (var petal in petals)
{
    petal.sharedMaterial = material;
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(petal);
}
var emission = layer.AddComponent<Amgen.Deepavali.FlowerEmissionController>();
var serialized = new UnityEditor.SerializedObject(emission);
serialized.FindProperty("petalMaterial").objectReferenceValue = material;
var renderers = serialized.FindProperty("petals");
renderers.arraySize = petals.Length;
for (int i = 0; i < petals.Length; i++) renderers.GetArrayElementAtIndex(i).objectReferenceValue = petals[i];
serialized.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(layer, prefabPath, UnityEditor.InteractionMode.AutomatedAction);
UnityEngine.GameObject.Find("Test Instructions").GetComponentInChildren<UnityEngine.UI.Text>().text = "DEEPAVALI PETAL TEXTURE TEST\nTap or click a petal to pulse it";
UnityEditor.Selection.activeGameObject = layer;
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.SaveAssets();
return new { scene = scenePath, prefab = prefabPath, material = materialPath,
    artSize = art.width + "x" + art.height, maskSize = mask.width + "x" + mask.height,
    rendererCount = petals.Length, materialCount = petals.Select(p => p.sharedMaterial).Distinct().Count(),
    meshUnchanged = petals.All(p => UnityEditor.AssetDatabase.GetAssetPath(p.sharedMesh).EndsWith("Deepavali_SinglePetal.fbx")) };
