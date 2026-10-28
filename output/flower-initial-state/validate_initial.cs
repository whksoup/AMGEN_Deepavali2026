if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required.");
var c=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var d=new UnityEditor.SerializedObject(c);var appearances=d.FindProperty("petalAppearances");
var surfaces=Enumerable.Range(0,c.PetalCount).Select(i=>(UnityEngine.Renderer)d.FindProperty("petals").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
var center=(UnityEngine.Renderer)d.FindProperty("centerDisc").objectReferenceValue;
var checks=new System.Collections.Generic.Dictionary<string,bool>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var clock=c.GetType().GetField("waveStartedAt",flags);
var update=(System.Action)System.Delegate.CreateDelegate(typeof(System.Action),c,c.GetType().GetMethod("Update",flags));
void Step(float fraction){clock.SetValue(c,UnityEngine.Time.timeAsDouble-c.TransitionDuration*fraction);update();}
bool ColorNear(UnityEngine.Color a,UnityEngine.Color b)=>(new UnityEngine.Vector4(a.r-b.r,a.g-b.g,a.b-b.b,a.a-b.a)).sqrMagnitude<0.000001f;
var errors=new System.Collections.Generic.List<string>();
UnityEngine.Application.LogCallback logger=(message,trace,type)=>{if(type==UnityEngine.LogType.Error||type==UnityEngine.LogType.Exception)errors.Add(message);};
UnityEngine.Application.logMessageReceived+=logger;
try
{
    c.ResetToInitialState();update();
    checks["all23OpeningPalettesAndCenter"]=Enumerable.Range(0,23).All(c.IsUsingStartupPalette)&&c.IsUsingStartupCenter;
    checks["eightUniqueInnerMasks"]=surfaces.Take(8).All(r=>r.sharedMaterial.GetFloat("_StartupSymbolEnabled")==1)&&surfaces.Take(8).Select(r=>r.sharedMaterial.GetVector("_StartupRect")).Distinct().Count()==8;
    checks["outerPeacockArtworkPreserved"]=surfaces.Skip(8).All(r=>r.sharedMaterial.GetFloat("_StartupSymbolEnabled")==0&&r.sharedMaterial.GetTexture("_BaseMap").name=="OuterPetal_Peacock_Regions_RGBA"&&r.sharedMaterial.GetTexture("_TertiaryMap").name=="OuterPetal_Peacock_Inlays");
    checks["eightDistinctFills"]=surfaces.Take(8).Select(r=>r.sharedMaterial.GetColor("_BaseColor")).Distinct().Count()==8;
    checks["sameGoldAllInnerPalettes"]=surfaces.Take(8).Select(r=>r.sharedMaterial.GetColor("_PatternColor")).Distinct().Count()==1;
    checks["sameIndependentYellowEdge"]=surfaces.Take(8).Select(r=>r.sharedMaterial.GetColor("_EdgeColor")).Distinct().Count()==1&&surfaces.Take(8).All(r=>r.sharedMaterial.GetColor("_EdgeColor").a==1);
    c.CenterIconSet.TryGet(6,out var mandala);var mr=mandala.uvRect;
    checks["openingCenterMandala"]=center.sharedMaterial.GetVector("_SymbolRect")==new UnityEngine.Vector4(mr.width,mr.height,mr.x,mr.y);
    checks["originalPhaseIconMapping"]=Enumerable.Range(1,6).All(i=>c.GetCenterIcon((Amgen.Deepavali.FlowerPaletteState)i)==i-1)&&c.GetCenterIcon(Amgen.Deepavali.FlowerPaletteState.Base)==-1;
    c.SetPetalEmissionScale(0,0.75f);c.RefreshPaletteColors();update();
    checks["glowRefreshKeepsStartup"]=c.IsUsingStartupPalette(0)&&c.IsUsingStartupCenter&&!c.IsTransitioning;
    c.SetPetalEmissionScale(0,1);
    c.TransitionPetalTo(0,Amgen.Deepavali.FlowerPaletteState.Blue);Step(0.4f);
    checks["singlePetalOnlyConsumesItsStartup"]=c.IsTransitioning&&!c.IsStartupRequested(0)&&Enumerable.Range(1,22).All(c.IsStartupRequested)&&c.IsUsingStartupCenter;
    Step(1.01f);
    checks["singlePetalSettlesToOriginalArtwork"]=!c.IsUsingStartupPalette(0)&&surfaces[0].sharedMaterial.GetFloat("_TargetStartupAppearance")==0&&c.GetSymbol(0)==-1;
    c.ResetToInitialState();c.TransitionCenterTo(Amgen.Deepavali.FlowerPaletteState.Gold);Step(1.01f);
    checks["centerOnlyLeavesPetalsInitial"]=!c.IsUsingStartupCenter&&Enumerable.Range(0,23).All(c.IsUsingStartupPalette);
    c.ResetToInitialState();c.TransitionTo(Amgen.Deepavali.FlowerPaletteState.Base);Step(0.4f);
    checks["sameEnumBaseStartsRealOpeningWave"]=c.IsTransitioning&&c.TransitionProgress>0.3f&&surfaces[0].sharedMaterial.GetFloat("_StartupAppearance")==1&&surfaces[0].sharedMaterial.GetFloat("_TargetStartupAppearance")==0;
    Step(1.01f);
    checks["sameEnumBaseConsumesAll"]=!c.IsTransitioning&&!c.IsUsingStartupCenter&&Enumerable.Range(0,23).All(i=>!c.IsUsingStartupPalette(i));
    bool allPhases=true,outerTertiary=true;
    for(int state=0;state<7;state++)
    {
        c.ResetToInitialState();c.TransitionTo((Amgen.Deepavali.FlowerPaletteState)state);Step(0.45f);
        allPhases&=c.IsTransitioning&&surfaces.Take(8).All(r=>r.sharedMaterial.GetFloat("_StartupAppearance")==1&&r.sharedMaterial.GetFloat("_TargetStartupAppearance")==0);
        Step(1.01f);
        for(int i=0;i<23;i++)
        {
            var item=appearances.GetArrayElementAtIndex(i);
            var set=item.FindPropertyRelative("paletteOverride").objectReferenceValue as Amgen.Deepavali.FlowerPaletteSet;
            if(set==null)set=c.PaletteSet;
            set.TryGet((Amgen.Deepavali.FlowerPaletteState)state,out var palette);
            var mat=surfaces[i].sharedMaterial;
            allPhases&=!c.IsUsingStartupPalette(i)&&ColorNear(mat.GetColor("_TargetBaseColor"),palette.fill.linear)&&ColorNear(mat.GetColor("_TargetPatternColor"),palette.ornament.linear)&&mat.GetFloat("_TargetEdgeIntensity")==palette.edgeIntensity&&mat.GetColor("_TargetEdgeColor").a==0;
            if(i>=8)outerTertiary&=ColorNear(mat.GetColor("_TargetTertiaryColor"),(palette.tertiary.a>0?palette.tertiary:palette.accent).linear);
        }
    }
    checks["all7PhasesRestoreExistingColorsAndEdgeSettings"]=allPhases;
    checks["outerTertiaryTakesOverForAll7Phases"]=outerTertiary;
    c.ResetToInitialState();c.TransitionTo(Amgen.Deepavali.FlowerPaletteState.Blue);Step(0.3f);
    float progress=c.TransitionProgress;c.TransitionTo(Amgen.Deepavali.FlowerPaletteState.Pink);c.TransitionTo(Amgen.Deepavali.FlowerPaletteState.Green);
    checks["latestRequestDoesNotRestartActiveWave"]=c.HasPendingTransition&&c.TransitionProgress==progress;
    Step(1.01f);
    checks["queueStartsFromCompletedBlue"]=c.IsTransitioning&&c.GetPetalState(0)==Amgen.Deepavali.FlowerPaletteState.Blue&&!c.IsUsingStartupPalette(0)&&surfaces[0].sharedMaterial.GetFloat("_StartupAppearance")==0;
    Step(1.01f);
    checks["latestQueuedGreenSettles"]=!c.IsTransitioning&&c.GetPetalState(0)==Amgen.Deepavali.FlowerPaletteState.Green&&c.CenterState==Amgen.Deepavali.FlowerPaletteState.Green;
    c.enabled=false;c.enabled=true;update();
    checks["noStartupReplayAfterDisableEnable"]=Enumerable.Range(0,23).All(i=>!c.IsUsingStartupPalette(i))&&!c.IsUsingStartupCenter&&c.GetPetalState(8)==Amgen.Deepavali.FlowerPaletteState.Green;
    c.ResetToInitialState();update();
    checks["explicitInitialResetReplaysAll"]=Enumerable.Range(0,23).All(c.IsUsingStartupPalette)&&c.IsUsingStartupCenter;
    checks["invalidStatesRejected"]=!c.TransitionTo((Amgen.Deepavali.FlowerPaletteState)(-1))&&!c.TransitionCenterTo((Amgen.Deepavali.FlowerPaletteState)99);
    checks["shaderDiagnosticsClear"]=UnityEditor.ShaderUtil.GetShaderMessages(surfaces[0].sharedMaterial.shader).Length==0;
    checks["noRuntimeErrors"]=errors.Count==0;
    var result=new{passed=checks.Values.All(x=>x),checks,errors};
    System.IO.File.WriteAllText("output/flower-initial-state/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
    return result;
}
finally{UnityEngine.Application.logMessageReceived-=logger;c.SetPetalEmissionScale(0,1);c.ResetToInitialState();}
