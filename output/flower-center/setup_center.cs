if (UnityEditor.EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
    throw new System.InvalidOperationException("Center setup requires the saved review scene outside Play Mode.");
const string folder = "Assets/HK_Signature/Deepavali/PetalPrototype/Center/";
const string texturePath = folder + "Center Icons.png";
const string setPath = folder + "Center Icons.asset";
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(texturePath);
importer.textureType = UnityEditor.TextureImporterType.Default;
importer.sRGBTexture = false;
importer.alphaSource = UnityEditor.TextureImporterAlphaSource.None;
importer.alphaIsTransparency = false;
// POT import allows block compression; normalized tile rects preserve icon proportions.
importer.npotScale = UnityEditor.TextureImporterNPOTScale.ToLarger;
importer.isReadable = false;
importer.mipmapEnabled = true;
importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
importer.filterMode = UnityEngine.FilterMode.Trilinear;
importer.maxTextureSize = 2048;
importer.SetPlatformTextureSettings(new UnityEditor.TextureImporterPlatformSettings { name = "Standalone", overridden = true, maxTextureSize = 2048, format = UnityEditor.TextureImporterFormat.BC4, compressionQuality = 100 });
importer.SetPlatformTextureSettings(new UnityEditor.TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 2048, format = UnityEditor.TextureImporterFormat.ETC2_RGB4, compressionQuality = 100 });
importer.SaveAndReimport();
var atlas = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(texturePath);
var set = UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerSymbolSet>(setPath);
if (set == null)
{
    set = UnityEngine.ScriptableObject.CreateInstance<Amgen.Deepavali.FlowerSymbolSet>();
    UnityEditor.AssetDatabase.CreateAsset(set, setPath);
}
var data = new UnityEditor.SerializedObject(set);
data.FindProperty("atlas").objectReferenceValue = atlas;
data.FindProperty("maxMipLevel").floatValue = 4;
var entries = data.FindProperty("symbols"); entries.arraySize = 6;
var labels = new[] { "Gold — Lotus", "Pink — Sunburst", "Purple — Star", "Green — Sprout", "Blue — Diya", "Orange — People and heart" };
for (int i = 0; i < 6; i++)
{
    var entry = entries.GetArrayElementAtIndex(i);
    entry.FindPropertyRelative("label").stringValue = labels[i];
    entry.FindPropertyRelative("uvRect").rectValue = new UnityEngine.Rect(
        (i % 3) / 3f + 0.5f / atlas.width, (1 - i / 3) / 2f + 0.5f / atlas.height,
        1f / 3f - 1f / atlas.width, 0.5f - 1f / atlas.height);
}
data.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(set);
foreach (string prefabPath in new[] {
    "Assets/HK_Signature/Deepavali/PetalPrototype/Prefabs/Deepavali Palette Inner Layer.prefab",
    "Assets/HK_Signature/Deepavali/PetalPrototype/Prefabs/Deepavali Two Layer Flower.prefab" })
{
    var contents = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
    try
    {
        var flower = contents.GetComponent<Amgen.Deepavali.FlowerPaletteController>();
        var settings = new UnityEditor.SerializedObject(flower);
        settings.FindProperty("centerIconSet").objectReferenceValue = set;
        settings.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(contents); }
}
UnityEditor.AssetDatabase.SaveAssets();
var controller = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
if (controller.CenterIconSet != set) throw new System.InvalidOperationException("Review instance did not inherit its center icon set.");
return new { width = atlas.width, height = atlas.height, format = atlas.format.ToString(), icons = set.Count, bound = controller.CenterIconSet.name, saved = !controller.gameObject.scene.isDirty };
