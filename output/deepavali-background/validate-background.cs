if(!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var layout=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliBackground>();
var camera=UnityEngine.Camera.main;
float oldAspect=camera.aspect;
var layouts=new System.Collections.Generic.List<object>();
bool cornersValid=true,clearanceValid=true;
try
{
    foreach(float aspect in new[]{16f/9f,2560f/1118f})
    {
        camera.aspect=aspect;layout.RefreshLayout(camera);
        var lampPoints=layout.lamps.Select(t=>camera.WorldToViewportPoint(t.position)).ToArray();
        bool valid=lampPoints.Length==4 && lampPoints.All(p=>p.x>0.04f&&p.x<0.96f&&p.y>0.06f&&p.y<0.94f);
        bool clear=layout.border.Where(p=>p.transform.gameObject.activeInHierarchy).All(p=>new UnityEngine.Vector2(p.transform.position.x,p.transform.position.z).magnitude>layout.flowerClearance+p.clearance-0.001f);
        cornersValid&=valid;clearanceValid&=clear;
        layouts.Add(new{aspect,valid,clear,visibleBorderPieces=layout.border.Count(p=>p.transform.gameObject.activeInHierarchy),lamps=lampPoints.Select(p=>new[]{p.x,p.y}).ToArray()});
    }
}
finally{camera.aspect=oldAspect;layout.RefreshLayout(camera);}
var input=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalLayerPointerInput>();
var petals=UnityEngine.Object.FindObjectsByType<Amgen.Deepavali.PetalMotionController>(UnityEngine.FindObjectsSortMode.None).OrderBy(p=>p.name).ToArray();
var picked=new System.Collections.Generic.List<bool>();
var baked=new UnityEngine.Mesh();
Amgen.Deepavali.PetalMotionController selected=null;
System.Action<Amgen.Deepavali.PetalPickTarget> onPick=p=>selected=p.Motion;
input.PetalPressed+=onPick;
try
{
    foreach(var petal in petals)
    {
        var skin=petal.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();skin.BakeMesh(baked);
        int index=System.Array.IndexOf(skin.sharedMesh.vertices,skin.sharedMesh.vertices.OrderBy(v=>UnityEngine.Vector3.SqrMagnitude(v-new UnityEngine.Vector3(0,.21f,0))).First());
        var point=camera.WorldToScreenPoint(skin.transform.TransformPoint(baked.vertices[index]));selected=null;
        picked.Add(input.TryPulseAtScreenPoint(new UnityEngine.Vector2(point.x,point.y))&&selected==petal);
    }
}
finally{input.PetalPressed-=onPick;UnityEngine.Object.Destroy(baked);}
var meshes=layout.GetComponentsInChildren<UnityEngine.MeshFilter>(true).Select(f=>f.sharedMesh).Distinct().ToArray();
var generated=meshes.Where(m=>UnityEditor.AssetDatabase.GetAssetPath(m).StartsWith("Assets/HK_Signature/Deepavali/Background/")).ToArray();
bool finite=generated.All(m=>m.vertexCount>0&&m.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)));
var shaderErrors=new[]{"Amgen/Deepavali/Festival Soil","Amgen/Deepavali/Diya Flame"}.SelectMany(s=>UnityEditor.ShaderUtil.GetShaderMessages(UnityEngine.Shader.Find(s))).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
var sources=layout.lamps.Select(t=>UnityEditor.AssetDatabase.GetAssetPath(t.Find("Oil Lamp - supplied FBX").GetComponent<UnityEngine.MeshFilter>().sharedMesh)).ToArray();
var report=new{
    passed=cornersValid&&clearanceValid&&picked.Count==8&&picked.All(p=>p)&&finite&&shaderErrors.Length==0
        &&sources.All(p=>p=="Assets/DiwaliAssets/cgtrader_optimized_Oil+Lamp.fbx"),
    lamps=layout.lamps.Length,layouts,picked,generatedMeshesFinite=finite,
    backgroundColliders=layout.GetComponentsInChildren<UnityEngine.Collider>(true).Length,
    modelSources=sources,
    soilAlbedo=UnityEditor.AssetDatabase.GetAssetPath(layout.soil.sharedMaterial.GetTexture("_BaseMap")),
    shaderErrors
};
string json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("output/deepavali-background/validation.json",json);
return json;
