if(UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
var root="Assets/HK_Signature/Deepavali/PetalPrototype";
var symbolsFolder=root+"/Symbols";
var sourcesFolder=symbolsFolder+"/Sources";
if(!UnityEditor.AssetDatabase.IsValidFolder(symbolsFolder))UnityEditor.AssetDatabase.CreateFolder(root,"Symbols");
if(!UnityEditor.AssetDatabase.IsValidFolder(sourcesFolder))UnityEditor.AssetDatabase.CreateFolder(symbolsFolder,"Sources");
var labels=new[]{"Blank","Diamond","Ring","Rosette"};
var inputMasks=new UnityEngine.Texture2D[4];
float Smooth(float a,float b,float v){float t=UnityEngine.Mathf.Clamp01((v-a)/(b-a));return t*t*(3-2*t);}
for(int s=0;s<4;s++)
{
    string path=sourcesFolder+"/"+labels[s]+".png";
    if(!System.IO.File.Exists(path))
    {
        // Technical sample masks, authored once. All interior pixels erase the old pattern.
        var texture=new UnityEngine.Texture2D(224,480,UnityEngine.TextureFormat.RGBA32,false,true);
        try
        {
            var pixels=new UnityEngine.Color32[224*480];
            for(int y=0;y<480;y++)for(int x=0;x<224;x++)
            {
                float u=(x+0.5f)/224f,v=(y+0.5f)/480f;
                float px=(u-0.5f)*0.182f,py=(v-0.62f)*0.263f;
                float radius=UnityEngine.Mathf.Sqrt(px*px+py*py),coverage=0f,role=Smooth(-0.012f,0.012f,py);
                if(s==1)coverage=1-Smooth(0.96f,1.04f,UnityEngine.Mathf.Abs(px)/0.044f+UnityEngine.Mathf.Abs(py)/0.056f);
                if(s==2)coverage=UnityEngine.Mathf.Max(1-Smooth(0.005f,0.007f,UnityEngine.Mathf.Abs(radius-0.039f)),1-Smooth(0.010f,0.012f,radius));
                if(s==3){float edge=0.033f+0.011f*UnityEngine.Mathf.Cos(UnityEngine.Mathf.Atan2(py,px)*8);coverage=1-Smooth(edge-0.001f,edge+0.001f,radius);role=Smooth(0.015f,0.023f,radius);}
                pixels[y*224+x]=new UnityEngine.Color(coverage,role,1f/1.15f,1f);
            }
            texture.SetPixels32(pixels);texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());
        }
        finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    Amgen.Deepavali.Editor.FlowerSymbolAtlasBuilder.ConfigureTexture(path);
    inputMasks[s]=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
}
string sourcePath=symbolsFolder+"/Sample Symbol Atlas Source.asset";
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.Editor.FlowerSymbolAtlasSource>(sourcePath);
if(source==null)
{
    source=UnityEngine.ScriptableObject.CreateInstance<Amgen.Deepavali.Editor.FlowerSymbolAtlasSource>();
    source.outputName="Sample Symbols";source.padding=16;
    source.tiles=Enumerable.Range(0,4).Select(i=>new Amgen.Deepavali.Editor.SymbolMaskSource{label=labels[i],mask=inputMasks[i]}).ToArray();
    UnityEditor.AssetDatabase.CreateAsset(source,sourcePath);
}
var catalog=Amgen.Deepavali.Editor.FlowerSymbolAtlasBuilder.Build(source);

// Keep a similar HDR peak across palettes while retaining exact fill chromaticity.
var palette=UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerPaletteSet>(root+"/Palettes/Deepavali Seven States.asset");
var palettes=new UnityEditor.SerializedObject(palette);
var states=palettes.FindProperty("states");
for(int i=0;i<states.arraySize;i++)
{
    var item=states.GetArrayElementAtIndex(i);
    var fillColor=item.FindPropertyRelative("fill").colorValue.linear;
    item.FindPropertyRelative("edgeIntensity").floatValue=3f/UnityEngine.Mathf.Max(0.001f,fillColor.r,fillColor.g,fillColor.b);
}
palettes.ApplyModifiedPropertiesWithoutUndo();UnityEditor.EditorUtility.SetDirty(palette);
palette.TryGet(Amgen.Deepavali.FlowerPaletteState.Base,out var basePalette);
var material=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(root+"/Materials/Petal Palette Lit.mat");
foreach(var prefix in new[]{"","_Target"})
{
    material.SetColor(prefix==""?"_BaseColor":"_TargetBaseColor",basePalette.fill.linear);
    material.SetColor(prefix==""?"_PatternColor":"_TargetPatternColor",basePalette.ornament.linear);
    material.SetColor(prefix==""?"_AccentColor":"_TargetAccentColor",basePalette.accent.linear);
    material.SetFloat(prefix==""?"_EdgeIntensity":"_TargetEdgeIntensity",basePalette.edgeIntensity);
}
material.SetFloat("_WaveProgress",1);material.SetFloat("_EmissionScale",1);material.SetFloat("_SymbolEnabled",0);
UnityEditor.EditorUtility.SetDirty(material);

string prefabPath=root+"/Prefabs/Deepavali Palette Inner Layer.prefab";
var contents=UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var controller=contents.GetComponent<Amgen.Deepavali.FlowerPaletteController>();
    var data=new UnityEditor.SerializedObject(controller);
    data.FindProperty("symbolSet").objectReferenceValue=catalog;
    var appearances=data.FindProperty("petalAppearances");appearances.arraySize=data.FindProperty("petals").arraySize;
    for(int i=0;i<appearances.arraySize;i++)
    {
        var p=appearances.GetArrayElementAtIndex(i);
        p.FindPropertyRelative("palette").enumValueIndex=0;p.FindPropertyRelative("emissionScale").floatValue=1;
        p.FindPropertyRelative("symbol").intValue=-1;
        p.FindPropertyRelative("artworkMin").vector2Value=new UnityEngine.Vector2(-0.091f,0);
        p.FindPropertyRelative("artworkSize").vector2Value=new UnityEngine.Vector2(0.182f,0.263f);
    }
    data.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.SaveAsPrefabAsset(contents,prefabPath);
}
finally{UnityEditor.PrefabUtility.UnloadPrefabContents(contents);}

var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Deepavali_PaletteTest.unity");
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var input=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalLayerPointerInput>();
var canvas=UnityEngine.GameObject.Find("Test Instructions").GetComponent<UnityEngine.Canvas>();
var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();if(scaler==null)scaler=canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new UnityEngine.Vector2(1280,800);scaler.matchWidthOrHeight=0.5f;
var label=canvas.GetComponentInChildren<UnityEngine.UI.Text>();
label.text="All petals | Artwork: Original\nTap a petal to select it. Choose a palette below.";label.fontSize=20;
var labelRect=label.rectTransform;labelRect.anchorMin=labelRect.anchorMax=new UnityEngine.Vector2(0.5f,0);labelRect.pivot=new UnityEngine.Vector2(0.5f,0);labelRect.anchoredPosition=new UnityEngine.Vector2(0,144);labelRect.sizeDelta=new UnityEngine.Vector2(900,60);
var oldControls=canvas.transform.Find("Appearance Controls");if(oldControls!=null)UnityEngine.Object.DestroyImmediate(oldControls.gameObject);
var row=new UnityEngine.GameObject("Appearance Controls",typeof(UnityEngine.RectTransform));row.transform.SetParent(canvas.transform,false);
var rowRect=row.GetComponent<UnityEngine.RectTransform>();rowRect.anchorMin=rowRect.anchorMax=new UnityEngine.Vector2(0.5f,0);rowRect.pivot=new UnityEngine.Vector2(0.5f,0);rowRect.anchoredPosition=new UnityEngine.Vector2(0,82);rowRect.sizeDelta=new UnityEngine.Vector2(776,48);
UnityEngine.RectTransform Rect(string name,UnityEngine.Transform parent,float x,float width)
{
    var go=new UnityEngine.GameObject(name,typeof(UnityEngine.RectTransform));go.transform.SetParent(parent,false);
    var r=go.GetComponent<UnityEngine.RectTransform>();r.anchorMin=new UnityEngine.Vector2(0,0);r.anchorMax=new UnityEngine.Vector2(0,1);r.pivot=new UnityEngine.Vector2(0,0.5f);r.anchoredPosition=new UnityEngine.Vector2(x,0);r.sizeDelta=new UnityEngine.Vector2(width,0);return r;
}
UnityEngine.UI.Text Text(string name,UnityEngine.RectTransform parent,string words,int size)
{
    var r=Rect(name,parent,0,parent.sizeDelta.x);r.anchorMin=UnityEngine.Vector2.zero;r.anchorMax=UnityEngine.Vector2.one;r.offsetMin=r.offsetMax=UnityEngine.Vector2.zero;
    var t=r.gameObject.AddComponent<UnityEngine.UI.Text>();t.font=UnityEngine.Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");t.fontSize=size;t.text=words;t.alignment=UnityEngine.TextAnchor.MiddleCenter;t.color=UnityEngine.Color.white;t.raycastTarget=false;return t;
}
UnityEngine.UI.Button Button(string name,string title,float x,float width)
{
    var r=Rect(name,rowRect,x,width);var bg=r.gameObject.AddComponent<UnityEngine.UI.Image>();bg.color=new UnityEngine.Color(0.17f,0.19f,0.23f,0.95f);
    var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=bg;Text("Label",r,title,18);return b;
}
var all=Button("All Petals","All petals",0,120);var mixed=Button("Mixed Palettes","Mixed colors",128,144);var artwork=Button("Next Artwork","Next artwork",280,152);
var glow=Rect("Glow Label",rowRect,440,64);Text("Label",glow,"Glow",18);
var sliderRect=Rect("Glow",rowRect,516,260);var slider=sliderRect.gameObject.AddComponent<UnityEngine.UI.Slider>();
var track=Rect("Track",sliderRect,0,260);track.anchorMin=new UnityEngine.Vector2(0,0.38f);track.anchorMax=new UnityEngine.Vector2(1,0.62f);track.offsetMin=track.offsetMax=UnityEngine.Vector2.zero;track.gameObject.AddComponent<UnityEngine.UI.Image>().color=new UnityEngine.Color(0.13f,0.14f,0.18f);
var fill=Rect("Fill",track,0,260);fill.anchorMin=UnityEngine.Vector2.zero;fill.anchorMax=UnityEngine.Vector2.one;fill.offsetMin=fill.offsetMax=UnityEngine.Vector2.zero;fill.gameObject.AddComponent<UnityEngine.UI.Image>().color=new UnityEngine.Color(0.72f,0.55f,0.82f);
var handle=Rect("Handle",sliderRect,0,16);handle.anchorMin=new UnityEngine.Vector2(0,0.18f);handle.anchorMax=new UnityEngine.Vector2(0,0.82f);handle.pivot=new UnityEngine.Vector2(0.5f,0.5f);handle.gameObject.AddComponent<UnityEngine.UI.Image>().color=UnityEngine.Color.white;
slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<UnityEngine.UI.Image>();slider.minValue=0;slider.maxValue=3;slider.value=1;
var preview=canvas.GetComponent<Amgen.Deepavali.FlowerAppearancePreview>();if(preview==null)preview=canvas.gameObject.AddComponent<Amgen.Deepavali.FlowerAppearancePreview>();
var previewData=new UnityEditor.SerializedObject(preview);previewData.FindProperty("flower").objectReferenceValue=flower;previewData.FindProperty("pointerInput").objectReferenceValue=input;previewData.FindProperty("selectionLabel").objectReferenceValue=label;previewData.FindProperty("emissionSlider").objectReferenceValue=slider;previewData.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.Events.UnityEventTools.AddPersistentListener(all.onClick,preview.SelectAll);
UnityEditor.Events.UnityEventTools.AddPersistentListener(mixed.onClick,preview.ShowMixedPalettes);
UnityEditor.Events.UnityEventTools.AddPersistentListener(artwork.onClick,preview.CycleSymbol);
UnityEditor.Events.UnityEventTools.AddPersistentListener(slider.onValueChanged,preview.SetEmission);
var paletteBar=UnityEngine.GameObject.Find("Palette Choices").transform;
for(int i=0;i<7;i++)
{
    var button=paletteBar.GetChild(i).GetComponent<UnityEngine.UI.Button>();
    while(button.onClick.GetPersistentEventCount()>0)UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick,0);
    UnityEditor.Events.UnityEventTools.AddIntPersistentListener(button.onClick,preview.SelectPalette,i);
}
var inputData=new UnityEditor.SerializedObject(input);var blocked=inputData.FindProperty("blockedScreenAreas");blocked.arraySize=2;
blocked.GetArrayElementAtIndex(0).objectReferenceValue=paletteBar.GetComponent<UnityEngine.RectTransform>();blocked.GetArrayElementAtIndex(1).objectReferenceValue=rowRect;inputData.ApplyModifiedPropertiesWithoutUndo();
UnityEngine.Camera.main.transform.position=new UnityEngine.Vector3(0,1.65f,0.11f);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.AssetDatabase.SaveAssets();
return new {scene=scene.path,petals=flower.PetalCount,symbols=catalog.Count,atlasWidth=catalog.Atlas.width,atlasHeight=catalog.Atlas.height,safeMip=catalog.MaxMipLevel};
