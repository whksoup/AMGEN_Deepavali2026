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
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>().transform;
var originalScale=flower.localScale;
var scaleChecks=new System.Collections.Generic.List<object>();
Amgen.Deepavali.PetalMotionController selected=null;
System.Action<Amgen.Deepavali.PetalPickTarget> onPick=p=>selected=p.Motion;
input.PetalPressed+=onPick;
try
{
    foreach(float scale in new[]{1f,originalScale.x})
    {
        flower.localScale=UnityEngine.Vector3.one*scale;
        UnityEngine.Physics.SyncTransforms();
        var scalePicked=new System.Collections.Generic.List<bool>();
        foreach(var petal in petals)
        {
            // Independent reference: skin a visible body vertex directly from its bone weights.
            // Do not derive the test point from the same BakeMesh path used by picking.
            var skin=petal.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();var mesh=skin.sharedMesh;
            int index=System.Array.IndexOf(mesh.vertices,mesh.vertices.OrderBy(v=>UnityEngine.Vector3.SqrMagnitude(v-new UnityEngine.Vector3(0,.21f,0))).First());
            var vertex=mesh.vertices[index];var weight=mesh.boneWeights[index];var bind=mesh.bindposes;var bones=skin.bones;
            var world=UnityEngine.Vector3.zero;
            int[] indices={weight.boneIndex0,weight.boneIndex1,weight.boneIndex2,weight.boneIndex3};
            float[] weights={weight.weight0,weight.weight1,weight.weight2,weight.weight3};
            for(int j=0;j<4;j++)world+=(bones[indices[j]].localToWorldMatrix*bind[indices[j]]).MultiplyPoint3x4(vertex)*weights[j];
            var point=camera.WorldToScreenPoint(world);selected=null;
            bool correct=input.TryPulseAtScreenPoint(new UnityEngine.Vector2(point.x,point.y))&&selected==petal;
            picked.Add(correct);scalePicked.Add(correct);
        }
        scaleChecks.Add(new{scale,picked=scalePicked});
    }
}
finally{input.PetalPressed-=onPick;flower.localScale=originalScale;UnityEngine.Physics.SyncTransforms();}
var meshes=layout.GetComponentsInChildren<UnityEngine.MeshFilter>(true).Select(f=>f.sharedMesh).Distinct().ToArray();
var generated=meshes.Where(m=>UnityEditor.AssetDatabase.GetAssetPath(m).StartsWith("Assets/HK_Signature/Deepavali/Background/")).ToArray();
bool finite=generated.All(m=>m.vertexCount>0&&m.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)));
var shaderErrors=new[]{"Amgen/Deepavali/Festival Soil","Amgen/Deepavali/Diya Flame"}.SelectMany(s=>UnityEditor.ShaderUtil.GetShaderMessages(UnityEngine.Shader.Find(s))).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
var sources=layout.lamps.Select(t=>UnityEditor.AssetDatabase.GetAssetPath(t.Find("Oil Lamp - supplied FBX").GetComponent<UnityEngine.MeshFilter>().sharedMesh)).ToArray();
var report=new{
    passed=cornersValid&&clearanceValid&&picked.Count==16&&picked.All(p=>p)&&finite&&shaderErrors.Length==0
        &&sources.All(p=>p=="Assets/DiwaliAssets/cgtrader_optimized_Oil+Lamp.fbx"),
    lamps=layout.lamps.Length,layouts,scaleChecks,generatedMeshesFinite=finite,
    backgroundColliders=layout.GetComponentsInChildren<UnityEngine.Collider>(true).Length,
    modelSources=sources,
    soilAlbedo=UnityEditor.AssetDatabase.GetAssetPath(layout.soil.sharedMaterial.GetTexture("_BaseMap")),
    shaderErrors
};
string json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("output/deepavali-camera-framing/interaction-validation.json",json);
return json;

