if(!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var failures=new System.Collections.Generic.List<string>();
void Check(bool condition,string message){if(!condition)failures.Add(message);}
var probeObject=UnityEngine.Object.Instantiate(flower.gameObject);
probeObject.name="Temporary tonal refinement validation";
probeObject.transform.position+=new UnityEngine.Vector3(20,0,0);
try
{
    var probe=probeObject.GetComponent<Amgen.Deepavali.FlowerPaletteController>();
    var so=new UnityEditor.SerializedObject(probe);var pp=so.FindProperty("petals");
    var renderers=Enumerable.Range(0,probe.PetalCount).Select(i=>(UnityEngine.Renderer)pp.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    var center=(UnityEngine.Renderer)so.FindProperty("centerDisc").objectReferenceValue;
    for(int state=0;state<7;state++)
    {
        Check(probe.SetState((Amgen.Deepavali.FlowerPaletteState)state),"Palette "+state);
        for(int i=0;i<renderers.Length;i++)
        {
            var m=renderers[i].sharedMaterial;
            Check(m.GetFloat("_RimGlowScale")>2,"Bright rim "+state+"/"+i);
            Check(m.GetFloat("_OrnamentGlowScale")<.4f,"Quiet ornament "+state+"/"+i);
            Check(m.GetFloat("_BodyShadeStrength")==1&&m.GetFloat("_BodySpecularStrength")<.2f,"Dark body "+state+"/"+i);
            Check(m.GetTexture("_BodyShadingMap")!=null,"Body mask "+state+"/"+i);
            Check(probe.TryGetPetalLightSample(i,out var spill)&&!float.IsNaN(spill.r)&&spill.maxColorComponent>0,"Finite spill "+state+"/"+i);
        }
        Check(center.sharedMaterial.GetFloat("_CenterIconGlow")<.5f,"Quiet center "+state);
        Check(center.sharedMaterial.GetTexture("_CenterShadingMap")!=null,"Center mask "+state);
    }
    probe.ResetToInitialState();
    Check(probe.IsUsingStartupCenter&&Enumerable.Range(0,probe.PetalCount).All(probe.IsUsingStartupPalette),"Opening palettes restored");
    var shader=renderers[0].sharedMaterial.shader;
    var errors=UnityEditor.ShaderUtil.GetShaderMessages(shader).Where(x=>x.severity.ToString()=="Error").Select(x=>x.message).ToArray();
    Check(errors.Length==0,"Shader compilation");
    var method=typeof(UnityEditor.ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
    bool batcher=method!=null&&(int)method.Invoke(null,new object[]{shader,0})==0;
    Check(batcher,"SRP Batcher");
    var renderer=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
    Check(renderer.rendererFeatures.Any(f=>f.GetType().Name=="ScreenSpaceAmbientOcclusion"&&f.isActive),"SSAO active");
    var result=new{passed=failures.Count==0,palettes=7,petals=probe.PetalCount,shaderErrors=errors,srpBatcherCompatible=batcher,failures};
    string json=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);
    System.IO.File.WriteAllText("output/flower-tonal-refinement/validation.json",json);return json;
}
finally{UnityEngine.Object.DestroyImmediate(probeObject);}
