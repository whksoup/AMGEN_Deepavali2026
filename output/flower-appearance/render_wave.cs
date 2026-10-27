var c=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
if(!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var camera=UnityEngine.Camera.main;
var motions=c.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>();
var oldEnabled=motions.Select(m=>m.enabled).ToArray();
var pipeline=UnityEngine.QualitySettings.renderPipeline;
var cameraPosition=camera.transform.position;
var oldTarget=camera.targetTexture;
var oldActive=UnityEngine.RenderTexture.active;
var rt=UnityEngine.RenderTexture.GetTemporary(800,800,24,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.sRGB);
var tex=new UnityEngine.Texture2D(800,800,UnityEngine.TextureFormat.RGB24,false);
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var clock=c.GetType().GetField("waveStartedAt",flags);
var update=c.GetType().GetMethod("Update",flags);
try
{
    UnityEngine.QualitySettings.renderPipeline=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
    foreach(var m in motions)m.enabled=false;
    camera.transform.position=new UnityEngine.Vector3(0,1.4f,0);
    camera.targetTexture=rt;
    void Capture(string name){camera.Render();UnityEngine.RenderTexture.active=rt;tex.ReadPixels(new UnityEngine.Rect(0,0,800,800),0,0);tex.Apply();System.IO.File.WriteAllBytes("output/flower-appearance/"+name+".png",tex.EncodeToPNG());}
    c.SetState(Amgen.Deepavali.FlowerPaletteState.Base);
    for(int i=0;i<c.PetalCount;i++) c.SetPetalState(i,(Amgen.Deepavali.FlowerPaletteState)(i%7));
    Capture("01-mixed-palettes");
    c.SetState(Amgen.Deepavali.FlowerPaletteState.Base);
    c.TransitionTo(Amgen.Deepavali.FlowerPaletteState.Blue);
    for(int i=0;i<=4;i++)
    {
        clock.SetValue(c,UnityEngine.Time.timeAsDouble-c.TransitionDuration*i*0.25);
        update.Invoke(c,null);
        Capture("02-wave-"+(i*25).ToString("000"));
    }
    var shader=UnityEngine.Shader.Find("Amgen/Deepavali/Petal Palette Lit");
    var check=typeof(UnityEditor.ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
    return new {srpCode=check.Invoke(null,new object[]{shader,0}),shaderMessages=UnityEditor.ShaderUtil.GetShaderMessages(shader).Select(m=>m.message).ToArray(),methods=typeof(UnityEditor.ShaderUtil).GetMethods(System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Where(m=>m.Name.Contains("SRPBatcher")).Select(m=>m.ToString()).ToArray()};
}
finally
{
    c.SetState(Amgen.Deepavali.FlowerPaletteState.Base);
    for(int i=0;i<motions.Length;i++)motions[i].enabled=oldEnabled[i];
    camera.targetTexture=oldTarget;camera.transform.position=cameraPosition;
    UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(tex);
    UnityEngine.QualitySettings.renderPipeline=pipeline;
}
