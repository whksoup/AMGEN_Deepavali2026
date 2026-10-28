if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Exit play first.");
const string folder = "Assets/HK_Signature/Deepavali/PetalPrototype/Rendering/Spatial Lighting";
var flower = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var data = new UnityEditor.SerializedObject(flower);
var appearances = data.FindProperty("petalAppearances");
UnityEngine.Texture2D BuildMap(int index, string name)
{
    var renderer = flower.GetPetalRenderer(index);
    var mesh = ((UnityEngine.SkinnedMeshRenderer)renderer).sharedMesh;
    var uv = mesh.uv; var tris = mesh.triangles;
    var canonical = new System.Collections.Generic.Dictionary<(int,int),int>();
    var vertices = new System.Collections.Generic.List<UnityEngine.Vector2>();
    var remap = new int[uv.Length];
    for (int i=0;i<uv.Length;i++)
    {
        var key = (UnityEngine.Mathf.RoundToInt(uv[i].x*100000), UnityEngine.Mathf.RoundToInt(uv[i].y*100000));
        if (!canonical.TryGetValue(key, out int v)) { v=vertices.Count; canonical.Add(key,v); vertices.Add(uv[i]); }
        remap[i]=v;
    }
    var edges = new System.Collections.Generic.Dictionary<(int,int),int>();
    for(int i=0;i<tris.Length;i+=3) for(int k=0;k<3;k++)
    {
        int a=remap[tris[i+k]], b=remap[tris[i+(k+1)%3]];
        if(a==b) continue;
        var key=(System.Math.Min(a,b),System.Math.Max(a,b)); edges[key]=edges.TryGetValue(key,out int n)?n+1:1;
    }
    var boundary = edges.Where(e=>e.Value==1).Select(e=>e.Key).ToArray();
    var neighbors = new System.Collections.Generic.Dictionary<int,System.Collections.Generic.List<int>>();
    foreach(var e in boundary)
    {
        foreach(var pair in new[]{(e.Item1,e.Item2),(e.Item2,e.Item1)})
        { if(!neighbors.ContainsKey(pair.Item1)) neighbors[pair.Item1]=new System.Collections.Generic.List<int>(); neighbors[pair.Item1].Add(pair.Item2); }
    }
    if(neighbors.Any(n=>n.Value.Count!=2)) throw new System.Exception("Expected a closed outline for "+name);
    int root=neighbors.Keys.OrderBy(v=>vertices[v].y).First(), tip=neighbors.Keys.OrderByDescending(v=>vertices[v].y).First();
    var size=appearances.GetArrayElementAtIndex(index).FindPropertyRelative("artworkSize").vector2Value;
    var metric=new UnityEngine.Vector2(size.x/size.y,1);
    var segments = new System.Collections.Generic.List<(UnityEngine.Vector2 a,UnityEngine.Vector2 b,float start,float end,int side)>();
    for(int side=0;side<2;side++)
    {
        var chain=new System.Collections.Generic.List<int>{root}; int prev=root, current=neighbors[root][side];
        while(current!=tip)
        {
            chain.Add(current); int next=neighbors[current].First(n=>n!=prev); prev=current; current=next;
            if(chain.Count>vertices.Count) throw new System.Exception("Outline loop cannot reach tip");
        }
        chain.Add(tip); float total=0;
        for(int i=1;i<chain.Count;i++) total+=UnityEngine.Vector2.Scale(vertices[chain[i]]-vertices[chain[i-1]],metric).magnitude;
        float length=0;
        for(int i=1;i<chain.Count;i++)
        {
            var a=UnityEngine.Vector2.Scale(vertices[chain[i-1]],metric); var b=UnityEngine.Vector2.Scale(vertices[chain[i]],metric);
            float next=length+(b-a).magnitude; segments.Add((a,b,length/total,next/total,side)); length=next;
        }
    }
    const int resolution=512; var pixels=new UnityEngine.Color32[resolution*resolution];
    for(int y=0;y<resolution;y++) for(int x=0;x<resolution;x++)
    {
        var p=UnityEngine.Vector2.Scale(new UnityEngine.Vector2((x+.5f)/resolution,(y+.5f)/resolution),metric);
        float distance=float.PositiveInfinity,path=0; int side=0;
        foreach(var e in segments)
        {
            var ab=e.b-e.a; float t=UnityEngine.Mathf.Clamp01(UnityEngine.Vector2.Dot(p-e.a,ab)/ab.sqrMagnitude);
            float d=(p-e.a-ab*t).sqrMagnitude;
            if(d<distance) {distance=d;path=UnityEngine.Mathf.Lerp(e.start,e.end,t);side=e.side;}
        }
        float droot=UnityEngine.Mathf.Sqrt(distance);
        float noise=UnityEngine.Mathf.PerlinNoise(path*9.5f+index*3.7f,side*11.3f+2.4f);
        noise=UnityEngine.Mathf.Clamp01((noise-.25f)*2);
        float arrival=UnityEngine.Mathf.Clamp01(.05f+.82f*path+.13f*UnityEngine.Mathf.Clamp01(droot/.08f));
        pixels[y*resolution+x]=new UnityEngine.Color(UnityEngine.Mathf.Clamp01(droot/.15f),path,noise,arrival);
    }
    var texture=new UnityEngine.Texture2D(resolution,resolution,UnityEngine.TextureFormat.RGBA32,false,true);
    texture.SetPixels32(pixels);texture.Apply(); var pathName=folder+"/"+name+".png";
    System.IO.File.WriteAllBytes(pathName,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
    UnityEditor.AssetDatabase.ImportAsset(pathName,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(pathName);
    importer.sRGBTexture=false;importer.alphaSource=UnityEditor.TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;
    importer.mipmapEnabled=true;importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;importer.filterMode=UnityEngine.FilterMode.Bilinear;
    importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
    return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(pathName);
}
var inner=BuildMap(0,"Inner Light Coordinates");var outer=BuildMap(8,"Outer Light Coordinates");
var preset=UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.PetalLightingPreset>(folder+"/Two Petal Study.asset");
if(preset==null) {preset=UnityEngine.ScriptableObject.CreateInstance<Amgen.Deepavali.PetalLightingPreset>();UnityEditor.AssetDatabase.CreateAsset(preset,folder+"/Two Petal Study.asset");}
preset.innerMap=inner;preset.outerMap=outer;
preset.inner=new Amgen.Deepavali.PetalLightingSettings();preset.outer=new Amgen.Deepavali.PetalLightingSettings{seed=17,gapCoverage=.44f};
UnityEditor.EditorUtility.SetDirty(preset);
var go=UnityEngine.GameObject.Find("Petal Lighting Study")??new UnityEngine.GameObject("Petal Lighting Study");
var lab=go.GetComponent<Amgen.Deepavali.PetalLightingPrototype>()??go.AddComponent<Amgen.Deepavali.PetalLightingPrototype>();
lab.flower=flower;lab.preset=preset;lab.previewCamera=UnityEngine.Camera.main;lab.innerIndex=0;lab.outerIndex=8;lab.focusPair=true;
lab.reference=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder+"/Reference.png");
foreach(var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None)) canvas.gameObject.SetActive(false);
foreach(var pointer in UnityEngine.Object.FindObjectsByType<Amgen.Deepavali.PetalLayerPointerInput>(UnityEngine.FindObjectsSortMode.None)) pointer.enabled=false;
UnityEditor.EditorUtility.SetDirty(lab);
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(go.scene);
UnityEditor.Selection.activeGameObject=go;
return "Saved two-petal prototype scene, 512px linear outline maps, preset and reference panel.";
