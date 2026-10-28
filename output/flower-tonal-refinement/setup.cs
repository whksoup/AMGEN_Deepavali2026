if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
const string root = "Assets/HK_Signature/Deepavali/PetalPrototype";
const string folder = root + "/Rendering/Tonal Refinement";
System.IO.Directory.CreateDirectory(folder);
UnityEditor.AssetDatabase.Refresh();
float Smooth(float a, float b, float x) { float t = UnityEngine.Mathf.Clamp01((x-a)/(b-a)); return t*t*(3-2*t); }
UnityEngine.Texture2D BuildMask(string name, string modelPath, float width, float length, bool center)
{
    const int w = 512, h = 512;
    var points = new System.Collections.Generic.List<(UnityEngine.Vector2 a, UnityEngine.Vector2 b)>();
    if (!center)
    {
        var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(modelPath);
        var mesh = model.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>().sharedMesh;
        var uv = mesh.uv; var tris = mesh.triangles;
        var edges = new System.Collections.Generic.Dictionary<(int,int),int>();
        for (int i=0; i<tris.Length; i+=3) for (int k=0; k<3; k++)
        {
            int a=tris[i+k], b=tris[i+(k+1)%3]; var key=(System.Math.Min(a,b),System.Math.Max(a,b));
            edges[key]=edges.TryGetValue(key,out int count)?count+1:1;
        }
        foreach(var e in edges.Where(e=>e.Value==1))
            points.Add((UnityEngine.Vector2.Scale(uv[e.Key.Item1],new UnityEngine.Vector2(width,length)),
                UnityEngine.Vector2.Scale(uv[e.Key.Item2],new UnityEngine.Vector2(width,length))));
        if (points.Count<4) throw new System.Exception("No petal boundary: "+name);
    }
    var pixels = new UnityEngine.Color32[w*h];
    for(int y=0;y<h;y++) for(int x=0;x<w;x++)
    {
        float u=(x+.5f)/w, v=(y+.5f)/h, shade, ao;
        if(center)
        {
            float radius=new UnityEngine.Vector2((u-.5f)*2,(v-.5f)*2).magnitude;
            // Broad annular shadow beneath the bright rim, rather than a thin outline.
            float recess=Smooth(.38f,.72f,radius)*(1-Smooth(.91f,1.0f,radius));
            float bowl=.20f+.12f*(1-Smooth(.08f,.62f,radius));
            float side=.68f+.32f*Smooth(.08f,.92f,u);
            shade=(bowl*(1-.82f*recess)+.20f*Smooth(.94f,1.0f,radius))*side;
            ao=1-.50f*recess;
        }
        else
        {
            var p=new UnityEngine.Vector2(u*width,v*length); float distance2=float.PositiveInfinity;
            foreach(var e in points)
            {
                var ab=e.b-e.a; float t=UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(p-e.a,ab)/ab.sqrMagnitude);
                distance2=UnityEngine.Mathf.Min(distance2,(p-e.a-ab*t).sqrMagnitude);
            }
            float d=UnityEngine.Mathf.Sqrt(distance2);
            // Large gradients cross the whole petal: a shaded root, asymmetric
            // bowl, and a long shoulder that catches light next to the rim.
            float shoulder=1-Smooth(.004f,.070f,d);
            float rootShade=.42f+.58f*Smooth(.07f,.62f,v);
            float crossShade=.42f+.58f*Smooth(.06f,.94f,u);
            float basin=1-Smooth(.18f,.88f,new UnityEngine.Vector2((u-.48f)*2.1f,(v-.57f)*1.9f).magnitude);
            shade=(.18f+.67f*shoulder)*rootShade*crossShade*(1-.38f*basin);
            float rimRecess=(1-Smooth(.005f,.018f,UnityEngine.Mathf.Abs(d-.013f)))*Smooth(.001f,.005f,d);
            ao=(1-.34f*rimRecess)*(.55f+.45f*Smooth(.08f,.48f,v));
        }
        pixels[y*w+x]=new UnityEngine.Color(shade,ao,0,1);
    }
    var tex=new UnityEngine.Texture2D(w,h,UnityEngine.TextureFormat.RGBA32,false,true);
    string path=folder+"/"+name+".png";
    try { tex.SetPixels32(pixels); tex.Apply(); System.IO.File.WriteAllBytes(path,tex.EncodeToPNG()); }
    finally { UnityEngine.Object.DestroyImmediate(tex); }
    UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.sRGBTexture=false; importer.mipmapEnabled=true; importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;
    importer.filterMode=UnityEngine.FilterMode.Bilinear; importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;
    importer.alphaSource=UnityEditor.TextureImporterAlphaSource.None; importer.SaveAndReimport();
    return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
}
var inner=BuildMask("Inner Body Shading",root+"/Models/Deepavali_SinglePetal.fbx",.182f,.263f,false);
var outer=BuildMask("Outer Body Shading",root+"/Models/Deepavali_OuterPetals.fbx",.202f,.346f,false);
var center=BuildMask("Center Body Shading",null,0,0,true);
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var data=new UnityEditor.SerializedObject(flower);
UnityEngine.Material MakeMaterial(UnityEngine.Material source, string name, UnityEngine.Texture2D body)
{
    string path=folder+"/"+name+".mat";
    var material=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if(material==null) { material=new UnityEngine.Material(source); UnityEditor.AssetDatabase.CreateAsset(material,path); }
    material.SetTexture("_BodyShadingMap",body); material.SetTexture("_CenterShadingMap",center);
    material.SetFloat("_BodyShadeStrength",1f); material.SetFloat("_BodyOcclusionStrength",.9f);
    material.SetFloat("_BodySpecularStrength",.16f);
    material.SetFloat("_RimGlowScale",name=="Tonal Inner"?2.6f:3f);
    material.SetFloat("_OrnamentGlowScale",name=="Tonal Inner"?.30f:.24f);
    UnityEditor.EditorUtility.SetDirty(material); return material;
}
var original=(UnityEngine.Material)data.FindProperty("petalMaterial").objectReferenceValue;
var innerMat=MakeMaterial(original,"Tonal Inner",inner);
var appearances=data.FindProperty("petalAppearances");
var outerMats=new System.Collections.Generic.Dictionary<UnityEngine.Material,UnityEngine.Material>();
for(int i=8;i<appearances.arraySize;i++)
{
    var prop=appearances.GetArrayElementAtIndex(i).FindPropertyRelative("materialOverride");
    var source=(UnityEngine.Material)prop.objectReferenceValue;
    if(!outerMats.TryGetValue(source,out var target))
    { target=MakeMaterial(source,"Tonal Outer "+(outerMats.Count+1),outer); outerMats.Add(source,target); }
    prop.objectReferenceValue=target;
}
data.FindProperty("petalMaterial").objectReferenceValue=innerMat;
data.FindProperty("centerIconGlow").floatValue=.45f;
data.FindProperty("centerDotGlow").floatValue=.30f;
data.FindProperty("centerRingGlow").floatValue=.95f;
data.ApplyModifiedProperties();
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(flower);
UnityEngine.RenderSettings.ambientLight=new UnityEngine.Color(.10f,.107f,.133f,1);
var fill=UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None).Single(l=>l.name=="Overhead Fill - No Shadows");
UnityEditor.Undo.RecordObject(fill,"Reduce rangoli fill lighting"); fill.intensity=.04f;
UnityEditor.EditorUtility.SetDirty(fill);
// Keep the stronger cores crisp enough that their halo does not fill the bowls.
var volume=UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(UnityEngine.FindObjectsSortMode.None).Single(v=>v.sharedProfile!=null&&v.name=="Petal Bloom - Mobile");
string profilePath=folder+"/Tonal Bloom.asset";
var profile=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath);
if(profile==null)
{
    UnityEditor.AssetDatabase.CopyAsset(UnityEditor.AssetDatabase.GetAssetPath(volume.sharedProfile),profilePath);
    profile=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath);
}
if(profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))
{
    bloom.intensity.Override(.55f); bloom.scatter.Override(.38f);
    UnityEditor.EditorUtility.SetDirty(bloom);
}
UnityEditor.Undo.RecordObject(volume,"Tighten tonal rim bloom"); volume.sharedProfile=profile;
UnityEditor.EditorUtility.SetDirty(volume);UnityEditor.EditorUtility.SetDirty(profile);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(flower.gameObject.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(flower.gameObject.scene);
UnityEditor.AssetDatabase.SaveAssets();
return "Broad body gradients, center annular shadow, reduced body sheen, rim multipliers 2.6/3, interior glow multipliers .30/.24; center icon .45, dots .30, ring .95. Bloom .55 intensity/.38 scatter. New scene-specific assets preserve the first comparison.";
