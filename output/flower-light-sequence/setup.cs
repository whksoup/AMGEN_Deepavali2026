if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode before setup.");
const string folder="Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Light Sequence";
System.IO.Directory.CreateDirectory(folder);UnityEditor.AssetDatabase.Refresh();
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var data=new UnityEditor.SerializedObject(flower);var appearances=data.FindProperty("petalAppearances");
var clones=new System.Collections.Generic.Dictionary<Amgen.Deepavali.FlowerPaletteSet,Amgen.Deepavali.FlowerPaletteSet>();
Amgen.Deepavali.FlowerPaletteSet Extend(Amgen.Deepavali.FlowerPaletteSet source)
{
    if(clones.TryGetValue(source,out var existing))return existing;
    string name=source.name+" - Eight Colors",path=folder+"/"+name+".asset";
    var copy=UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerPaletteSet>(path);
    if(copy==null){copy=UnityEngine.Object.Instantiate(source);copy.name=name;UnityEditor.AssetDatabase.CreateAsset(copy,path);}
    var so=new UnityEditor.SerializedObject(copy);var states=so.FindProperty("states");states.arraySize=9;
    bool outer=source.name.Contains("Outer");float tone=outer?(source.name.Contains("2")?.85f:.7f):1;
    for(int index=7;index<=8;index++)
    {
        bool cyan=index==7;var s=states.GetArrayElementAtIndex(index);
        UnityEngine.Color fill=cyan?new UnityEngine.Color(.02f,.38f,.48f):new UnityEngine.Color(.54f,.018f,.035f);
        fill*=tone;fill.a=1;
        s.FindPropertyRelative("label").stringValue=(cyan?"Cyan":"Red")+(outer?" / "+source.name:"");
        s.FindPropertyRelative("fill").colorValue=fill;
        s.FindPropertyRelative("ornament").colorValue=cyan?new UnityEngine.Color(.64f,.96f,1):new UnityEngine.Color(1,.68f,.44f);
        s.FindPropertyRelative("accent").colorValue=cyan?new UnityEngine.Color(.14f,.66f,.9f):new UnityEngine.Color(.96f,.15f,.06f);
        s.FindPropertyRelative("tertiary").colorValue=new UnityEngine.Color(1,.75f,.25f);
        s.FindPropertyRelative("useEdgeColor").boolValue=true;
        s.FindPropertyRelative("edge").colorValue=cyan?new UnityEngine.Color(.04f,.85f,1):new UnityEngine.Color(1,.04f,.06f);
        s.FindPropertyRelative("center").colorValue=cyan?new UnityEngine.Color(.015f,.20f,.27f):new UnityEngine.Color(.29f,.008f,.02f);
        s.FindPropertyRelative("edgeIntensity").floatValue=cyan?5.2f:7f;
        s.FindPropertyRelative("mainPatternIntensity").floatValue=outer?3.2f:3.5f;
        s.FindPropertyRelative("accentPatternIntensity").floatValue=1.2f;
    }
    so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.EditorUtility.SetDirty(copy);clones.Add(source,copy);return copy;
}
data.FindProperty("paletteSet").objectReferenceValue=Extend((Amgen.Deepavali.FlowerPaletteSet)data.FindProperty("paletteSet").objectReferenceValue);
for(int i=0;i<appearances.arraySize;i++)
{
    var p=appearances.GetArrayElementAtIndex(i).FindPropertyRelative("paletteOverride");
    if(p.objectReferenceValue!=null)p.objectReferenceValue=Extend((Amgen.Deepavali.FlowerPaletteSet)p.objectReferenceValue);
}
data.ApplyModifiedPropertiesWithoutUndo();UnityEditor.EditorUtility.SetDirty(flower);
var preset=UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerSequencePreset>(folder+"/Flower Sequence.asset");
if(preset==null){preset=UnityEngine.ScriptableObject.CreateInstance<Amgen.Deepavali.FlowerSequencePreset>();UnityEditor.AssetDatabase.CreateAsset(preset,folder+"/Flower Sequence.asset");}
const string lighting="Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Spatial Lighting";
preset.innerMap=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(lighting+"/Inner Light Coordinates.png");
preset.outerMap=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(lighting+"/Outer Light Coordinates.png");
UnityEditor.EditorUtility.SetDirty(preset);
var go=UnityEngine.GameObject.Find("Flower Light Sequence")??new UnityEngine.GameObject("Flower Light Sequence");
var sequence=go.GetComponent<Amgen.Deepavali.FlowerLightSequence>()??go.AddComponent<Amgen.Deepavali.FlowerLightSequence>();
sequence.flower=flower;sequence.preset=preset;sequence.autoPreview=false;
var preview=go.GetComponent<Amgen.Deepavali.FlowerSequencePreview>()??go.AddComponent<Amgen.Deepavali.FlowerSequencePreview>();
preview.sequence=sequence;preview.previewCamera=UnityEngine.Camera.main;
preview.reference=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(lighting+"/Reference.png");
foreach(var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
foreach(var input in UnityEngine.Object.FindObjectsByType<Amgen.Deepavali.PetalLayerPointerInput>(UnityEngine.FindObjectsSortMode.None))input.enabled=false;
UnityEditor.EditorUtility.SetDirty(sequence);UnityEditor.EditorUtility.SetDirty(preview);
UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(go.scene);
UnityEditor.Selection.activeGameObject=go;
return "Saved full-flower light sequence with "+clones.Count+" palette sets, each with Base plus eight distinct result colors.";
