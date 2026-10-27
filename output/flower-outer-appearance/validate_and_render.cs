// Run through Unity MCP in Play Mode. Saves camera renders and a repeatable integration report.
if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Enter Play Mode first.");
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var preview=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerAppearancePreview>();
var slider=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Slider>(UnityEngine.FindObjectsSortMode.None).Single(s=>s.name=="Glow");
var data=new UnityEditor.SerializedObject(flower);var p=data.FindProperty("petals");var a=data.FindProperty("petalAppearances");
var renderers=Enumerable.Range(0,flower.PetalCount).Select(i=>(UnityEngine.Renderer)p.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
var sets=Enumerable.Range(0,flower.PetalCount).Select(i=>(Amgen.Deepavali.FlowerPaletteSet)a.GetArrayElementAtIndex(i).FindPropertyRelative("paletteOverride").objectReferenceValue??flower.PaletteSet).ToArray();
var states=Enumerable.Range(0,flower.PetalCount).Select(flower.GetRequestedPetalState).ToArray();
var symbols=Enumerable.Range(0,flower.PetalCount).Select(flower.GetSymbol).ToArray();
var emission=Enumerable.Range(0,flower.PetalCount).Select(flower.GetPetalEmissionScale).ToArray();
var selected=flower.SelectedState;var sliderValue=slider.value;
var materialIds=renderers.Select(r=>r.sharedMaterial.GetInstanceID()).ToArray();
var maskIds=renderers.Select(r=>r.sharedMaterial.GetTexture("_BaseMap").GetInstanceID()).ToArray();
string output=System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,"output/flower-outer-appearance");
var camera=UnityEngine.Camera.main;var originalTarget=camera.targetTexture;float originalAspect=camera.aspect;
var active=UnityEngine.RenderTexture.active;
var rt=UnityEngine.RenderTexture.GetTemporary(1100,1100,24,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.sRGB);
var texture=new UnityEngine.Texture2D(1100,1100,UnityEngine.TextureFormat.RGB24,false);
var hashes=new System.Collections.Generic.List<string>();var failures=new System.Collections.Generic.List<string>();
float maxColorError=0;
void Check(bool condition,string label){if(!condition)failures.Add(label);}
float Difference(UnityEngine.Color x,UnityEngine.Color y)=>UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(x.r-y.r),UnityEngine.Mathf.Abs(x.g-y.g),UnityEngine.Mathf.Abs(x.b-y.b));
UnityEngine.Color32[] Capture(string name)
{
    camera.Render();UnityEngine.RenderTexture.active=rt;
    texture.ReadPixels(new UnityEngine.Rect(0,0,1100,1100),0,0);texture.Apply();
    var png=texture.EncodeToPNG();System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,name+".png"),png);
    using(var sha=System.Security.Cryptography.SHA256.Create())hashes.Add(System.Convert.ToBase64String(sha.ComputeHash(png)));
    return texture.GetPixels32();
}
try
{
    Check(flower.PetalCount==23,"Expected 8 inner and 15 outer petals");
    for(int i=8;i<23;i++)Check(sets[i].name=="Deepavali Outer Tone "+((i-8)%2+1),"Alternating tone "+i);
    camera.targetTexture=rt;camera.aspect=1;
    for(int state=0;state<7;state++)
    {
        Check(flower.SetState((Amgen.Deepavali.FlowerPaletteState)state),"State accepted "+state);
        for(int i=0;i<23;i++)
        {
            sets[i].TryGet((Amgen.Deepavali.FlowerPaletteState)state,out var palette);var mat=renderers[i].sharedMaterial;
            maxColorError=UnityEngine.Mathf.Max(maxColorError,Difference(mat.GetColor("_BaseColor"),palette.fill.linear),Difference(mat.GetColor("_TargetBaseColor"),palette.fill.linear));
            Check(mat.GetFloat("_MainPatternIntensity")==palette.mainPatternIntensity,"Main glow binding "+i);
            Check(mat.GetFloat("_AccentPatternIntensity")==palette.accentPatternIntensity,"Accent glow binding "+i);
            Check(mat.GetInstanceID()==materialIds[i]&&mat.GetTexture("_BaseMap").GetInstanceID()==maskIds[i],"Material/mask ownership "+i);
            Check(flower.GetPetalState(i)==(Amgen.Deepavali.FlowerPaletteState)state,"Palette state "+i);
        }
        Check(Difference(renderers[8].sharedMaterial.GetColor("_BaseColor"),renderers[9].sharedMaterial.GetColor("_BaseColor"))>.01f,"Distinct tone shades "+state);
        Capture(state.ToString("00")+"-"+((Amgen.Deepavali.FlowerPaletteState)state).ToString().ToLowerInvariant());
    }
    int distinctStates=hashes.Distinct().Count();Check(distinctStates==7,"Distinct rendered states");
    preview.SelectAll();
    foreach(float value in new[]{0f,2.25f,1f})
    {
        slider.value=value;
        for(int i=0;i<23;i++)Check(UnityEngine.Mathf.Abs(renderers[i].sharedMaterial.GetFloat("_EmissionScale")-value)<.00001f,"Shared glow slider "+i);
    }
    flower.SetState(Amgen.Deepavali.FlowerPaletteState.Gold);
    foreach(var r in renderers.Skip(8))foreach(string n in new[]{"_EdgeIntensity","_TargetEdgeIntensity","_MainPatternIntensity","_TargetMainPatternIntensity","_AccentPatternIntensity","_TargetAccentPatternIntensity"})r.sharedMaterial.SetFloat(n,0);
    var baseline=Capture("glow-00-off");
    foreach(var r in renderers.Skip(8)){r.sharedMaterial.SetFloat("_MainPatternIntensity",2);r.sharedMaterial.SetFloat("_TargetMainPatternIntensity",2);}
    var main=Capture("glow-01-main");
    foreach(var r in renderers.Skip(8)){r.sharedMaterial.SetFloat("_MainPatternIntensity",0);r.sharedMaterial.SetFloat("_TargetMainPatternIntensity",0);r.sharedMaterial.SetFloat("_AccentPatternIntensity",2);r.sharedMaterial.SetFloat("_TargetAccentPatternIntensity",2);}
    var accent=Capture("glow-02-accent");
    int mainPixels=0,accentPixels=0,distinctGlowPixels=0;
    for(int i=0;i<baseline.Length;i++)
    {
        int m=System.Math.Abs(main[i].r-baseline[i].r)+System.Math.Abs(main[i].g-baseline[i].g)+System.Math.Abs(main[i].b-baseline[i].b);
        int b=System.Math.Abs(accent[i].r-baseline[i].r)+System.Math.Abs(accent[i].g-baseline[i].g)+System.Math.Abs(accent[i].b-baseline[i].b);
        if(m>10)mainPixels++;if(b>10)accentPixels++;if((m>10)!=(b>10))distinctGlowPixels++;
    }
    Check(mainPixels>1000&&accentPixels>1000&&distinctGlowPixels>1000,"Independent glow region render differences");
    Check(maxColorError<.000001f,"Palette color accuracy");
    var shader=renderers[8].sharedMaterial.shader;
    Check(!UnityEditor.ShaderUtil.ShaderHasError(shader),"Shader compile");
    var method=typeof(UnityEditor.ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
    bool batcher=method!=null&&(int)method.Invoke(null,new object[]{shader,0})==0;Check(batcher,"SRP Batcher");
    var report=new{passed=failures.Count==0,petals=23,outer=15,tone1=8,tone2=7,states=7,distinctStates,maxColorError,sharedSlider=true,mainGlowChangedPixels=mainPixels,accentGlowChangedPixels=accentPixels,distinctGlowPixels,batcher,shaderMessages=UnityEditor.ShaderUtil.GetShaderMessages(shader),failures};
    System.IO.File.WriteAllText(System.IO.Path.Combine(output,"render-validation.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
    return report;
}
finally
{
    flower.SetState(selected);
    for(int i=0;i<states.Length;i++){flower.SetPetalState(i,states[i]);flower.SetPetalEmissionScale(i,emission[i]);flower.SetSymbol(i,symbols[i]);}
    slider.SetValueWithoutNotify(sliderValue);camera.targetTexture=originalTarget;camera.aspect=originalAspect;
    UnityEngine.RenderTexture.active=active;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(texture);
}
