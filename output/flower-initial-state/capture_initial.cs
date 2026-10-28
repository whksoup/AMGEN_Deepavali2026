var c=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var data=new UnityEditor.SerializedObject(c);
var petals=data.FindProperty("petals");
var surfaces=Enumerable.Range(0,c.PetalCount).Select(i=>(UnityEngine.Renderer)petals.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
var camera=UnityEngine.Camera.main;
var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;var oldOrtho=camera.orthographic;var oldSize=camera.orthographicSize;
var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;
var oldPipeline=UnityEngine.QualitySettings.renderPipeline;
var motion=c.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>();var oldMotion=motion.Select(m=>m.enabled).ToArray();
var outerMotion=c.GetComponentInChildren<Amgen.Deepavali.OuterPetalLayerMotion>();bool oldOuterMotion=outerMotion!=null&&outerMotion.enabled;
var oldEnabled=surfaces.Select(r=>r.enabled).ToArray();
var rt=UnityEngine.RenderTexture.GetTemporary(1400,1400,24,UnityEngine.RenderTextureFormat.ARGBHalf,UnityEngine.RenderTextureReadWrite.Linear);
var tex=new UnityEngine.Texture2D(1400,1400,UnityEngine.TextureFormat.RGBAFloat,false,true);
var png=new UnityEngine.Texture2D(1400,1400,UnityEngine.TextureFormat.RGB24,false);
try
{
    c.ResetToInitialState();
    foreach(var m in motion)m.enabled=false;
    if(outerMotion!=null)outerMotion.enabled=false;
    UnityEngine.QualitySettings.renderPipeline=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
    camera.transform.position=new UnityEngine.Vector3(0,1.5f,0);camera.transform.rotation=UnityEngine.Quaternion.Euler(90,180,0);
    camera.orthographic=true;camera.orthographicSize=0.46f;camera.targetTexture=rt;
    void Save(string name)
    {
        camera.Render();UnityEngine.RenderTexture.active=rt;
        tex.ReadPixels(new UnityEngine.Rect(0,0,1400,1400),0,0);tex.Apply();
        var pixels=tex.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
        png.SetPixels(pixels);png.Apply();
        System.IO.File.WriteAllBytes("output/flower-initial-state/"+name,png.EncodeToPNG());
    }
    Save("initial-flower.png");
    for(int i=8;i<surfaces.Length;i++)surfaces[i].enabled=false;
    camera.orthographicSize=0.375f;
    Save("initial-inner.png");
    return new{shaderErrors=UnityEditor.ShaderUtil.GetShaderMessages(surfaces[0].sharedMaterial.shader).Select(x=>x.message).ToArray(),outerStartupEnabled=surfaces.Skip(8).Select(r=>r.sharedMaterial.GetFloat("_StartupSymbolEnabled")).ToArray(),pipeline=UnityEngine.QualitySettings.renderPipeline.name};
}
finally
{
    for(int i=0;i<surfaces.Length;i++)surfaces[i].enabled=oldEnabled[i];
    camera.transform.position=oldPosition;camera.transform.rotation=oldRotation;camera.orthographic=oldOrtho;camera.orthographicSize=oldSize;camera.targetTexture=oldTarget;
    UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(tex);UnityEngine.Object.Destroy(png);
    UnityEngine.QualitySettings.renderPipeline=oldPipeline;
    for(int i=0;i<motion.Length;i++)motion[i].enabled=oldMotion[i];
    if(outerMotion!=null)outerMotion.enabled=oldOuterMotion;
}
