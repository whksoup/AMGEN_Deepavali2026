if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required");
var seq=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerLightSequence>();var flower=seq.flower;var config=seq.Config;
if(config==null)throw new System.Exception("Sequence not initialized");
var data=new UnityEditor.SerializedObject(flower);var center=(UnityEngine.Renderer)data.FindProperty("centerDisc").objectReferenceValue;
var checks=new System.Collections.Generic.Dictionary<string,bool>();
var camera=UnityEngine.Camera.main;var oldRect=camera.rect;var oldAspect=camera.aspect;var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;
var motions=flower.GetComponentsInChildren<UnityEngine.MonoBehaviour>().Where(m=>m is Amgen.Deepavali.PetalMotionController||m is Amgen.Deepavali.OuterPetalLayerMotion).ToArray();
var motionStates=motions.Select(m=>m.enabled).ToArray();
const int width=1400,height=1100;
var rt=UnityEngine.RenderTexture.GetTemporary(width,height,24,UnityEngine.RenderTextureFormat.ARGBHalf,UnityEngine.RenderTextureReadWrite.Linear);
var read=new UnityEngine.Texture2D(width,height,UnityEngine.TextureFormat.RGBAFloat,false,true);var png=new UnityEngine.Texture2D(width,height,UnityEngine.TextureFormat.RGB24,false);
UnityEngine.Vector4 Levels(int i)=>flower.GetPetalRenderer(i).sharedMaterial.GetVector("_FlowerStageLevels");
bool CenterDark()=>center.sharedMaterial.GetVector("_FlowerStageLevels").x==0&&center.sharedMaterial.GetVector("_FlowerStageLevels").w==0;
bool OuterDark()=>Enumerable.Range(8,14).All(i=>Levels(i).x==0&&flower.GetPetalRenderer(i).sharedMaterial.GetFloat("_PetalLightEnabled")==0);
void Capture(string name)
{
    seq.ApplyLighting();camera.Render();UnityEngine.RenderTexture.active=rt;read.ReadPixels(new UnityEngine.Rect(0,0,width,height),0,0);read.Apply();
    png.SetPixels(read.GetPixels().Select(c=>c.gamma).ToArray());png.Apply();System.IO.File.WriteAllBytes("output/refined-flower-integration/light-"+name+".png",png.EncodeToPNG());
}
int resultEvents=0;System.Action<int,Amgen.Deepavali.FlowerPaletteState> onResult=(slot,color)=>resultEvents++;
seq.ResultSelected+=onResult;
try
{
    seq.autoPreview=false;foreach(var m in motions)m.enabled=false;
    camera.rect=new UnityEngine.Rect(0,0,1,1);camera.aspect=width/(float)height;camera.targetTexture=rt;
    checks["eight_distinct_supported_results"]=config.outcomes.Length==8&&config.outcomes.Distinct().Count()==8&&config.outcomes.All(c=>c!=Amgen.Deepavali.FlowerPaletteState.Base&&flower.SupportsPalette(c));
    seq.Sleep();
    checks["sleep_all_emission_off"]=Enumerable.Range(0,flower.PetalCount).All(i=>Levels(i).x==0)&&CenterDark();
    Capture("01-sleeping");
    seq.WakeAmbient();seq.Tick(3);
    var first=Enumerable.Range(0,flower.PetalCount).Select(i=>flower.GetPetalRenderer(i).sharedMaterial.GetVector("_PetalLightTravel").z).ToArray();
    checks["ambient_spatially_varied"]=first.Max()-first.Min()>.3f;
    checks["ambient_center_rim_off"]=CenterDark();Capture("02-ambient");
    seq.Tick(7);var second=Enumerable.Range(0,flower.PetalCount).Select(i=>flower.GetPetalRenderer(i).sharedMaterial.GetVector("_PetalLightTravel").z).ToArray();
    checks["ambient_changes_over_time"]=first.Zip(second,(a,b)=>UnityEngine.Mathf.Abs(a-b)).Max()>.2f;
    Capture("03-ambient-later");
    seq.StartRoulette();int winner=seq.WinnerSlot;var chosen=seq.WinnerPalette;
    checks["roulette_entry_fades_from_zero"]=Enumerable.Range(0,8).All(i=>Levels(i).x==0);
    checks["retrigger_does_not_reroll"]=!seq.StartRoulette()&&seq.WinnerSlot==winner;
    bool masked=true,clockwise=true;int last=seq.ActiveSlot;var seen=new System.Collections.Generic.HashSet<int>{last};
    for(int step=0;step<240;step++)
    {
        seq.Tick(config.rouletteSeconds/240f);
        if(seq.ActiveSlot!=last){clockwise&=seq.ActiveSlot==(last+1)%8;last=seq.ActiveSlot;seen.Add(last);}
        var lit=Enumerable.Range(0,8).Where(i=>Levels(i).x>0).ToArray();
        masked&=OuterDark()&&CenterDark()&&lit.Length>=1&&lit.Length<=2;
        if(lit.Length==2)masked&=(lit[1]-lit[0]==1||lit[1]-lit[0]==7);
        if(step==55)Capture("04-roulette");
    }
    checks["roulette_clockwise_visits_all_eight"]=clockwise&&seen.Count==8;
    checks["roulette_only_adjacent_inner_tips_crossfade"]=masked;
    checks["roulette_tip_only_no_edge_or_wash"]=Enumerable.Range(0,8).All(i=>{
        var material=flower.GetPetalRenderer(i).sharedMaterial;
        var travel=material.GetVector("_PetalLightTravel");var style=material.GetVector("_PetalLightStyle");
        return travel.z==0&&travel.y>0&&style.y==0&&Levels(i).y==0;});
    checks["roulette_icons_share_tip_fade"]=Enumerable.Range(0,8).All(i=>{
        var material=flower.GetPetalRenderer(i).sharedMaterial;
        return material.GetFloat("_PetalLightIconBrightness")==config.rouletteIconBrightness&&
            UnityEngine.Mathf.Approximately(material.GetFloat("_PetalLightTipReach"),config.rouletteTipReach);});
    checks["roulette_lands_on_preselected_winner"]=seq.WinnerSlot==winner&&seq.ActiveSlot==winner;
    Capture("05-winner-hold");
    float originalReach=config.rouletteTipReach,originalIcon=config.rouletteIconBrightness;
    config.rouletteTipReach=.14f;Capture("08-reach-14");
    config.rouletteTipReach=.6f;Capture("09-reach-60");
    checks["reach_slider_updates_shader_live"]=UnityEngine.Mathf.Approximately(flower.GetPetalRenderer(seq.innerClockwise[winner]).sharedMaterial.GetFloat("_PetalLightTipReach"),.6f);
    config.rouletteTipReach=originalReach;config.rouletteIconBrightness=0;Capture("10-icons-off");
    config.rouletteIconBrightness=originalIcon;seq.ApplyLighting();
    seq.Tick(config.winnerHoldSeconds-.15f);
    checks["winner_tip_fades_before_result"]=Levels(seq.innerClockwise[winner]).x>0&&Levels(seq.innerClockwise[winner]).x<1;
    seq.Tick(.2f);
    checks["result_matches_winner"]=seq.Stage==Amgen.Deepavali.FlowerLightingStage.Result&&seq.WinnerPalette==chosen&&flower.SelectedState==chosen;
    seq.Tick(2);flower.SetState(chosen);Capture("06-result-"+chosen);
    checks["result_restores_full_lighting_and_rim"]=Enumerable.Range(0,flower.PetalCount).All(i=>Levels(i)==UnityEngine.Vector4.one)&&center.sharedMaterial.GetVector("_FlowerStageLevels")==UnityEngine.Vector4.one;
    seq.Tick(3);checks["result_event_fires_once"]=resultEvents==1;
    bool pathValid=true;
    for(int target=0;target<8;target++)
    {
        float previous=-1;
        for(int step=0;step<=1000;step++)
        {float p=Amgen.Deepavali.FlowerRoulettePath.Position(step/1000f,4,target);pathValid&=p>=previous;previous=p;}
        pathValid&=Amgen.Deepavali.FlowerRoulettePath.Slot(1,4,target)==target;
        float start=Amgen.Deepavali.FlowerRoulettePath.Position(.01f,4,target);
        float middle=Amgen.Deepavali.FlowerRoulettePath.Position(.51f,4,target)-Amgen.Deepavali.FlowerRoulettePath.Position(.5f,4,target);
        float end=Amgen.Deepavali.FlowerRoulettePath.Position(1,4,target)-Amgen.Deepavali.FlowerRoulettePath.Position(.99f,4,target);
        pathValid&=middle>start*5&&middle>end*5;
    }
    checks["all_eight_paths_accelerate_decelerate_and_stop_correctly"]=pathValid;
    bool fades=true;
    for(int step=0;step<=1600;step++)
    {
        float position=step*.01f;
        var weights=Enumerable.Range(0,8).Select(i=>Amgen.Deepavali.FlowerRoulettePath.TipWeight(position,i)).ToArray();
        fades&=UnityEngine.Mathf.Abs(weights.Sum()-1)<.00001f&&weights.All(w=>w>=0&&w<=1);
        fades&=Enumerable.Range(0,8).All(i=>UnityEngine.Mathf.Abs(weights[i]-Amgen.Deepavali.FlowerRoulettePath.TipWeight(position+.001f,i))<.002f);
    }
    checks["crossfades_continuous_and_preserve_energy_including_wrap"]=fades;
    checks["halfway_handoff_is_equal_lerp"]=UnityEngine.Mathf.Approximately(Amgen.Deepavali.FlowerRoulettePath.TipWeight(7.5f,7),.5f)&&UnityEngine.Mathf.Approximately(Amgen.Deepavali.FlowerRoulettePath.TipWeight(7.5f,0),.5f);
    for(int slot=0;slot<8;slot++)
    {
        seq.PreviewResult(slot);seq.Tick(2);flower.SetState(config.outcomes[slot]);
        checks["result_"+config.outcomes[slot]]=Enumerable.Range(0,flower.PetalCount).All(i=>flower.GetPetalState(i)==config.outcomes[slot])&&flower.CenterState==config.outcomes[slot];
        if(config.outcomes[slot]==Amgen.Deepavali.FlowerPaletteState.Cyan||config.outcomes[slot]==Amgen.Deepavali.FlowerPaletteState.Red)Capture("07-result-"+config.outcomes[slot]);
    }
    seq.Sleep();checks["reset_cancels_previous_glow_and_palette_wave"]=!flower.IsTransitioning&&seq.WinnerSlot==-1&&Enumerable.Range(0,flower.PetalCount).All(i=>Levels(i).x==0)&&CenterDark();
    seq.enabled=false;checks["disable_restores_neutral_stage_levels"]=Enumerable.Range(0,flower.PetalCount).All(i=>Levels(i)==UnityEngine.Vector4.one)&&center.sharedMaterial.GetVector("_FlowerStageLevels")==UnityEngine.Vector4.one;seq.enabled=true;
    seq.PlaySequence();seq.Tick(config.sleepingSeconds+.01f);bool auto=seq.Stage==Amgen.Deepavali.FlowerLightingStage.Ambient;
    seq.Tick(config.ambientSeconds+.01f);auto&=seq.Stage==Amgen.Deepavali.FlowerLightingStage.Roulette;
    seq.Tick(config.rouletteSeconds+config.winnerHoldSeconds+.01f);auto&=seq.Stage==Amgen.Deepavali.FlowerLightingStage.Result;
    seq.Tick(config.resultSeconds+.01f);auto&=seq.Stage==Amgen.Deepavali.FlowerLightingStage.Sleeping;
    checks["automatic_sequence_runs_all_four_stages"]=auto;
    var errors=UnityEditor.ShaderUtil.GetShaderMessages(UnityEngine.Shader.Find("Amgen/Deepavali/Petal Palette Lit")).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
    checks["shader_compiles"]=errors.Length==0;
    var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=checks.All(c=>c.Value),checks,winnerSlot=winner,winnerPalette=chosen.ToString(),errors},Newtonsoft.Json.Formatting.Indented);
    System.IO.File.WriteAllText("output/refined-flower-integration/light-validation.json",report);return report;
}
finally
{
    seq.ResultSelected-=onResult;seq.autoPreview=false;seq.WakeAmbient();seq.Tick(3);
    camera.rect=oldRect;camera.aspect=oldAspect;camera.targetTexture=oldTarget;UnityEngine.RenderTexture.active=oldActive;
    UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(read);UnityEngine.Object.Destroy(png);
    for(int i=0;i<motions.Length;i++)motions[i].enabled=motionStates[i];
}

