if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var data=new UnityEditor.SerializedObject(flower);
var petals=data.FindProperty("petals");
var surfaces=Enumerable.Range(0,flower.PetalCount).Select(i=>(UnityEngine.Renderer)petals.GetArrayElementAtIndex(i).objectReferenceValue)
    .Concat(new[]{(UnityEngine.Renderer)data.FindProperty("centerDisc").objectReferenceValue}).ToArray();
var camera=UnityEngine.Camera.main;
var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;var oldOrtho=camera.orthographic;var oldSize=camera.orthographicSize;var oldAspect=camera.aspect;
var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;
var motions=flower.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>();var oldMotion=motions.Select(m=>m.enabled).ToArray();
var outer=flower.GetComponentInChildren<Amgen.Deepavali.OuterPetalLayerMotion>();bool oldOuter=outer!=null&&outer.enabled;
var renderer=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
var ao=renderer.rendererFeatures.Single(f=>f.GetType().Name=="ScreenSpaceAmbientOcclusion");bool oldAO=ao.isActive;
var rt=UnityEngine.RenderTexture.GetTemporary(700,700,24,UnityEngine.RenderTextureFormat.ARGBHalf,UnityEngine.RenderTextureReadWrite.Linear);
var image=new UnityEngine.Texture2D(700,700,UnityEngine.TextureFormat.RGBAFloat,false,true);
UnityEngine.Color[] Render()
{
    camera.Render();UnityEngine.RenderTexture.active=rt;
    image.ReadPixels(new UnityEngine.Rect(0,0,700,700),0,0);image.Apply();return image.GetPixels();
}
try
{
    flower.ResetToInitialState();foreach(var motion in motions)motion.enabled=false;if(outer!=null)outer.enabled=false;
    camera.transform.position=new UnityEngine.Vector3(0,1.5f,0);camera.transform.rotation=UnityEngine.Quaternion.Euler(90,180,0);
    camera.orthographic=true;camera.orthographicSize=.48f;camera.aspect=1;camera.targetTexture=rt;
    ao.SetActive(false);var without=Render();ao.SetActive(true);var withAO=Render();
    int changed=0;double totalReduction=0;float maxReduction=0;
    // Sample the inner flower where its own shader, rather than the ground, is visible.
    for(int y=175;y<525;y++)for(int x=175;x<525;x++)
    {
        int i=y*700+x;float d=(without[i].r-withAO[i].r)*.2126f+(without[i].g-withAO[i].g)*.7152f+(without[i].b-withAO[i].b)*.0722f;
        if(d>.0001f)changed++;totalReduction+=d;maxReduction=UnityEngine.Mathf.Max(maxReduction,d);
    }
    var shader=surfaces[0].sharedMaterial.shader;
    var errors=UnityEditor.ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
    var batcherMethod=typeof(UnityEditor.ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
    bool batcher=batcherMethod!=null&&(int)batcherMethod.Invoke(null,new object[]{shader,0})==0;
    bool emissionSame=System.IO.File.ReadAllText("output/flower-tonal-comparison/before-emission.json")==System.IO.File.ReadAllText("output/flower-tonal-comparison/after-emission.json");
    bool masks=surfaces.All(r=>r.sharedMaterial.GetTexture("_BodyShadingMap")!=null&&r.sharedMaterial.GetTexture("_CenterShadingMap")!=null&&r.sharedMaterial.GetFloat("_BodyShadeStrength")>.8f);
    var result=new {passed=errors.Length==0&&batcher&&emissionSame&&masks&&changed>100,surfaces=surfaces.Length,emissionParametersIdentical=emissionSame,allMasksAssigned=masks,shaderErrors=errors,srpBatcherCompatible=batcher,ssaoAffectedFlowerPixels=changed,ssaoMeanLinearLuminanceReduction=totalReduction/(350*350),ssaoMaxLinearLuminanceReduction=maxReduction};
    string json=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);
    System.IO.File.WriteAllText("output/flower-tonal-comparison/validation.json",json);return json;
}
finally
{
    ao.SetActive(oldAO);camera.transform.position=oldPosition;camera.transform.rotation=oldRotation;camera.orthographic=oldOrtho;camera.orthographicSize=oldSize;camera.aspect=oldAspect;camera.targetTexture=oldTarget;
    UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(image);
    for(int i=0;i<motions.Length;i++)motions[i].enabled=oldMotion[i];if(outer!=null)outer.enabled=oldOuter;
}
