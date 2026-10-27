// Render every palette with identical pose/lighting and validate state ownership.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Enter Play Mode first.");
var controller=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var skins=controller.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
var motions=controller.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>();
var camera=UnityEngine.Camera.main;
var disc=UnityEngine.GameObject.Find("Center Disc").GetComponent<UnityEngine.Renderer>();
var originalCameraPosition=camera.transform.position;
var originalPipeline=UnityEngine.QualitySettings.renderPipeline;
var originalTarget=camera.targetTexture;
var originalActive=UnityEngine.RenderTexture.active;
var originalStates=motions.Select(m=>m.enabled).ToArray();
var originalPalette=controller.SelectedState;
var output=System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,"output/flower-palettes");
var rt=UnityEngine.RenderTexture.GetTemporary(800,800,24,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.sRGB);
var texture=new UnityEngine.Texture2D(800,800,UnityEngine.TextureFormat.RGB24,false);
var errors=new System.Collections.Generic.List<string>();
UnityEngine.Application.LogCallback logger=(m,t,k)=>{if(k==UnityEngine.LogType.Error||k==UnityEngine.LogType.Exception||k==UnityEngine.LogType.Assert) errors.Add(m);};
UnityEngine.Application.logMessageReceived+=logger;
float maxColorError=0;
bool materialStable=true,textureStable=true,geometryStable=true;
int materialId=skins[0].sharedMaterial.GetInstanceID();
var mask=skins[0].sharedMaterial.GetTexture("_BaseMap");
var meshIds=skins.Select(s=>s.sharedMesh.GetInstanceID()).ToArray();
var hashes=new System.Collections.Generic.List<string>();
try
{
    UnityEngine.QualitySettings.renderPipeline=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
    foreach(var motion in motions) motion.enabled=false;
    camera.transform.position=new UnityEngine.Vector3(0f,1.4f,0f);
    camera.targetTexture=rt;
    for(int i=0;i<7;i++)
    {
        var state=(Amgen.Deepavali.FlowerPaletteState)i;
        if(!controller.SetState(state)||!controller.PaletteSet.TryGet(state,out var palette)) throw new System.InvalidOperationException("Missing state: "+state);
        var properties=new[]{"_BaseColor","_PatternColor","_AccentColor","_EmissionColor"};
        var values=new[]{palette.fill.linear,palette.ornament.linear,palette.accent.linear,palette.edge.linear*palette.edgeIntensity};
        foreach(var skin in skins)
        {
            materialStable &= skin.sharedMaterial.GetInstanceID()==materialId;
            textureStable &= skin.sharedMaterial.GetTexture("_BaseMap")==mask;
            for(int p=0;p<properties.Length;p++)
            {
                var delta=skin.sharedMaterial.GetColor(properties[p])-values[p];
                maxColorError=UnityEngine.Mathf.Max(maxColorError,UnityEngine.Mathf.Abs(delta.r),UnityEngine.Mathf.Abs(delta.g),UnityEngine.Mathf.Abs(delta.b));
            }
        }
        var centerDelta=disc.sharedMaterial.GetColor("_BaseColor")-palette.center.linear;
        maxColorError=UnityEngine.Mathf.Max(maxColorError,UnityEngine.Mathf.Abs(centerDelta.r),UnityEngine.Mathf.Abs(centerDelta.g),UnityEngine.Mathf.Abs(centerDelta.b));
        camera.Render(); UnityEngine.RenderTexture.active=rt;
        texture.ReadPixels(new UnityEngine.Rect(0,0,800,800),0,0); texture.Apply();
        var png=texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,i.ToString("00")+"-"+state.ToString().ToLowerInvariant()+".png"),png);
        using(var sha=System.Security.Cryptography.SHA256.Create()) hashes.Add(System.Convert.ToBase64String(sha.ComputeHash(png)));
    }
    for(int i=0;i<skins.Length;i++) geometryStable &= skins[i].sharedMesh.GetInstanceID()==meshIds[i];
    var beforeInvalid=controller.SelectedState;
    bool invalidRejected=!controller.SetState((Amgen.Deepavali.FlowerPaletteState)(-1))&&!controller.SetState((Amgen.Deepavali.FlowerPaletteState)7)&&controller.SelectedState==beforeInvalid;
    var shader=skins[0].sharedMaterial.shader;
    var messages=UnityEditor.ShaderUtil.GetShaderMessages(shader);
    var method=typeof(UnityEditor.ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
    bool batcher=method!=null&&(int)method.Invoke(null,new object[]{shader,0})==0;
    bool pass=maxColorError<0.000001f&&materialStable&&textureStable&&geometryStable&&hashes.Distinct().Count()==7&&invalidRejected&&batcher&&errors.Count==0&&!UnityEditor.ShaderUtil.ShaderHasError(shader);
    string report="{\"passed\":"+pass.ToString().ToLower()+",\"states\":7,\"distinctRenderedStates\":"+hashes.Distinct().Count()+",\"maxColorError\":"+maxColorError.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+
        ",\"oneStablePetalMaterial\":"+materialStable.ToString().ToLower()+",\"sharedTextureUnchanged\":"+textureStable.ToString().ToLower()+",\"geometryUnchanged\":"+geometryStable.ToString().ToLower()+
        ",\"invalidStateRejected\":"+invalidRejected.ToString().ToLower()+",\"srpBatcherCompatible\":"+batcher.ToString().ToLower()+",\"runtimeErrors\":"+errors.Count+",\"shaderMessages\":"+messages.Length+"}";
    System.IO.File.WriteAllText(System.IO.Path.Combine(output,"render-validation.json"),report);
    return report;
}
finally
{
    UnityEngine.Application.logMessageReceived-=logger;
    camera.targetTexture=originalTarget; camera.transform.position=originalCameraPosition;
    UnityEngine.RenderTexture.active=originalActive;
    UnityEngine.RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.Destroy(texture);
    UnityEngine.QualitySettings.renderPipeline=originalPipeline;
    for(int i=0;i<motions.Length;i++) motions[i].enabled=originalStates[i];
    controller.SetState(originalPalette);
}
