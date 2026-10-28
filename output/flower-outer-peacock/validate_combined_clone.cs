if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Run while the combined scene is playing.");
var source=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var before=Enumerable.Range(8,15).Select(source.IsUsingStartupPalette).ToArray();
var scope=new UnityEngine.GameObject("Temporary outer startup regression");scope.hideFlags=UnityEngine.HideFlags.HideAndDontSave;scope.SetActive(false);
var errors=new System.Collections.Generic.List<string>();var failures=new System.Collections.Generic.List<string>();
void Check(bool value,string label){if(!value)failures.Add(label);}
UnityEngine.Application.LogCallback logger=(m,s,t)=>{if(t==UnityEngine.LogType.Error||t==UnityEngine.LogType.Exception||t==UnityEngine.LogType.Assert)errors.Add(m);};
UnityEngine.Application.logMessageReceived+=logger;
try
{
    var go=UnityEngine.Object.Instantiate(source.gameObject,scope.transform);
    foreach(var renderer in go.GetComponentsInChildren<UnityEngine.Renderer>(true))renderer.enabled=false;
    foreach(var light in go.GetComponentsInChildren<UnityEngine.Light>(true))light.enabled=false;
    var lightRig=go.GetComponent<Amgen.Deepavali.FlowerPaletteLighting>();if(lightRig!=null)lightRig.enabled=false;
    scope.SetActive(true);var f=go.GetComponent<Amgen.Deepavali.FlowerPaletteController>();
    var so=new UnityEditor.SerializedObject(f);var a=so.FindProperty("petalAppearances");var p=so.FindProperty("petals");
    Check(Enumerable.Range(8,15).All(f.IsUsingStartupPalette),"All outer startup colors survive inner integration");
    Check(Enumerable.Range(8,15).All(i=>!a.GetArrayElementAtIndex(i).FindPropertyRelative("useStartupArtwork").boolValue),"Inner startup artwork gate disabled on outer petals");
    Check(f.TransitionTo(Amgen.Deepavali.FlowerPaletteState.Base)&&f.IsTransitioning,"First Base request starts wave");
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    var clock=typeof(Amgen.Deepavali.FlowerPaletteController).GetField("waveStartedAt",flags);
    var update=typeof(Amgen.Deepavali.FlowerPaletteController).GetMethod("Update",flags);
    clock.SetValue(f,UnityEngine.Time.timeAsDouble-f.TransitionDuration*.5);update.Invoke(f,null);
    Check(f.TransitionProgress>.49f&&f.TransitionProgress<.51f,"Mid-wave progression");
    for(int i=8;i<23;i++)
    {
        var ap=a.GetArrayElementAtIndex(i);var startup=(Amgen.Deepavali.FlowerStartupPalette)ap.FindPropertyRelative("startupPalette").objectReferenceValue;
        var palette=(Amgen.Deepavali.FlowerPaletteSet)ap.FindPropertyRelative("paletteOverride").objectReferenceValue;palette.TryGet(Amgen.Deepavali.FlowerPaletteState.Base,out var normal);
        var mat=((UnityEngine.Renderer)p.GetArrayElementAtIndex(i).objectReferenceValue).sharedMaterial;
        Check(UnityEngine.Vector4.Distance(mat.GetColor("_TertiaryColor"),startup.Colors.tertiary.linear)<.00001f,"Opening tertiary source "+i);
        Check(UnityEngine.Vector4.Distance(mat.GetColor("_TargetTertiaryColor"),(normal.tertiary.a>0?normal.tertiary:normal.accent).linear)<.00001f,"Normal tertiary target "+i);
        Check(mat.GetTexture("_BaseMap").name=="OuterPetal_Peacock_Regions_RGBA"&&mat.GetTexture("_TertiaryMap").name=="OuterPetal_Peacock_Inlays","Peacock artwork preserved "+i);
    }
    clock.SetValue(f,UnityEngine.Time.timeAsDouble-f.TransitionDuration-1);update.Invoke(f,null);
    Check(!f.IsTransitioning&&!Enumerable.Range(8,15).Any(f.IsUsingStartupPalette),"First wave consumes outer startup");
    f.enabled=false;f.enabled=true;Check(!Enumerable.Range(8,15).Any(f.IsUsingStartupPalette),"No startup replay on re-enable");
    for(int state=0;state<7;state++)
    {
        Check(f.SetState((Amgen.Deepavali.FlowerPaletteState)state),"Normal state "+state);
        for(int i=8;i<23;i++)
        {
            var palette=(Amgen.Deepavali.FlowerPaletteSet)a.GetArrayElementAtIndex(i).FindPropertyRelative("paletteOverride").objectReferenceValue;
            palette.TryGet((Amgen.Deepavali.FlowerPaletteState)state,out var normal);var mat=((UnityEngine.Renderer)p.GetArrayElementAtIndex(i).objectReferenceValue).sharedMaterial;
            Check(UnityEngine.Vector4.Distance(mat.GetColor("_BaseColor"),normal.fill.linear)<.00001f,"Existing fill "+state+"/"+i);
            Check(UnityEngine.Vector4.Distance(mat.GetColor("_TertiaryColor"),(normal.tertiary.a>0?normal.tertiary:normal.accent).linear)<.00001f,"Existing detail "+state+"/"+i);
        }
    }
    Check(before.SequenceEqual(Enumerable.Range(8,15).Select(source.IsUsingStartupPalette)),"Active flower was untouched");
}
catch(System.Exception ex){failures.Add(ex.ToString());}
finally{UnityEngine.Object.DestroyImmediate(scope);UnityEngine.Application.logMessageReceived-=logger;}
var result=new{passed=failures.Count==0&&errors.Count==0,outerPetals=15,states=7,sharedInnerIntegration=true,failures,errors};
System.IO.File.WriteAllText("output/flower-outer-peacock/combined-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
