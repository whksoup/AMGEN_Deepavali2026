if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Enter Play Mode first.");
var f=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var preview=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerAppearancePreview>();
var data=new UnityEditor.SerializedObject(f);var p=data.FindProperty("petals");var a=data.FindProperty("petalAppearances");
var r=(UnityEngine.Renderer)p.GetArrayElementAtIndex(8).objectReferenceValue;
var opening=(Amgen.Deepavali.FlowerStartupPalette)a.GetArrayElementAtIndex(8).FindPropertyRelative("startupPalette").objectReferenceValue;
var normal=(Amgen.Deepavali.FlowerPaletteSet)a.GetArrayElementAtIndex(8).FindPropertyRelative("paletteOverride").objectReferenceValue;
var original=opening.Colors.tertiary;var temporary=new UnityEngine.Color(.18f,.64f,.8f,1);
var errors=new System.Collections.Generic.List<string>();var failures=new System.Collections.Generic.List<string>();
void Check(bool pass,string label){if(!pass)failures.Add(label);}
void EditInlay(UnityEngine.Color color){var so=new UnityEditor.SerializedObject(opening);so.FindProperty("colors").FindPropertyRelative("tertiary").colorValue=color;so.ApplyModifiedPropertiesWithoutUndo();}
UnityEngine.Application.LogCallback logger=(m,s,t)=>{if(t==UnityEngine.LogType.Error||t==UnityEngine.LogType.Exception||t==UnityEngine.LogType.Assert)errors.Add(m);};
UnityEngine.Application.logMessageReceived+=logger;
int phase=0,frames=0,startFrame=UnityEngine.Time.frameCount;bool liveEdit=false,midWave=false;double began=UnityEditor.EditorApplication.timeSinceStartup;
EditInlay(temporary);a.GetArrayElementAtIndex(8).FindPropertyRelative("emissionScale").floatValue=.75f;data.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    bool finished=false;
    try
    {
        frames++;if(UnityEditor.EditorApplication.timeSinceStartup-began>15||!UnityEditor.EditorApplication.isPlaying)throw new System.TimeoutException("Play check timed out/interrupted");
        if(phase==0&&UnityEngine.Time.frameCount>startFrame+4)
        {
            Check(Enumerable.Range(8,15).All(f.IsUsingStartupPalette),"Inspector glow edit preserves startup");
            liveEdit=UnityEngine.Vector4.Distance(r.sharedMaterial.GetColor("_TertiaryColor"),temporary.linear)<.00001f;Check(liveEdit,"Startup tertiary live editing");
            EditInlay(original);phase=1;
        }
        else if(phase==1&&UnityEngine.Time.frameCount>startFrame+8)
        {
            preview.SelectAll();preview.SetEmission(1);preview.SelectPalette(0);
            Check(f.IsTransitioning,"Base request starts wave despite unchanged enum");phase=2;
        }
        else if(phase==2&&f.TransitionProgress>.15f&&f.TransitionProgress<.8f)
        {
            normal.TryGet(Amgen.Deepavali.FlowerPaletteState.Base,out var target);
            midWave=UnityEngine.Vector4.Distance(r.sharedMaterial.GetColor("_BaseColor"),opening.Colors.fill.linear)<.00001f&&UnityEngine.Vector4.Distance(r.sharedMaterial.GetColor("_TargetBaseColor"),target.fill.linear)<.00001f;
            Check(midWave,"Wave blends opening pink into existing red");
            Check(UnityEngine.Vector4.Distance(r.sharedMaterial.GetColor("_TertiaryColor"),original.linear)<.00001f,"Opening inlay remains source during wave");
            Check(UnityEngine.Vector4.Distance(r.sharedMaterial.GetColor("_TargetTertiaryColor"),target.accent.linear)<.00001f,"Current palette overwrites tertiary in same wave");phase=3;
        }
        else if(phase==3&&!f.IsTransitioning)
        {
            Check(!Enumerable.Range(8,15).Any(f.IsUsingStartupPalette),"All outer startup colors consumed");
            Check(Enumerable.Range(8,15).All(i=>f.GetPetalState(i)==Amgen.Deepavali.FlowerPaletteState.Base),"Same-state wave reaches red");
            f.enabled=false;f.enabled=true;Check(!Enumerable.Range(8,15).Any(f.IsUsingStartupPalette),"Disable/enable does not replay startup");finished=true;
        }
    }
    catch(System.Exception ex){failures.Add(ex.ToString());finished=true;}
    if(!finished)return;
    UnityEditor.EditorApplication.update-=tick;EditInlay(original);UnityEditor.AssetDatabase.SaveAssetIfDirty(opening);
    UnityEngine.Application.logMessageReceived-=logger;
    var result=new{passed=failures.Count==0&&errors.Count==0,editorUpdates=frames,simulationFrames=UnityEngine.Time.frameCount-startFrame,seconds=UnityEditor.EditorApplication.timeSinceStartup-began,phase,liveEdit,midWave,failures,errors};
    System.IO.File.WriteAllText("output/flower-outer-peacock/wave-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
};
UnityEditor.EditorApplication.update+=tick;return new{started=true};
