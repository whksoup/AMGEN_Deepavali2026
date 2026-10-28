if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Run in a fresh Play Mode session.");
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var data=new UnityEditor.SerializedObject(flower);var petals=data.FindProperty("petals");var appearances=data.FindProperty("petalAppearances");
var surfaces=Enumerable.Range(0,23).Select(i=>(UnityEngine.Renderer)petals.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
var failures=new System.Collections.Generic.List<string>();
void Check(bool value,string label){if(!value)failures.Add(label);}
float Delta(UnityEngine.Color a,UnityEngine.Color b)=>UnityEngine.Vector4.Distance(a,b);
for(int i=0;i<23;i++)
{
    Check(flower.IsUsingStartupPalette(i)==(i>=8),"Startup membership "+i);
    if(i<8)continue;
    var startup=(Amgen.Deepavali.FlowerStartupPalette)appearances.GetArrayElementAtIndex(i).FindPropertyRelative("startupPalette").objectReferenceValue;
    var mat=surfaces[i].sharedMaterial;
    Check(Delta(mat.GetColor("_BaseColor"),startup.Colors.fill.linear)<.00001f,"Opening fill "+i);
    Check(Delta(mat.GetColor("_TertiaryColor"),startup.Colors.tertiary.linear)<.00001f,"Opening inlay "+i);
    Check(mat.GetTexture("_TertiaryMap").name=="OuterPetal_Peacock_Inlays","Inlay mask "+i);
    Check(flower.TryGetPetalLightSample(i,out var light)&&light.maxColorComponent>0,"Startup spill light "+i);
}
var camera=UnityEngine.Camera.main;var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;float oldAspect=camera.aspect;
var rt=UnityEngine.RenderTexture.GetTemporary(1200,1200,24,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.sRGB);
var image=new UnityEngine.Texture2D(1200,1200,UnityEngine.TextureFormat.RGB24,false);
try
{
    camera.targetTexture=rt;camera.aspect=1;camera.Render();UnityEngine.RenderTexture.active=rt;
    image.ReadPixels(new UnityEngine.Rect(0,0,1200,1200),0,0);image.Apply();
    System.IO.File.WriteAllBytes("output/flower-outer-peacock/startup-flower.png",image.EncodeToPNG());
}
finally{camera.targetTexture=oldTarget;camera.aspect=oldAspect;UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(image);}
// A fresh instance proves startup ownership and all normal palette paths without consuming the review flower.
var probeObject=UnityEngine.Object.Instantiate(flower.gameObject);probeObject.name="Temporary peacock validation";
try
{
    var probe=probeObject.GetComponent<Amgen.Deepavali.FlowerPaletteController>();
    var so=new UnityEditor.SerializedObject(probe);var pp=so.FindProperty("petals");var pa=so.FindProperty("petalAppearances");
    Check(Enumerable.Range(8,15).All(probe.IsUsingStartupPalette),"New instance starts in opening colors");
    Check(probe.SetPetalState(8,Amgen.Deepavali.FlowerPaletteState.Base),"Per-petal same-state accepted");
    Check(!probe.IsUsingStartupPalette(8)&&Enumerable.Range(9,14).All(probe.IsUsingStartupPalette),"Per-petal takeover leaves other startup petals untouched");
    probe.enabled=false;probe.enabled=true;
    Check(!probe.IsUsingStartupPalette(8)&&Enumerable.Range(9,14).All(probe.IsUsingStartupPalette),"Disable/enable preserves individual startup consumption");
    for(int state=0;state<7;state++)
    {
        Check(probe.SetState((Amgen.Deepavali.FlowerPaletteState)state),"Normal state accepted "+state);
        Check(!Enumerable.Range(8,15).Any(probe.IsUsingStartupPalette),"Normal state consumes opening "+state);
        for(int i=8;i<23;i++)
        {
            var set=(Amgen.Deepavali.FlowerPaletteSet)pa.GetArrayElementAtIndex(i).FindPropertyRelative("paletteOverride").objectReferenceValue;
            set.TryGet((Amgen.Deepavali.FlowerPaletteState)state,out var colors);var mat=((UnityEngine.Renderer)pp.GetArrayElementAtIndex(i).objectReferenceValue).sharedMaterial;
            Check(Delta(mat.GetColor("_BaseColor"),colors.fill.linear)<.00001f,"Current palette fill "+state+"/"+i);
            Check(Delta(mat.GetColor("_TertiaryColor"),(colors.tertiary.a>0?colors.tertiary:colors.accent).linear)<.00001f,"Current palette replaces inlay "+state+"/"+i);
        }
    }
    probe.enabled=false;probe.enabled=true;Check(!Enumerable.Range(8,15).Any(probe.IsUsingStartupPalette),"Startup does not return after regular palette");
}
finally{UnityEngine.Object.DestroyImmediate(probeObject);}
var shader=surfaces[8].sharedMaterial.shader;Check(!UnityEditor.ShaderUtil.ShaderHasError(shader),"Shader compilation");
var batcherMethod=typeof(UnityEditor.ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
bool batcher=batcherMethod!=null&&(int)batcherMethod.Invoke(null,new object[]{shader,0})==0;Check(batcher,"SRP Batcher");
var result=new{passed=failures.Count==0,outerPetals=15,states=7,startupTone1=8,startupTone2=7,tertiaryFallback="Accent when alpha is zero",batcher,failures};
System.IO.File.WriteAllText("output/flower-outer-peacock/state-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
