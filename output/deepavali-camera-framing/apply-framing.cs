// Edit Mode: set the oblique camera first, then fit the actual rangoli vertices vertically.
if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Edit Mode required.");
var camera=UnityEngine.Camera.main;
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>().transform;
var background=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliBackground>();
var vertices=new System.Collections.Generic.List<UnityEngine.Vector3>();
var baked=new UnityEngine.Mesh();
try
{
    foreach(var skin in flower.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
    {
        skin.BakeMesh(baked, true);
        vertices.AddRange(baked.vertices.Select(p=>flower.InverseTransformPoint(skin.transform.TransformPoint(p))));
    }
    foreach(var filter in flower.GetComponentsInChildren<UnityEngine.MeshFilter>())
        vertices.AddRange(filter.sharedMesh.vertices.Select(p=>flower.InverseTransformPoint(filter.transform.TransformPoint(p))));
}
finally{UnityEngine.Object.DestroyImmediate(baked);}
UnityEngine.Vector4 ScreenBounds()
{
    float minX=1,minY=1,maxX=0,maxY=0;
    foreach(var p in vertices)
    {
        var q=camera.WorldToViewportPoint(flower.TransformPoint(p));
        minX=UnityEngine.Mathf.Min(minX,q.x);maxX=UnityEngine.Mathf.Max(maxX,q.x);
        minY=UnityEngine.Mathf.Min(minY,q.y);maxY=UnityEngine.Mathf.Max(maxY,q.y);
    }
    return new UnityEngine.Vector4(minX,minY,maxX,maxY);
}
var before=ScreenBounds();
UnityEditor.Undo.RecordObjects(new UnityEngine.Object[]{camera.transform,flower,background},"Angle camera and enlarge rangoli");
camera.transform.rotation=UnityEngine.Quaternion.Euler(50,180,0);
var orbit=-camera.transform.forward*1.85f;
camera.transform.position=orbit;
float centerZ=0;
// Perspective shifts the visible centre towards the near edge as the flower gets larger.
// Solve both scale and table-plane aim offset, keeping all petals inside 5% top/bottom margins.
for(int iteration=0;iteration<16;iteration++)
{
    var bounds=ScreenBounds();
    float coverage=bounds.w-bounds.y;
    flower.localScale*=0.90f/coverage;
    bounds=ScreenBounds();
    float center=(bounds.y+bounds.w)*0.5f;
    camera.transform.position=orbit+new UnityEngine.Vector3(0,0,centerZ+0.001f);
    var shifted=ScreenBounds();
    float derivative=((shifted.y+shifted.w)*0.5f-center)/0.001f;
    if(UnityEngine.Mathf.Abs(derivative)>0.0001f)centerZ+=(0.5f-center)/derivative;
    camera.transform.position=orbit+new UnityEngine.Vector3(0,0,centerZ);
}
background.flowerClearance=0.52f*flower.localScale.x;
background.RefreshLayout(camera);
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(flower);
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(background);
UnityEditor.EditorUtility.SetDirty(background);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
var after=ScreenBounds();
var report=new{beforeHeightPercent=(before.w-before.y)*100,afterHeightPercent=(after.w-after.y)*100,
    cameraPitchDegrees=camera.transform.eulerAngles.x,tiltFromOverhead=40,
    cameraPosition=new[]{camera.transform.position.x,camera.transform.position.y,camera.transform.position.z},
    flowerScale=flower.localScale.x,viewportBounds=new[]{after.x,after.y,after.z,after.w},background.flowerClearance,
    lowestFlowerVertex=vertices.Min(p=>flower.TransformPoint(p).y),floorHeight=background.floorHeight};
string json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("output/deepavali-camera-framing/framing.json",json);
return json;
