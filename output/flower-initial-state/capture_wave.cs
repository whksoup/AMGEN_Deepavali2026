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
    for(int i=8;i<surfaces.Length;i++)surfaces[i].enabled=false;
    camera.orthographicSize=0.375f;
    Save("transition-00.png");
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    var clock=c.GetType().GetField("waveStartedAt",flags);
    var update=(System.Action)System.Delegate.CreateDelegate(typeof(System.Action),c,c.GetType().GetMethod("Update",flags));
    void Step(float fraction){clock.SetValue(c,UnityEngine.Time.timeAsDouble-c.TransitionDuration*fraction);update();}
    c.TransitionTo(Amgen.Deepavali.FlowerPaletteState.Blue);Step(0.45f);
    Save("transition-45.png");Step(1.01f);Save("transition-100.png");
    // Compare linear GPU output outside the radial blend band, without bloom spread.
    var cd=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
    bool oldPost=cd.renderPostProcessing;cd.renderPostProcessing=false;
    UnityEngine.Color[] Read(){camera.Render();UnityEngine.RenderTexture.active=rt;tex.ReadPixels(new UnityEngine.Rect(0,0,1400,1400),0,0);tex.Apply();return tex.GetPixels();}
    var after=Read();c.ResetToInitialState();var before=Read();
    c.TransitionTo(Amgen.Deepavali.FlowerPaletteState.Blue);Step(0.45f);var middle=Read();
    float radius=surfaces[0].sharedMaterial.GetFloat("_WaveRadius");
    float softness=surfaces[0].sharedMaterial.GetFloat("_WaveSoftness");
    float front=UnityEngine.Mathf.Lerp(-softness,radius+softness,c.TransitionProgress);
    int testedBefore=0,testedAfter=0,mismatch=0;
    for(int y=0;y<1400;y++)for(int x=0;x<1400;x++)
    {
        float r=new UnityEngine.Vector2((x+0.5f-700)*0.75f/1400,(y+0.5f-700)*0.75f/1400).magnitude;
        if(r>0.35f||UnityEngine.Mathf.Abs(r-front)<=softness+0.002f)continue;
        int k=y*1400+x;var target=r<front?after[k]:before[k];
        if(r<front)testedAfter++;else testedBefore++;
        if(UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(target.r-middle[k].r),UnityEngine.Mathf.Abs(target.g-middle[k].g),UnityEngine.Mathf.Abs(target.b-middle[k].b))>0.005f)mismatch++;
    }
    cd.renderPostProcessing=oldPost;
    System.IO.File.WriteAllText("output/flower-initial-state/pixel-wave-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{testedBefore,testedAfter,mismatch,front,softness},Newtonsoft.Json.Formatting.Indented));
    c.ResetToInitialState();
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
