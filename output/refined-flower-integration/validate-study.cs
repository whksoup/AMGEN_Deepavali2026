if(!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required");
var lab=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalLightingPrototype>();
var camera=lab.previewCamera;var flower=lab.flower;
var originalRect=camera.rect;var originalAspect=camera.aspect;var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;
bool oldAnimation=lab.animateShimmer,oldLoop=lab.loopGrowth,oldEnabled=lab.effectEnabled;
float oldClock=lab.Clock,oldInner=lab.Inner.progress,oldOuter=lab.Outer.progress;
var motion=flower.GetComponentsInChildren<UnityEngine.MonoBehaviour>().Where(m=>m is Amgen.Deepavali.PetalMotionController || m is Amgen.Deepavali.OuterPetalLayerMotion).ToArray();
var motionStates=motion.Select(m=>m.enabled).ToArray();
var originalInner=UnityEngine.JsonUtility.ToJson(lab.Inner);var originalOuter=UnityEngine.JsonUtility.ToJson(lab.Outer);
const int width=1100,height=900;
var rt=UnityEngine.RenderTexture.GetTemporary(width,height,24,UnityEngine.RenderTextureFormat.ARGBHalf,UnityEngine.RenderTextureReadWrite.Linear);
var read=new UnityEngine.Texture2D(width,height,UnityEngine.TextureFormat.RGBAFloat,false,true);
var png=new UnityEngine.Texture2D(width,height,UnityEngine.TextureFormat.RGB24,false);
var checks=new System.Collections.Generic.Dictionary<string,bool>();
var stats=new System.Collections.Generic.List<object>();
UnityEngine.Color[] Capture(string name,bool save=true)
{
    lab.ApplyNow();camera.Render();UnityEngine.RenderTexture.active=rt;
    read.ReadPixels(new UnityEngine.Rect(0,0,width,height),0,0);read.Apply();var values=read.GetPixels();
    if(save){png.SetPixels(values.Select(c=>c.gamma).ToArray());png.Apply();System.IO.File.WriteAllBytes("output/refined-flower-integration/study-"+name+".png",png.EncodeToPNG());}
    stats.Add(new{name,luminance=values.Average(c=>(double)(c.r*.2126f+c.g*.7152f+c.b*.0722f))});
    return values;
}
double Difference(UnityEngine.Color[] a,UnityEngine.Color[] b) => a.Zip(b,(x,y)=>(double)(UnityEngine.Mathf.Abs(x.r-y.r)+UnityEngine.Mathf.Abs(x.g-y.g)+UnityEngine.Mathf.Abs(x.b-y.b))).Average();
try
{
    lab.animateShimmer=false;lab.loopGrowth=false;lab.Clock=3;
    foreach(var m in motion)m.enabled=false;
    camera.rect=new UnityEngine.Rect(0,0,1,1);camera.aspect=width/(float)height;camera.targetTexture=rt;
    var otherBefore=Enumerable.Range(0,flower.PetalCount).Where(i=>i!=0&&i!=8).ToDictionary(i=>i,i=>UnityEditor.EditorJsonUtility.ToJson(flower.GetPetalRenderer(i).sharedMaterial));
    lab.effectEnabled=false;var baseline=Capture("01-original");
    lab.effectEnabled=true;lab.Inner.progress=lab.Outer.progress=0;var zero=Capture("02-growth-000");
    lab.Inner.progress=lab.Outer.progress=.35f;var early=Capture("03-growth-035");
    lab.Inner.progress=lab.Outer.progress=.65f;var middle=Capture("04-growth-065");
    lab.Inner.progress=lab.Outer.progress=1;var full=Capture("05-growth-100");
    checks["growth_changes_image"]=Difference(zero,full)>.001;
    checks["growth_has_intermediate_stages"]=Difference(early,middle)>.0001;
    lab.Clock=7;var shimmer=Capture("06-shimmer-later");
    checks["shimmer_changes_image"]=Difference(full,shimmer)>.0001;
    lab.Clock=3;lab.Inner.tipBrightness=lab.Outer.tipBrightness=0;var noTip=Capture("07-no-tip");
    checks["tip_control_changes_image"]=Difference(full,noTip)>.0001;
    UnityEngine.JsonUtility.FromJsonOverwrite(originalInner,lab.Inner);UnityEngine.JsonUtility.FromJsonOverwrite(originalOuter,lab.Outer);
    lab.Inner.gapDepth=lab.Outer.gapDepth=0;var noGaps=Capture("08-no-dark-gaps");
    checks["dark_gaps_change_image"]=Difference(full,noGaps)>.0001;
    lab.effectEnabled=false;var baselineAgain=Capture("09-original-restored");
    checks["AB_restores_original_pixels"]=Difference(baseline,baselineAgain)<.000001;
    checks["other_20_materials_unchanged"]=otherBefore.All(p=>UnityEditor.EditorJsonUtility.ToJson(flower.GetPetalRenderer(p.Key).sharedMaterial)==p.Value);
    lab.effectEnabled=true;lab.ApplyNow();
    checks["exactly_two_enabled"]=Enumerable.Range(0,flower.PetalCount).Where(i=>flower.GetPetalRenderer(i).sharedMaterial.GetFloat("_PetalLightEnabled")>.5f).SequenceEqual(new[]{0,8});
    lab.enabled=false;
    checks["disable_cleans_up_shader"]=new[]{0,8}.All(i=>flower.GetPetalRenderer(i).sharedMaterial.GetFloat("_PetalLightEnabled")==0);
    lab.enabled=true;
    var errors=UnityEditor.ShaderUtil.GetShaderMessages(UnityEngine.Shader.Find("Amgen/Deepavali/Petal Palette Lit")).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
    checks["shader_compiles"]=errors.Length==0;
    checks["maps_linear_uncompressed"]=new[]{lab.preset.innerMap,lab.preset.outerMap}.All(t=>{var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(t));return !importer.sRGBTexture&&importer.textureCompression==UnityEditor.TextureImporterCompression.Uncompressed&&!importer.alphaIsTransparency;});
    var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=checks.All(c=>c.Value),checks,stats,errors},Newtonsoft.Json.Formatting.Indented);
    System.IO.File.WriteAllText("output/refined-flower-integration/study-validation.json",report);return report;
}
finally
{
    UnityEngine.JsonUtility.FromJsonOverwrite(originalInner,lab.Inner);UnityEngine.JsonUtility.FromJsonOverwrite(originalOuter,lab.Outer);
    lab.animateShimmer=oldAnimation;lab.loopGrowth=oldLoop;lab.effectEnabled=oldEnabled;lab.Clock=oldClock;lab.Inner.progress=oldInner;lab.Outer.progress=oldOuter;lab.ApplyNow();
    camera.rect=originalRect;camera.aspect=originalAspect;camera.targetTexture=oldTarget;
    UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(read);UnityEngine.Object.Destroy(png);
    for(int i=0;i<motion.Length;i++)motion[i].enabled=motionStates[i];
}

