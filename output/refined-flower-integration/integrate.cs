if (Application.isPlaying) throw new System.Exception("Exit Play Mode first.");
const string root="Assets/HK_Signature/Deepavali/PetalPrototype";
const string folder=root+"/Models/Refined Flower";
AssetDatabase.Refresh();
foreach(var path in System.IO.Directory.GetFiles(root+"/Textures/Refined Flower","*.png"))
{
    var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
    importer.sRGBTexture=false; importer.alphaSource=TextureImporterAlphaSource.FromInput;
    importer.alphaIsTransparency=false; importer.mipmapEnabled=!path.EndsWith(" Fit.png");
    importer.wrapMode=TextureWrapMode.Clamp; importer.filterMode=FilterMode.Bilinear;
    importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=512;
    importer.SaveAndReimport();
}
var source=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Deepavali_RefinedFlower.fbx");
var manifest=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("output/refined-flower-integration/source-manifest.json"));
var names=manifest["inner"].Concat(manifest["outer"]).Select(n=>(string)n).ToArray();
var dimensions=manifest["meshes"].ToDictionary(m=>(string)m["name"]);
var sourceSkins=source.GetComponentsInChildren<SkinnedMeshRenderer>().ToDictionary(r=>r.name);
var report=new System.Collections.Generic.List<object>();
void SetPose(Transform target,Transform original,Transform flower)
{
    var matrix=target.parent.worldToLocalMatrix*flower.localToWorldMatrix*original.localToWorldMatrix;
    target.localPosition=matrix.GetColumn(3);target.localRotation=matrix.rotation;target.localScale=matrix.lossyScale;
}
Material Fit(Material original,string name)
{
    string path=folder+"/"+original.name+" - "+name+".mat";
    var material=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(material==null){material=new Material(original);AssetDatabase.CreateAsset(material,path);}
    else material.CopyPropertiesFromMaterial(original);
    material.SetTexture("_ArtworkFitMap",AssetDatabase.LoadAssetAtPath<Texture2D>(root+"/Textures/Refined Flower/"+name+" Fit.png"));
    material.SetFloat("_ArtworkFitEnabled",1);
    material.SetTexture("_BodyShadingMap",AssetDatabase.LoadAssetAtPath<Texture2D>(root+"/Textures/Refined Flower/"+name+" Body.png"));
    EditorUtility.SetDirty(material);return material;
}
void Integrate(Amgen.Deepavali.FlowerPaletteController flower,string context)
{
    if(flower.PetalCount!=23)throw new System.Exception("Expected untouched 23-petal target: "+context);
    var data=new SerializedObject(flower);var appearances=data.FindProperty("petalAppearances");
    var renders=data.FindProperty("petals");
    var old=Enumerable.Range(0,flower.PetalCount).Select(i=>(SkinnedMeshRenderer)flower.GetPetalRenderer(i)).ToArray();
    var outer=flower.GetComponentInChildren<Amgen.Deepavali.OuterPetalLayerMotion>(true);
    var outerBones=new System.Collections.Generic.List<Transform>();
    for(int i=0;i<22;i++)
    {
        var dest=old[i];var src=sourceSkins[names[i]];var dims=dimensions[names[i]];
        var oldBones=new[]{"_Base","_Mid","_Tip"}.Select(s=>dest.bones.Single(b=>b.name.EndsWith(s))).ToArray();
        var newBones=new[]{"_Base","_Mid","_Tip"}.Select(s=>src.bones.Single(b=>b.name.EndsWith(s))).ToArray();
        SetPose(dest.transform,src.transform,flower.transform);
        for(int j=0;j<3;j++){SetPose(oldBones[j],newBones[j],flower.transform);oldBones[j].name=newBones[j].name;}
        // Preserve the original renderer and motion/picking component identities.
        dest.name=names[i];dest.sharedMesh=src.sharedMesh;
        dest.bones=src.bones.Select(b=>oldBones[System.Array.IndexOf(newBones,b)]).ToArray();dest.rootBone=oldBones[0];
        dest.quality=SkinQuality.Bone2;dest.updateWhenOffscreen=false;
        float length=(float)dims["max"][1]-(float)dims["min"][1];float width=(float)dims["max"][0]-(float)dims["min"][0];
        var bounds=src.sharedMesh.bounds;bounds.Expand(.3f);dest.localBounds=bounds;
        var app=appearances.GetArrayElementAtIndex(i);
        var material=app.FindPropertyRelative("materialOverride").objectReferenceValue as Material;
        if(material==null) material=data.FindProperty("petalMaterial").objectReferenceValue as Material;
        var fitted=Fit(material,names[i]);app.FindPropertyRelative("materialOverride").objectReferenceValue=fitted;dest.sharedMaterial=fitted;
        app.FindPropertyRelative("artworkMin").vector2Value=new Vector2((float)dims["min"][0],(float)dims["min"][1]);
        app.FindPropertyRelative("artworkSize").vector2Value=new Vector2(width,length);
        app.FindPropertyRelative("artworkMirroredX").boolValue=true;
        app.FindPropertyRelative("spatialLightingMap").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"/Textures/Refined Flower/"+names[i]+" Light.png");
        if(i>=8)outerBones.AddRange(oldBones);
    }
    appearances.arraySize=22;renders.arraySize=22;data.ApplyModifiedPropertiesWithoutUndo();
    var retired=old[22];var retiredBase=retired.bones.Single(b=>b.name.EndsWith("_Base"));
    UnityEngine.Object.DestroyImmediate(retiredBase.gameObject);UnityEngine.Object.DestroyImmediate(retired.gameObject);
    var od=new SerializedObject(outer);var ob=od.FindProperty("outerBones");ob.arraySize=42;
    for(int i=0;i<42;i++)ob.GetArrayElementAtIndex(i).objectReferenceValue=outerBones[i];
    od.ApplyModifiedPropertiesWithoutUndo();outer.name="Outer Petal Layer - 14";
    var center=(Renderer)data.FindProperty("centerDisc").objectReferenceValue;
    var srcCenter=source.GetComponentsInChildren<MeshFilter>().Single(m=>m.name=="CenterDisc");
    center.GetComponent<MeshFilter>().sharedMesh=srcCenter.sharedMesh;SetPose(center.transform,srcCenter.transform,flower.transform);
    if(center.TryGetComponent<MeshCollider>(out var collider))collider.sharedMesh=srcCenter.sharedMesh;
    foreach(var motion in flower.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>(true))
    {
        var settings=new SerializedObject(motion);
        settings.FindProperty("pulseLift").vector3Value=new Vector3(.08f,.35f,8f);
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
    // Dense visual indices are separate from retained, noncontiguous Blender source IDs.
    foreach(var seq in UnityEngine.Object.FindObjectsByType<Amgen.Deepavali.PetalWheelLightSequence>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        if(seq.flower==flower){seq.innerClockwise=Enumerable.Range(0,8).ToArray();seq.outerClockwise=Enumerable.Range(8,14).ToArray();EditorUtility.SetDirty(seq);}
    foreach(var seq in UnityEngine.Object.FindObjectsByType<Amgen.Deepavali.FlowerLightSequence>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        if(seq.flower==flower){seq.innerClockwise=Enumerable.Range(0,8).ToArray();EditorUtility.SetDirty(seq);}
    EditorUtility.SetDirty(flower);EditorUtility.SetDirty(outer);
    report.Add(new{context,petals=flower.PetalCount,outerBones=outerBones.Count,names});
}
string active=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
foreach(string sceneName in new[]{"Deepavali_PetalWheelSequence","Deepavali_LightSequence","Deepavali_PetalLightingPrototype","Deepavali_PaletteTest"})
{
    var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/"+sceneName+".unity");
    var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>(FindObjectsInactive.Include);
    Integrate(flower,scene.path);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
}
foreach(string name in new[]{"Deepavali Peacock Flower","Deepavali Two Layer Flower"})
{
    string path=root+"/Prefabs/"+name+".prefab";var contents=PrefabUtility.LoadPrefabContents(path);
    try{Integrate(contents.GetComponent<Amgen.Deepavali.FlowerPaletteController>(),path);PrefabUtility.SaveAsPrefabAsset(contents,path);}
    finally{PrefabUtility.UnloadPrefabContents(contents);}
}
AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.OpenScene(active);
System.IO.File.WriteAllText("output/refined-flower-integration/integration-map.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return new{targets=report.Count,petals=22,inner=8,outer=14};
