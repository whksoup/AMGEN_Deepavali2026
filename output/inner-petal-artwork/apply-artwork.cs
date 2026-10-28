if (Application.isPlaying) throw new System.InvalidOperationException("Edit Mode required.");
if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Deepavali_PetalWheelSequence.unity")
    throw new System.InvalidOperationException("Only apply this artwork to the main wheel scene.");
const string folder = "Assets/HK_Signature/Deepavali/PetalPrototype/Symbols/Rootward Inner";
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
var layout = Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("output/inner-petal-artwork/layout.json"));
string sourcePath = folder + "/Rootward Inner Source.asset";
var source = AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.Editor.FlowerSymbolAtlasSource>(sourcePath);
if (source == null) { source = ScriptableObject.CreateInstance<Amgen.Deepavali.Editor.FlowerSymbolAtlasSource>(); AssetDatabase.CreateAsset(source, sourcePath); }
source.outputName = "Rootward Inner Medallions"; source.padding = 16;
source.tiles = new Amgen.Deepavali.Editor.SymbolMaskSource[8];
for (int i = 0; i < 8; i++)
{
    string label = (string)layout[i]["label"];
    string path = folder + $"/Decorated Masks/{i:00}-{label}.png";
    Amgen.Deepavali.Editor.FlowerSymbolAtlasBuilder.ConfigureTexture(path);
    source.tiles[i] = new Amgen.Deepavali.Editor.SymbolMaskSource { label = label, mask = AssetDatabase.LoadAssetAtPath<Texture2D>(path) };
}
var set = Amgen.Deepavali.Editor.FlowerSymbolAtlasBuilder.Build(source);
var flower = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var so = new SerializedObject(flower);
var appearances = so.FindProperty("petalAppearances");
var changes = new System.Collections.Generic.List<object>();
for (int i = 0; i < 8; i++)
{
    var entry = appearances.GetArrayElementAtIndex(i);
    var renderer = flower.GetPetalRenderer(i);
    if (renderer.name != (string)layout[i]["name"]) throw new System.Exception("Petal index mismatch: " + i);
    var original = (Material)entry.FindPropertyRelative("materialOverride").objectReferenceValue;
    string materialPath = folder + "/Materials/" + renderer.name + " Rootward.mat";
    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    if (material == null) { material = new Material(original); AssetDatabase.CreateAsset(material, materialPath); }
    material.SetFloat("_StartupMeshUV", 1);
    var region = layout[i]["icon_region"].Values<float>().ToArray();
    material.SetVector("_PetalLightIconRegion", new Vector4(region[0], region[1], region[2], region[3]));
    EditorUtility.SetDirty(material);
    changes.Add(new { index = i, renderer = renderer.name, sourceMaterial = AssetDatabase.GetAssetPath(original), material = materialPath, iconRegion = region });
    entry.FindPropertyRelative("materialOverride").objectReferenceValue = material;
    renderer.sharedMaterial = material;
    EditorUtility.SetDirty(renderer);
    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
}
so.FindProperty("startupSymbolSet").objectReferenceValue = set;
so.ApplyModifiedPropertiesWithoutUndo();
PrefabUtility.RecordPrefabInstancePropertyModifications(flower);
EditorUtility.SetDirty(flower);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(flower.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(flower.gameObject.scene);
AssetDatabase.SaveAssets();
if (!System.IO.File.Exists("output/inner-petal-artwork/material-mapping.json"))
    System.IO.File.WriteAllText("output/inner-petal-artwork/material-mapping.json", Newtonsoft.Json.JsonConvert.SerializeObject(changes, Newtonsoft.Json.Formatting.Indented));
return new { scene = flower.gameObject.scene.path, root = flower.name, atlas = AssetDatabase.GetAssetPath(set.Atlas), tiles = set.Count, size = set.Atlas.width + "x" + set.Atlas.height };
