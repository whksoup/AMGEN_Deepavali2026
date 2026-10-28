if(UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Edit Mode required.");
var camera=UnityEngine.Camera.main;
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>().transform;
var background=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliBackground>();
var cameraPosition=camera.transform.position;var cameraRotation=camera.transform.rotation;float fov=camera.fieldOfView;
var oldScale=flower.localScale;
var points=new System.Collections.Generic.List<UnityEngine.Vector3>();var baked=new UnityEngine.Mesh();
try
{
    foreach(var skin in flower.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
    {
        skin.BakeMesh(baked,true);
        points.AddRange(baked.vertices.Select(p=>flower.InverseTransformPoint(skin.transform.TransformPoint(p))));
    }
}
finally{UnityEngine.Object.DestroyImmediate(baked);}
UnityEngine.Vector4 Bounds()
{
    var bounds=new UnityEngine.Vector4(1,1,0,0);
    foreach(var p in points)
    {
        var screen=camera.WorldToViewportPoint(flower.TransformPoint(p));
        bounds.x=UnityEngine.Mathf.Min(bounds.x,screen.x);bounds.y=UnityEngine.Mathf.Min(bounds.y,screen.y);
        bounds.z=UnityEngine.Mathf.Max(bounds.z,screen.x);bounds.w=UnityEngine.Mathf.Max(bounds.w,screen.y);
    }
    return bounds;
}
var before=Bounds();
UnityEditor.Undo.RecordObjects(new UnityEngine.Object[]{flower,background},"Widen both rangoli layers");
// This camera has yaw 180 and roll 0: world X is exactly horizontal on screen.
flower.localScale=new UnityEngine.Vector3(oldScale.x*1.30f,oldScale.y,oldScale.z);
background.flowerClearanceStretch=new UnityEngine.Vector2(1.30f,1f);
background.RefreshLayout(camera);
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(flower);
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(background);
UnityEditor.EditorUtility.SetDirty(background);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
var after=Bounds();
var report=new{cameraUnchanged=camera.transform.position==cameraPosition&&camera.transform.rotation==cameraRotation&&camera.fieldOfView==fov,
    cameraPosition=new[]{cameraPosition.x,cameraPosition.y,cameraPosition.z},cameraEuler=new[]{camera.transform.eulerAngles.x,camera.transform.eulerAngles.y,camera.transform.eulerAngles.z},fov,
    beforeScale=new[]{oldScale.x,oldScale.y,oldScale.z},afterScale=new[]{flower.localScale.x,flower.localScale.y,flower.localScale.z},
    widthMultiplier=(after.z-after.x)/(before.z-before.x),beforeHeightPercent=(before.w-before.y)*100,afterHeightPercent=(after.w-after.y)*100,
    beforeWidthPercent=(before.z-before.x)*100,afterWidthPercent=(after.z-after.x)*100,
    afterViewportBounds=new[]{after.x,after.y,after.z,after.w}};
var json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("output/deepavali-wide-flower/width-settings.json",json);return json;
