// Asynchronous Editor-update integration check. Restores all authored/runtime values on completion.
if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Enter Play Mode first.");
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var preview=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerAppearancePreview>();
var data=new UnityEditor.SerializedObject(flower);var petals=data.FindProperty("petals");
var renderers=Enumerable.Range(0,flower.PetalCount).Select(i=>(UnityEngine.Renderer)petals.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
var originalState=flower.SelectedState;var states=Enumerable.Range(0,flower.PetalCount).Select(flower.GetRequestedPetalState).ToArray();
var symbols=Enumerable.Range(0,flower.PetalCount).Select(flower.GetSymbol).ToArray();
var tone=UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerPaletteSet>("Assets/HK_Signature/Deepavali/PetalPrototype/Palettes/Deepavali Outer Tone 1.asset");
tone.TryGet(Amgen.Deepavali.FlowerPaletteState.Pink,out var pink);var originalFill=pink.fill;
var temporaryFill=new UnityEngine.Color(.57f,.08f,.24f,1f);
void SetTone(UnityEngine.Color color){var so=new UnityEditor.SerializedObject(tone);so.FindProperty("states").GetArrayElementAtIndex(2).FindPropertyRelative("fill").colorValue=color;so.ApplyModifiedPropertiesWithoutUndo();}
var failures=new System.Collections.Generic.List<string>();var logs=new System.Collections.Generic.List<string>();
void Check(bool pass,string label){if(!pass)failures.Add(label);}
UnityEngine.Application.LogCallback logger=(m,s,t)=>{if(t==UnityEngine.LogType.Error||t==UnityEngine.LogType.Exception||t==UnityEngine.LogType.Assert)logs.Add(m);};
UnityEngine.Application.logMessageReceived+=logger;
int phase=0,frames=0,editedAtFrame=0;float progressBeforeEdit=0;bool liveRefresh=false,overlap=false,restoredOwnership=false;
double began=UnityEditor.EditorApplication.timeSinceStartup;
flower.SetState(Amgen.Deepavali.FlowerPaletteState.Base);preview.SelectAll();preview.SelectPalette(2);flower.TransitionSymbol(8,0);
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    bool finish=false;
    try
    {
        frames++;
        if(!UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.timeSinceStartup-began>20)throw new System.TimeoutException("Live integration check timed out or Play Mode ended.");
        if(phase==0 && flower.TransitionProgress>.1f)
        {
            overlap=flower.IsTransitioning&&flower.IsArtworkTransitioning(8)&&flower.GetArtworkProgress(8)>0;
            progressBeforeEdit=flower.TransitionProgress;SetTone(temporaryFill);editedAtFrame=frames;phase=1;
        }
        else if(phase==1 && frames>editedAtFrame+2)
        {
            var actual=renderers[8].sharedMaterial.GetColor("_TargetBaseColor");var expected=temporaryFill.linear;
            liveRefresh=UnityEngine.Vector4.Distance(actual,expected)<.00001f;
            Check(liveRefresh,"Tone asset updates runtime materials automatically");
            Check(flower.TransitionProgress>=progressBeforeEdit,"Tone edit does not restart palette wave");
            SetTone(originalFill);phase=2;
        }
        else if(phase==2 && !flower.IsTransitioning&&!flower.IsArtworkTransitioning(8))
        {
            Check(Enumerable.Range(0,23).All(i=>flower.GetPetalState(i)==Amgen.Deepavali.FlowerPaletteState.Pink),"Whole-flower palette UI reaches all 23 petals");
            Check(flower.GetSettledSymbol(8)==0,"Outer artwork reveal completes");
            Check(renderers[8].sharedMaterial.GetFloat("_TargetSymbolEnabled")==1,"Outer symbol reaches shader");
            Check(Enumerable.Range(0,23).Where(i=>i!=8).All(i=>flower.GetSymbol(i)==symbols[i]),"Independent artwork leaves other petals unchanged");
            var actual=renderers[8].sharedMaterial.GetColor("_TargetBaseColor");Check(UnityEngine.Vector4.Distance(actual,originalFill.linear)<.00001f,"Restored tone refreshes automatically");
            flower.TransitionSymbol(8,-1);preview.SelectPalette(1);phase=3;
        }
        else if(phase==3 && !flower.IsTransitioning&&!flower.IsArtworkTransitioning(8))
        {
            Check(flower.GetSettledSymbol(8)==-1,"Original ornament restored");
            Check(Enumerable.Range(0,23).All(i=>flower.GetPetalState(i)==Amgen.Deepavali.FlowerPaletteState.Gold),"Second palette UI wave completes");
            var ids=renderers.Select(r=>r.sharedMaterial.GetInstanceID()).ToArray();flower.enabled=false;
            restoredOwnership=renderers.Skip(8).All(r=>UnityEditor.AssetDatabase.GetAssetPath(r.sharedMaterial).Contains("Outer Petal Palette Tone"));
            flower.enabled=true;
            Check(restoredOwnership,"Disable restores outer material templates");
            Check(renderers.Select(r=>r.sharedMaterial.GetInstanceID()).Distinct().Count()==23,"Enable owns 23 independent materials");
            Check(renderers.Skip(8).All(r=>r.sharedMaterial.GetTexture("_BaseMap").name=="OuterPetal_Regions_RGBA"),"Enable keeps outer artwork");
            Check(overlap,"Color and artwork waves progress together");finish=true;
        }
    }
    catch(System.Exception ex){failures.Add(ex.ToString());finish=true;}
    if(!finish)return;
    UnityEditor.EditorApplication.update-=tick;
    try
    {
        SetTone(originalFill);UnityEditor.AssetDatabase.SaveAssetIfDirty(tone);
        if(flower!=null&&UnityEditor.EditorApplication.isPlaying)
        {
            if(!flower.enabled)flower.enabled=true;flower.SetState(originalState);
            for(int i=0;i<states.Length;i++){flower.SetPetalState(i,states[i]);flower.SetSymbol(i,symbols[i]);}
        }
    }
    finally
    {
        UnityEngine.Application.logMessageReceived-=logger;
        var report=new{passed=failures.Count==0&&logs.Count==0,frames,seconds=UnityEditor.EditorApplication.timeSinceStartup-began,liveRefresh,overlap,restoredOwnership,failures,errors=logs};
        var path=System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,"output/flower-outer-appearance/live-validation.json");
        System.IO.File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
    }
};
UnityEditor.EditorApplication.update+=tick;
return new{started=true,report="output/flower-outer-appearance/live-validation.json"};
