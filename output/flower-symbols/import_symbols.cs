if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Build symbol assets in Edit Mode.");
const string root = "Assets/HK_Signature/Deepavali/PetalPrototype/Symbols/Initial State";
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var labels = new[] { "Lotus", "Sunburst", "Star", "Sprout", "Diya", "People and Heart", "Gift Box", "Stacked Coins", "Center Mandala" };
for (int i = 0; i < labels.Length; i++)
{
    string path = root + $"/Icons/{i:00}-{labels[i]}.png";
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Default;
    importer.sRGBTexture = false;
    importer.alphaSource = UnityEditor.TextureImporterAlphaSource.None;
    importer.alphaIsTransparency = false;
    importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
    importer.mipmapEnabled = true;
    importer.isReadable = false;
    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    importer.filterMode = UnityEngine.FilterMode.Trilinear;
    importer.maxTextureSize = 512;
    importer.SetPlatformTextureSettings(new UnityEditor.TextureImporterPlatformSettings { name = "Standalone", overridden = true, maxTextureSize = 512, format = UnityEditor.TextureImporterFormat.BC4, compressionQuality = 100 });
    importer.SetPlatformTextureSettings(new UnityEditor.TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 512, format = UnityEditor.TextureImporterFormat.ETC2_RGB4, compressionQuality = 100 });
    importer.SaveAndReimport();
}
string sourcePath = root + "/Initial Petal Symbol Source.asset";
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.Editor.FlowerSymbolAtlasSource>(sourcePath);
if (source == null)
{
    source = UnityEngine.ScriptableObject.CreateInstance<Amgen.Deepavali.Editor.FlowerSymbolAtlasSource>();
    UnityEditor.AssetDatabase.CreateAsset(source, sourcePath);
}
source.outputName = "Initial Petal Symbols";
source.padding = 16;
source.tiles = new Amgen.Deepavali.Editor.SymbolMaskSource[8];
for (int i = 0; i < 8; i++)
{
    string path = root + $"/Petal Masks/{i:00}-{labels[i]}.png";
    Amgen.Deepavali.Editor.FlowerSymbolAtlasBuilder.ConfigureTexture(path);
    source.tiles[i] = new Amgen.Deepavali.Editor.SymbolMaskSource { label = labels[i], mask = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path) };
}
var petals = Amgen.Deepavali.Editor.FlowerSymbolAtlasBuilder.Build(source);
string centerPath = root + "/Initial Center Mandala.asset";
var center = UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerSymbolSet>(centerPath);
if (center == null)
{
    center = UnityEngine.ScriptableObject.CreateInstance<Amgen.Deepavali.FlowerSymbolSet>();
    UnityEditor.AssetDatabase.CreateAsset(center, centerPath);
}
var centerData = new UnityEditor.SerializedObject(center);
centerData.FindProperty("atlas").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(root + "/Icons/08-Center Mandala.png");
centerData.FindProperty("maxMipLevel").floatValue = 4;
var symbols = centerData.FindProperty("symbols");
symbols.arraySize = 1;
symbols.GetArrayElementAtIndex(0).FindPropertyRelative("label").stringValue = "Initial Center Mandala";
symbols.GetArrayElementAtIndex(0).FindPropertyRelative("uvRect").rectValue = new UnityEngine.Rect(0.5f / 512, 0.5f / 512, 511f / 512, 511f / 512);
centerData.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(center);
UnityEditor.AssetDatabase.SaveAssetIfDirty(source);
UnityEditor.AssetDatabase.SaveAssetIfDirty(center);
var result = new {
    petals = petals.Count, petalAtlas = new { width = petals.Atlas.width, height = petals.Atlas.height, format = petals.Atlas.format.ToString(), safeMip = petals.MaxMipLevel },
    centerIcons = center.Count, centerFormat = center.Atlas.format.ToString(),
    validRects = Enumerable.Range(0, 8).All(i => petals.TryGet(i, out _)) && center.TryGet(0, out _),
    sceneDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty
};
System.IO.File.WriteAllText("output/flower-symbols/import-validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;
