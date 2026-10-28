if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
const string root="Assets/HK_Signature/Deepavali/PetalPrototype";
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
if(flower==null||flower.PetalCount!=23)throw new System.InvalidOperationException("Expected 23-petal flower.");
var data=new UnityEditor.SerializedObject(flower);var petals=data.FindProperty("petals");var appearances=data.FindProperty("petalAppearances");
UnityEngine.Color Hex(string value){UnityEngine.ColorUtility.TryParseHtmlString(value,out var c);return c;}
var starts=new Amgen.Deepavali.FlowerStartupPalette[2];var materials=new UnityEngine.Material[2];
var mask=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(root+"/Textures/OuterPetal_Peacock_Regions_RGBA.png");
var inlay=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(root+"/Textures/OuterPetal_Peacock_Inlays.png");
for(int tone=0;tone<2;tone++)
{
    string path=root+"/Palettes/Peacock Startup Tone "+(tone+1)+".asset";
    var preset=UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerStartupPalette>(path);
    if(preset==null){preset=UnityEngine.ScriptableObject.CreateInstance<Amgen.Deepavali.FlowerStartupPalette>();UnityEditor.AssetDatabase.CreateAsset(preset,path);}
    var so=new UnityEditor.SerializedObject(preset);var c=so.FindProperty("colors");
    var fill=Hex(tone==0?"#982264":"#D94791");
    c.FindPropertyRelative("label").stringValue=tone==0?"Opening raspberry pink / blue inlay":"Opening rose pink / pink inlay";
    c.FindPropertyRelative("fill").colorValue=fill;c.FindPropertyRelative("center").colorValue=fill;c.FindPropertyRelative("edge").colorValue=fill;
    c.FindPropertyRelative("ornament").colorValue=Hex("#FFD887");
    c.FindPropertyRelative("accent").colorValue=Hex("#EFA13C");
    c.FindPropertyRelative("tertiary").colorValue=Hex(tone==0?"#247DCC":"#FF8BC4");
    c.FindPropertyRelative("edgeIntensity").floatValue=1.6f/fill.linear.maxColorComponent;
    c.FindPropertyRelative("mainPatternIntensity").floatValue=1.25f;
    c.FindPropertyRelative("accentPatternIntensity").floatValue=.65f;
    so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.EditorUtility.SetDirty(preset);starts[tone]=preset;
    string materialPath=root+"/Materials/Peacock Outer Tone "+(tone+1)+".mat";
    var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath);
    if(mat==null)
    {
        var source=(UnityEngine.Material)appearances.GetArrayElementAtIndex(8+tone).FindPropertyRelative("materialOverride").objectReferenceValue;
        mat=new UnityEngine.Material(source);UnityEditor.AssetDatabase.CreateAsset(mat,materialPath);
    }
    mat.SetTexture("_BaseMap",mask);mat.SetTexture("_TertiaryMap",inlay);
    mat.SetFloat("_Cull",0);mat.SetFloat("_CenterSurface",0);mat.SetFloat("_WaveProgress",1);mat.SetFloat("_ArtworkProgress",1);
    mat.SetFloat("_SymbolEnabled",0);mat.SetFloat("_TargetSymbolEnabled",0);
    foreach(bool target in new[]{false,true})
    {
        string pre=target?"_Target":"_";var pal=preset.Colors;
        mat.SetColor(pre+"BaseColor",pal.fill.linear);mat.SetColor(pre+"PatternColor",pal.ornament.linear);
        mat.SetColor(pre+"AccentColor",pal.accent.linear);mat.SetColor(pre+"TertiaryColor",pal.tertiary.linear);
        mat.SetFloat(pre+"EdgeIntensity",pal.edgeIntensity);mat.SetFloat(pre+"MainPatternIntensity",pal.mainPatternIntensity);mat.SetFloat(pre+"AccentPatternIntensity",pal.accentPatternIntensity);
    }
    UnityEditor.EditorUtility.SetDirty(mat);materials[tone]=mat;
}
UnityEditor.Undo.RecordObject(flower,"Apply peacock outer artwork and opening colors");
var assignments=new System.Collections.Generic.List<object>();
for(int i=8;i<23;i++)
{
    int tone=(i-8)%2;var p=appearances.GetArrayElementAtIndex(i);
    p.FindPropertyRelative("startupPalette").objectReferenceValue=starts[tone];
    p.FindPropertyRelative("materialOverride").objectReferenceValue=materials[tone];
    p.FindPropertyRelative("symbol").intValue=-1;
    var surface=(UnityEngine.Renderer)petals.GetArrayElementAtIndex(i).objectReferenceValue;
    UnityEditor.Undo.RecordObject(surface,"Apply peacock outer material");surface.sharedMaterial=materials[tone];
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
    assignments.Add(new{petal=surface.name,startup=starts[tone].name,regularPalette=p.FindPropertyRelative("paletteOverride").objectReferenceValue.name});
}
data.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(flower);
flower.gameObject.name="Deepavali Peacock Flower";
string destination=root+"/Prefabs/Deepavali Peacock Flower.prefab";
UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(flower.gameObject,destination,UnityEditor.InteractionMode.AutomatedAction);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(flower.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(flower.gameObject.scene);UnityEditor.AssetDatabase.SaveAssets();
var result=new{prefab=destination,assignments,shaderMessages=UnityEditor.ShaderUtil.GetShaderMessages(materials[0].shader)};
System.IO.File.WriteAllText("output/flower-outer-peacock/setup-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
