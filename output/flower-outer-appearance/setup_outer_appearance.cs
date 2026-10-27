if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
const string root="Assets/HK_Signature/Deepavali/PetalPrototype";
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
if(flower==null || flower.PetalCount!=8)throw new System.InvalidOperationException("Expected the existing eight-petal palette controller.");
var outer=flower.GetComponentInChildren<Amgen.Deepavali.OuterPetalLayerMotion>();
var surfaces=outer.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().OrderBy(s=>s.name).ToArray();
if(surfaces.Length!=15)throw new System.InvalidOperationException("Expected 15 outer petals.");
var shader=UnityEngine.Shader.Find("Amgen/Deepavali/Petal Palette Lit");
if(UnityEditor.ShaderUtil.ShaderHasError(shader))throw new System.InvalidOperationException("Shader compilation failed.");
UnityEngine.Color Hex(string text){UnityEngine.ColorUtility.TryParseHtmlString(text,out var color);return color;}
var labels=new[]{"Base / Red","Gold","Pink","Purple","Green","Blue","Orange"};
var fills=new[]{
    new[]{"#610F2A","#713503","#780D3E","#2F124F","#0B3D2A","#0C2D56","#7F2508"},
    new[]{"#9E2345","#A85E0B","#B42762","#50257A","#216D4E","#24548B","#B94912"}
};
var toneSets=new Amgen.Deepavali.FlowerPaletteSet[2];
var materials=new UnityEngine.Material[2];
var mask=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(root+"/Textures/OuterPetal_Regions_RGBA.png");
for(int tone=0;tone<2;tone++)
{
    string setPath=root+"/Palettes/Deepavali Outer Tone "+(tone+1)+".asset";
    var set=UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerPaletteSet>(setPath);
    if(set==null){set=UnityEngine.ScriptableObject.CreateInstance<Amgen.Deepavali.FlowerPaletteSet>();UnityEditor.AssetDatabase.CreateAsset(set,setPath);}
    var data=new UnityEditor.SerializedObject(set);var states=data.FindProperty("states");states.arraySize=7;
    for(int state=0;state<7;state++)
    {
        var p=states.GetArrayElementAtIndex(state);var fill=Hex(fills[tone][state]);
        p.FindPropertyRelative("label").stringValue=labels[state]+" - Tone "+(tone+1);
        p.FindPropertyRelative("fill").colorValue=fill;
        p.FindPropertyRelative("ornament").colorValue=Hex(tone==0?"#FFDB83":"#FFD16A");
        p.FindPropertyRelative("accent").colorValue=Hex(tone==0?"#ECA638":"#FFAB36");
        p.FindPropertyRelative("edge").colorValue=fill;
        p.FindPropertyRelative("center").colorValue=fill;
        p.FindPropertyRelative("edgeIntensity").floatValue=1.6f/UnityEngine.Mathf.Max(.001f,fill.linear.maxColorComponent);
        p.FindPropertyRelative("mainPatternIntensity").floatValue=1.15f;
        p.FindPropertyRelative("accentPatternIntensity").floatValue=.55f;
    }
    data.ApplyModifiedPropertiesWithoutUndo();UnityEditor.EditorUtility.SetDirty(set);toneSets[tone]=set;
    string matPath=root+"/Materials/Outer Petal Palette Tone "+(tone+1)+".mat";
    var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(matPath);
    if(mat==null){mat=new UnityEngine.Material(shader);UnityEditor.AssetDatabase.CreateAsset(mat,matPath);}
    mat.SetTexture("_BaseMap",mask);mat.SetFloat("_Smoothness",.28f);mat.SetFloat("_Cull",0);
    mat.SetFloat("_CenterSurface",0);mat.SetFloat("_EmissionScale",1);mat.SetFloat("_WaveProgress",1);
    mat.SetFloat("_ArtworkProgress",1);mat.SetFloat("_SymbolEnabled",0);mat.SetFloat("_TargetSymbolEnabled",0);
    set.TryGet(Amgen.Deepavali.FlowerPaletteState.Base,out var palette);
    foreach(var target in new[]{false,true})
    {
        mat.SetColor(target?"_TargetBaseColor":"_BaseColor",palette.fill.linear);
        mat.SetColor(target?"_TargetPatternColor":"_PatternColor",palette.ornament.linear);
        mat.SetColor(target?"_TargetAccentColor":"_AccentColor",palette.accent.linear);
        mat.SetFloat(target?"_TargetEdgeIntensity":"_EdgeIntensity",palette.edgeIntensity);
        mat.SetFloat(target?"_TargetMainPatternIntensity":"_MainPatternIntensity",palette.mainPatternIntensity);
        mat.SetFloat(target?"_TargetAccentPatternIntensity":"_AccentPatternIntensity",palette.accentPatternIntensity);
    }
    UnityEditor.EditorUtility.SetDirty(mat);materials[tone]=mat;
}
UnityEditor.Undo.RecordObject(flower,"Connect outer palette and glow");
var setup=new UnityEditor.SerializedObject(flower);
var petals=setup.FindProperty("petals");var appearances=setup.FindProperty("petalAppearances");
petals.arraySize=23;appearances.arraySize=23;
for(int i=0;i<15;i++)
{
    int index=i+8,tone=i%2;
    petals.GetArrayElementAtIndex(index).objectReferenceValue=surfaces[i];
    var p=appearances.GetArrayElementAtIndex(index);
    p.FindPropertyRelative("palette").enumValueIndex=0;
    p.FindPropertyRelative("paletteOverride").objectReferenceValue=toneSets[tone];
    p.FindPropertyRelative("materialOverride").objectReferenceValue=materials[tone];
    p.FindPropertyRelative("emissionScale").floatValue=1;
    p.FindPropertyRelative("symbol").intValue=-1;
    p.FindPropertyRelative("artworkMin").vector2Value=new UnityEngine.Vector2(-.101f,0);
    p.FindPropertyRelative("artworkSize").vector2Value=new UnityEngine.Vector2(.202f,.346f);
    p.FindPropertyRelative("artworkDuration").floatValue=1.2f;
    p.FindPropertyRelative("artworkSoftness").floatValue=.04f;
    UnityEditor.Undo.RecordObject(surfaces[i],"Apply outer ornament material");
    surfaces[i].sharedMaterial=materials[tone];
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(surfaces[i]);
    var artwork=surfaces[i].GetComponent<Amgen.Deepavali.PetalArtworkController>();
    if(artwork==null)artwork=UnityEditor.Undo.AddComponent<Amgen.Deepavali.PetalArtworkController>(surfaces[i].gameObject);
    var ad=new UnityEditor.SerializedObject(artwork);
    ad.FindProperty("flower").objectReferenceValue=flower;
    ad.FindProperty("surface").objectReferenceValue=surfaces[i];ad.ApplyModifiedPropertiesWithoutUndo();
}
setup.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(flower);
// Save only this assembled prefab; the inherited inner-only prefab keeps its eight-petal layout.
string prefab=UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(flower.gameObject);
if(!prefab.EndsWith("Deepavali Two Layer Flower.prefab"))throw new System.InvalidOperationException("Unexpected prefab destination: "+prefab);
UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(flower.gameObject,prefab,UnityEditor.InteractionMode.AutomatedAction);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(flower.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(flower.gameObject.scene);UnityEditor.AssetDatabase.SaveAssets();
return new{prefab,petals=flower.PetalCount,outerTone1=8,outerTone2=7,paletteSets=toneSets.Select(UnityEditor.AssetDatabase.GetAssetPath).ToArray(),materialNames=materials.Select(m=>m.name).ToArray()};
