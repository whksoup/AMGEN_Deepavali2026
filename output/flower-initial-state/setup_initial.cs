if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Initial-state setup requires Edit Mode.");
const string root = "Assets/HK_Signature/Deepavali/PetalPrototype";
const string folder = root + "/Symbols/Initial State";
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var labels = new[] { "Sprout", "Diya", "Star", "Stacked Coins", "People and Heart", "Gift Box", "Sunburst", "Lotus" };
var hex = new[] { "#B51F4B", "#B7770A", "#264BB2", "#47871D", "#C51683", "#12748F", "#918011", "#6A239C" };
UnityEngine.Color Color(string value) { UnityEngine.ColorUtility.TryParseHtmlString(value, out var color); return color; }
string sourcePath = folder + "/Decorated Initial Petal Source.asset";
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.Editor.FlowerSymbolAtlasSource>(sourcePath);
if (source == null)
{
    source = UnityEngine.ScriptableObject.CreateInstance<Amgen.Deepavali.Editor.FlowerSymbolAtlasSource>();
    UnityEditor.AssetDatabase.CreateAsset(source, sourcePath);
}
source.outputName = "Decorated Initial Petals"; source.padding = 16;
source.tiles = new Amgen.Deepavali.Editor.SymbolMaskSource[8];
for (int i = 0; i < 8; i++)
{
    string path = folder + $"/Decorated Masks/{i:00}-{labels[i]}.png";
    Amgen.Deepavali.Editor.FlowerSymbolAtlasBuilder.ConfigureTexture(path);
    source.tiles[i] = new Amgen.Deepavali.Editor.SymbolMaskSource { label = labels[i], mask = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path) };
}
var petalSet = Amgen.Deepavali.Editor.FlowerSymbolAtlasBuilder.Build(source);

string centerTexture = folder + "/Center Phases and Mandala.png";
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(centerTexture);
importer.textureType = UnityEditor.TextureImporterType.Default; importer.sRGBTexture = false;
importer.alphaSource = UnityEditor.TextureImporterAlphaSource.None; importer.alphaIsTransparency = false;
importer.mipmapEnabled = true; importer.isReadable = false;
importer.wrapMode = UnityEngine.TextureWrapMode.Clamp; importer.filterMode = UnityEngine.FilterMode.Trilinear;
importer.maxTextureSize = 2048;
importer.SetPlatformTextureSettings(new UnityEditor.TextureImporterPlatformSettings { name = "Standalone", overridden = true, maxTextureSize = 2048, format = UnityEditor.TextureImporterFormat.BC4, compressionQuality = 100 });
importer.SetPlatformTextureSettings(new UnityEditor.TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 2048, format = UnityEditor.TextureImporterFormat.ETC2_RGB4, compressionQuality = 100 });
importer.SaveAndReimport();
string centerSetPath = folder + "/Center Phases and Mandala.asset";
var centerSet = UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerSymbolSet>(centerSetPath);
if (centerSet == null)
{
    centerSet = UnityEngine.ScriptableObject.CreateInstance<Amgen.Deepavali.FlowerSymbolSet>();
    UnityEditor.AssetDatabase.CreateAsset(centerSet, centerSetPath);
}
var centerData = new UnityEditor.SerializedObject(centerSet);
centerData.FindProperty("atlas").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(centerTexture);
centerData.FindProperty("maxMipLevel").floatValue = 4;
var entries = centerData.FindProperty("symbols"); entries.arraySize = 7;
var centerLabels = new[] { "Gold - Lotus", "Pink - Sunburst", "Purple - Star", "Green - Sprout", "Blue - Diya", "Orange - People and Heart", "Initial - Mandala" };
for (int i=0; i<7; i++)
{
    var e = entries.GetArrayElementAtIndex(i);
    e.FindPropertyRelative("label").stringValue = centerLabels[i];
    e.FindPropertyRelative("uvRect").rectValue = new UnityEngine.Rect((i%4)*0.25f + 0.5f/2048, (1-i/4)*0.5f + 0.5f/1024, 511f/2048, 511f/1024);
}
centerData.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.EditorUtility.SetDirty(centerSet);

string colorsFolder = root + "/Palettes/Initial Inner";
if (!UnityEditor.AssetDatabase.IsValidFolder(colorsFolder)) UnityEditor.AssetDatabase.CreateFolder(root + "/Palettes", "Initial Inner");
var startups = new Amgen.Deepavali.FlowerStartupPalette[9];
for (int i=0; i<9; i++)
{
    string path = colorsFolder + (i<8 ? $"/{i+1:00} {labels[i]}.asset" : "/Center Mandala.asset");
    var palette = UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerStartupPalette>(path);
    if (palette == null) { palette = UnityEngine.ScriptableObject.CreateInstance<Amgen.Deepavali.FlowerStartupPalette>(); UnityEditor.AssetDatabase.CreateAsset(palette, path); }
    var data = new UnityEditor.SerializedObject(palette); var p = data.FindProperty("colors");
    p.FindPropertyRelative("label").stringValue = i<8 ? "Initial " + labels[i] : "Initial Mandala";
    p.FindPropertyRelative("fill").colorValue = Color(i<8 ? hex[i] : "#30133F");
    p.FindPropertyRelative("center").colorValue = Color("#30133F");
    p.FindPropertyRelative("ornament").colorValue = Color("#F8CF78");
    p.FindPropertyRelative("accent").colorValue = Color("#F8CF78");
    p.FindPropertyRelative("tertiary").colorValue = Color("#F8CF78");
    p.FindPropertyRelative("useEdgeColor").boolValue = true;
    p.FindPropertyRelative("edge").colorValue = Color("#FFD35A");
    p.FindPropertyRelative("edgeIntensity").floatValue = i<8 ? 5.5f : 1.6f;
    p.FindPropertyRelative("mainPatternIntensity").floatValue = 0.65f;
    p.FindPropertyRelative("accentPatternIntensity").floatValue = 0.65f;
    data.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.EditorUtility.SetDirty(palette);
    startups[i] = palette;
}
void Configure(Amgen.Deepavali.FlowerPaletteController flower)
{
    var data = new UnityEditor.SerializedObject(flower);
    data.FindProperty("startupSymbolSet").objectReferenceValue = petalSet;
    data.FindProperty("centerIconSet").objectReferenceValue = centerSet;
    data.FindProperty("startupCenterPalette").objectReferenceValue = startups[8];
    data.FindProperty("startupCenterIcon").intValue = 6;
    var petals = data.FindProperty("petalAppearances");
    for(int i=0;i<8;i++)
    {
        var p=petals.GetArrayElementAtIndex(i);
        p.FindPropertyRelative("startupPalette").objectReferenceValue=startups[i];
        p.FindPropertyRelative("startupSymbol").intValue=i;
        p.FindPropertyRelative("useStartupArtwork").boolValue=true;
        p.FindPropertyRelative("symbol").intValue=-1;
    }
    data.ApplyModifiedPropertiesWithoutUndo();
}
string prefabPath=root+"/Prefabs/Deepavali Peacock Flower.prefab";
var contents=UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try { Configure(contents.GetComponent<Amgen.Deepavali.FlowerPaletteController>()); UnityEditor.PrefabUtility.SaveAsPrefabAsset(contents,prefabPath); }
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(contents); }
var sceneFlower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
Configure(sceneFlower);
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(sceneFlower);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sceneFlower.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sceneFlower.gameObject.scene);
UnityEditor.AssetDatabase.SaveAssets();
return new { innerPetals=8, patternAtlas=petalSet.Atlas.width+"x"+petalSet.Atlas.height, centerIcons=centerSet.Count, prefab=prefabPath, scene=sceneFlower.gameObject.scene.path };
