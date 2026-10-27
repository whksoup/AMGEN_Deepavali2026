// Build an editor-authored region data texture, seven palette presets and a review scene.
var root = "Assets/HK_Signature/Deepavali/PetalPrototype";
var scenePath = "Assets/Scenes/Deepavali_PaletteTest.unity";
var prefabPath = root + "/Prefabs/Deepavali Palette Inner Layer.prefab";
var palettePath = root + "/Palettes/Deepavali Seven States.asset";
var maskPath = root + "/Textures/Petal_Regions_RGBA.png";
var materialPath = root + "/Materials/Petal Palette Lit.mat";
var centerMaterialPath = root + "/Materials/Center Palette.mat";
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new System.InvalidOperationException("Preserve unsaved scenes first.");
foreach (var path in new[] {scenePath,prefabPath,palettePath,maskPath,materialPath,centerMaterialPath})
    if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) != null || System.IO.File.Exists(path)) throw new System.InvalidOperationException("Refusing to overwrite: " + path);
var shader = UnityEngine.Shader.Find("Amgen/Deepavali/Petal Palette Lit");
if (shader == null || UnityEditor.ShaderUtil.ShaderHasError(shader)) throw new System.InvalidOperationException("Palette shader must compile first.");
float Smooth(float a, float b, float value) { float t = UnityEngine.Mathf.Clamp01((value-a)/(b-a)); return t*t*(3f-2f*t); }
var source = new UnityEngine.Texture2D(2,2,UnityEngine.TextureFormat.RGBA32,false,true);
var edge = new UnityEngine.Texture2D(2,2,UnityEngine.TextureFormat.RGBA32,false,true);
var packed = new UnityEngine.Texture2D(512,1024,UnityEngine.TextureFormat.RGBA32,false,true);
try
{
    source.LoadImage(System.IO.File.ReadAllBytes(root + "/Textures/Petal_Deepavali_BaseColor.png"));
    edge.LoadImage(System.IO.File.ReadAllBytes(root + "/Textures/Petal_EdgeEmission.png"));
    var pixels = new UnityEngine.Color[512*1024];
    for (int y = 0; y < 1024; y++) for (int x = 0; x < 512; x++)
    {
        float u = (x+0.5f)/512f, v = (y+0.5f)/1024f;
        // CPU reads are raw sRGB source bytes. Ratios separate the original warm
        // ornament from its burgundy substrate; the source artwork is never edited.
        var sample = source.GetPixelBilinear(u,v);
        float ratio = sample.g / UnityEngine.Mathf.Max(sample.r,0.001f);
        float ornament = Smooth(0.18f,0.28f,ratio);
        float primary = Smooth(0.42f,0.78f,ratio);
        float detail = UnityEngine.Mathf.Clamp(sample.r / UnityEngine.Mathf.Lerp(0.58f,1f,ornament),0.5f,1.15f)/1.15f;
        pixels[y*512+x] = new UnityEngine.Color(ornament,primary,detail,edge.GetPixelBilinear(u,v).r);
    }
    packed.SetPixels(pixels); packed.Apply();
    System.IO.File.WriteAllBytes(maskPath,packed.EncodeToPNG());
}
finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(edge); UnityEngine.Object.DestroyImmediate(packed); }
UnityEditor.AssetDatabase.ImportAsset(maskPath,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(maskPath);
importer.textureType = UnityEditor.TextureImporterType.Default;
importer.sRGBTexture = false;
importer.alphaSource = UnityEditor.TextureImporterAlphaSource.FromInput;
importer.alphaIsTransparency = false;
importer.mipmapEnabled = true;
importer.isReadable = false;
importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
importer.filterMode = UnityEngine.FilterMode.Trilinear;
importer.maxTextureSize = 1024;
importer.textureCompression = UnityEditor.TextureImporterCompression.CompressedHQ;
importer.SetPlatformTextureSettings(new UnityEditor.TextureImporterPlatformSettings { name="Standalone",overridden=true,maxTextureSize=1024,format=UnityEditor.TextureImporterFormat.BC7,compressionQuality=100 });
importer.SetPlatformTextureSettings(new UnityEditor.TextureImporterPlatformSettings { name="Android",overridden=true,maxTextureSize=1024,format=UnityEditor.TextureImporterFormat.ETC2_RGBA8,compressionQuality=100 });
importer.SaveAndReimport();

UnityEngine.Color Hex(string hex) { UnityEngine.ColorUtility.TryParseHtmlString(hex,out var c); return c; }
var rows = new[] {
    new[]{"Base",  "#8D1740","#FFD46A","#F58A14","#FFB847","#2D163F"},
    new[]{"Gold",  "#9B4B04","#FFD44B","#F69E08","#FFC839","#5B2D05"},
    new[]{"Pink",  "#AC103F","#FFBDDB","#FF6840","#FF81C3","#5C0F30"},
    new[]{"Purple","#3B0964","#D69DFF","#A32BDB","#CB6AFF","#210D38"},
    new[]{"Green", "#135622","#F8DA4B","#71A82A","#C7F24A","#143C1B"},
    new[]{"Blue",  "#063C80","#A9D9FF","#D9B43D","#6FBDFF","#092B4E"},
    new[]{"Orange","#9F2705","#FFCB54","#FA6E13","#FF9B32","#5A2107"}
};
if (!UnityEditor.AssetDatabase.IsValidFolder(root+"/Palettes")) UnityEditor.AssetDatabase.CreateFolder(root,"Palettes");
var paletteSet = UnityEngine.ScriptableObject.CreateInstance<Amgen.Deepavali.FlowerPaletteSet>();
paletteSet.name = "Deepavali Seven States";
var setData = new UnityEditor.SerializedObject(paletteSet);
var states = setData.FindProperty("states"); states.arraySize = 7;
for (int i = 0; i < 7; i++)
{
    var item = states.GetArrayElementAtIndex(i);
    item.FindPropertyRelative("label").stringValue = rows[i][0];
    var names = new[]{"fill","ornament","accent","edge","center"};
    for(int c=0;c<names.Length;c++) item.FindPropertyRelative(names[c]).colorValue=Hex(rows[i][c+1]);
    item.FindPropertyRelative("edgeIntensity").floatValue=3f;
}
setData.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.AssetDatabase.CreateAsset(paletteSet,palettePath);
var material = new UnityEngine.Material(shader) { name="Petal Palette Lit" };
material.SetTexture("_BaseMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(maskPath));
material.SetColor("_BaseColor",Hex(rows[0][1]).linear);
material.SetColor("_PatternColor",Hex(rows[0][2]).linear);
material.SetColor("_AccentColor",Hex(rows[0][3]).linear);
material.SetColor("_EmissionColor",Hex(rows[0][4]).linear*3f);
material.SetFloat("_Smoothness",0.22f);
UnityEditor.AssetDatabase.CreateAsset(material,materialPath);
var centerMaterial = new UnityEngine.Material(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(root+"/Materials/Center Purple.mat")) {name="Center Palette"};
centerMaterial.SetColor("_BaseColor",Hex(rows[0][5]).linear);
UnityEditor.AssetDatabase.CreateAsset(centerMaterial,centerMaterialPath);
if (!UnityEditor.AssetDatabase.CopyAsset("Assets/Scenes/Deepavali_TexturedPetalTest.unity",scenePath)) throw new System.InvalidOperationException("Scene copy failed.");
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
var old = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerEmissionController>();
var layer = old.gameObject;
UnityEditor.PrefabUtility.UnpackPrefabInstance(layer,UnityEditor.PrefabUnpackMode.OutermostRoot,UnityEditor.InteractionMode.AutomatedAction);
UnityEngine.Object.DestroyImmediate(old);
layer.name="Deepavali Palette Inner Layer";
var petals=layer.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
foreach(var petal in petals) {petal.sharedMaterial=material; UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(petal);}
var disc=UnityEngine.GameObject.Find("Center Disc").GetComponent<UnityEngine.Renderer>();
disc.sharedMaterial=centerMaterial;
var controller=layer.AddComponent<Amgen.Deepavali.FlowerPaletteController>();
var data=new UnityEditor.SerializedObject(controller);
data.FindProperty("paletteSet").objectReferenceValue=UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerPaletteSet>(palettePath);
data.FindProperty("petalMaterial").objectReferenceValue=material;
data.FindProperty("centerDisc").objectReferenceValue=disc;
var refs=data.FindProperty("petals"); refs.arraySize=petals.Length;
for(int i=0;i<petals.Length;i++) refs.GetArrayElementAtIndex(i).objectReferenceValue=petals[i];
data.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(layer,prefabPath,UnityEditor.InteractionMode.AutomatedAction);
var input=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalLayerPointerInput>();
var inputData=new UnityEditor.SerializedObject(input);
inputData.FindProperty("flowerEmission").objectReferenceValue=null;

var canvas=UnityEngine.GameObject.Find("Test Instructions").GetComponent<UnityEngine.Canvas>();
canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
var label=canvas.GetComponentInChildren<UnityEngine.UI.Text>();
label.text="FLOWER COLOR STATES\nChoose a palette. Tap a petal to test its movement.";
label.rectTransform.anchoredPosition=new UnityEngine.Vector2(0f,86f);
var bar=new UnityEngine.GameObject("Palette Choices",typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.GridLayoutGroup));
bar.transform.SetParent(canvas.transform,false);
var barRect=bar.GetComponent<UnityEngine.RectTransform>();
barRect.anchorMin=new UnityEngine.Vector2(0,0); barRect.anchorMax=new UnityEngine.Vector2(1,0); barRect.pivot=new UnityEngine.Vector2(0.5f,0);
barRect.anchoredPosition=new UnityEngine.Vector2(0,20); barRect.sizeDelta=new UnityEngine.Vector2(0,60);
var grid=bar.GetComponent<UnityEngine.UI.GridLayoutGroup>();
grid.cellSize=new UnityEngine.Vector2(104,48); grid.spacing=new UnityEngine.Vector2(8,0);
grid.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount=7; grid.childAlignment=UnityEngine.TextAnchor.MiddleCenter;
for(int i=0;i<7;i++)
{
    var go=new UnityEngine.GameObject(rows[i][0],typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));
    go.transform.SetParent(bar.transform,false);
    var image=go.GetComponent<UnityEngine.UI.Image>(); image.color=Hex(rows[i][1]);
    var button=go.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic=image;
    var colors=button.colors; colors.normalColor=UnityEngine.Color.white; colors.highlightedColor=new UnityEngine.Color(1.15f,1.15f,1.15f); colors.selectedColor=colors.highlightedColor; colors.pressedColor=new UnityEngine.Color(0.75f,0.75f,0.75f); colors.fadeDuration=0; button.colors=colors;
    UnityEditor.Events.UnityEventTools.AddIntPersistentListener(button.onClick,controller.SelectState,i);
    var textGo=new UnityEngine.GameObject("Label",typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.Text)); textGo.transform.SetParent(go.transform,false);
    var text=textGo.GetComponent<UnityEngine.UI.Text>(); text.font=UnityEngine.Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf"); text.text=rows[i][0]; text.fontSize=20; text.alignment=UnityEngine.TextAnchor.MiddleCenter; text.color=UnityEngine.Color.white; text.raycastTarget=false;
    text.rectTransform.anchorMin=UnityEngine.Vector2.zero; text.rectTransform.anchorMax=UnityEngine.Vector2.one; text.rectTransform.offsetMin=text.rectTransform.offsetMax=UnityEngine.Vector2.zero;
}
var blocked=inputData.FindProperty("blockedScreenAreas"); blocked.arraySize=1; blocked.GetArrayElementAtIndex(0).objectReferenceValue=barRect;
inputData.ApplyModifiedPropertiesWithoutUndo();
var eventSystem=new UnityEngine.GameObject("Palette UI Events",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
UnityEngine.Camera.main.transform.position=new UnityEngine.Vector3(0,1.65f,0.045f);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.Selection.activeGameObject=layer;
var output=System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,"output/flower-palettes");
System.IO.Directory.CreateDirectory(output);
System.IO.File.WriteAllText(System.IO.Path.Combine(output,"palette-colors.csv"),"State,Fill,Ornament,Accent,Edge,Center\n"+string.Join("\n",rows.Select(r=>string.Join(",",r))));
return new {scene=scenePath,prefab=prefabPath,palettes=palettePath,texture=maskPath,states=rows.Select(r=>r[0]).ToArray(),shaderErrors=UnityEditor.ShaderUtil.GetShaderMessages(shader).Select(m=>m.message).ToArray()};
