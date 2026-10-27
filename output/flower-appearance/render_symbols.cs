if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required.");
var c=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var camera=UnityEngine.Camera.main;
var motions=c.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>();
var oldEnabled=motions.Select(m=>m.enabled).ToArray();
var pipeline=UnityEngine.QualitySettings.renderPipeline;
var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;var oldPos=camera.transform.position;
var rt=UnityEngine.RenderTexture.GetTemporary(800,800,24,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.sRGB);
var tex=new UnityEngine.Texture2D(800,800,UnityEngine.TextureFormat.RGB24,false);
try
{
    UnityEngine.QualitySettings.renderPipeline=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
    foreach(var m in motions)m.enabled=false;
    camera.transform.position=new UnityEngine.Vector3(0,1.4f,0);camera.targetTexture=rt;
    for(int i=0;i<c.PetalCount;i++) {c.SetPetalState(i,(Amgen.Deepavali.FlowerPaletteState)(i%7));c.SetSymbol(i,i%4);}
    camera.Render();UnityEngine.RenderTexture.active=rt;tex.ReadPixels(new UnityEngine.Rect(0,0,800,800),0,0);tex.Apply();
    System.IO.File.WriteAllBytes("output/flower-appearance/03-mixed-symbols.png",tex.EncodeToPNG());
    var shader=UnityEngine.Shader.Find("Amgen/Deepavali/Petal Palette Lit");
    var method=typeof(UnityEditor.ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
    return new {symbols=c.SymbolSet.Count,atlas=c.SymbolSet.Atlas.name,srpCode=method.Invoke(null,new object[]{shader,0}),shaderErrors=UnityEditor.ShaderUtil.GetShaderMessages(shader).Select(m=>m.message).ToArray()};
}
finally
{
    c.SetState(Amgen.Deepavali.FlowerPaletteState.Base);for(int i=0;i<c.PetalCount;i++)c.SetSymbol(i,-1);
    for(int i=0;i<motions.Length;i++)motions[i].enabled=oldEnabled[i];
    camera.targetTexture=oldTarget;camera.transform.position=oldPos;UnityEngine.RenderTexture.active=oldActive;
    UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(tex);UnityEngine.QualitySettings.renderPipeline=pipeline;
}
