if(!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required");
var seq=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalWheelLightSequence>();
var checks=new System.Collections.Generic.Dictionary<string,bool>();
void Check(string name,bool value){checks[name]=value;}
var t=new Amgen.Deepavali.PetalWheelTiming();
Check("mockup_default_timings",t.fadeIn==.16f&&t.innerAfterglow==.45f&&t.outerAfterglow==.42f&&t.pulseLength==.8f&&t.settleLength==1.6f&&t.breathPeriod==4.5f&&t.bloomDelay==1.4f&&t.bloomSpread==1.8f);
Check("linear_attack",UnityEngine.Mathf.Abs(Amgen.Deepavali.PetalWheelPattern.Approach(0,1,.08f,.16f,.45f)-.5f)<.0001f);
Check("linear_release",UnityEngine.Mathf.Abs(Amgen.Deepavali.PetalWheelPattern.Approach(1,0,.225f,.16f,.45f)-.5f)<.0001f);
Check("zero_fades_snap",Amgen.Deepavali.PetalWheelPattern.Approach(0,1,.00001f,0,.45f)==1&&Amgen.Deepavali.PetalWheelPattern.Approach(1,0,.00001f,.16f,0)==0);
var p=new Amgen.Deepavali.PetalWheelPattern(t,15);
p.Tick(2);Check("independent_shimmer",p.Inner.Max()-p.Inner.Min()>.1f&&p.Outer.Max()-p.Outer.Min()>.1f&&p.Hub>0&&p.CenterIcon>0);
p.EnterWheel();p.Tick(.4f);Check("pulse_half_brightness",p.Inner.All(v=>UnityEngine.Mathf.Abs(v-.53f)<.001f)&&p.Outer.All(v=>UnityEngine.Mathf.Abs(v-.53f)<.001f));
Check("busy_entry_rejects_retrigger",!p.Spin(3)&&!p.ReturnToShimmer());
p.Tick(.41f);Check("pulse_enters_settle",p.Mode==Amgen.Deepavali.PetalWheelMode.Settle);
p.Tick(1.61f);Check("settle_enters_ready",p.Mode==Amgen.Deepavali.PetalWheelMode.Ready);
p.Tick(2);Check("ready_center_icon_dark",p.CenterIcon==0&&p.Hub>.1f);
bool winners=true,allBounds=true,stages=true;
foreach(bool tease in new[]{false,true})foreach(bool windup in new[]{false,true})for(int slot=0;slot<8;slot++)
{
 var timing=new Amgen.Deepavali.PetalWheelTiming{overshoot=tease,windup=windup,afterHold=Amgen.Deepavali.PetalWheelAfterHold.Stay};
 var model=new Amgen.Deepavali.PetalWheelPattern(timing,15);model.Spin(slot);
 var phases=new System.Collections.Generic.HashSet<string>();
 for(int k=0;k<1800&&model.Mode!=Amgen.Deepavali.PetalWheelMode.Win;k++)
 {model.Tick(1f/120);phases.Add(model.Phase);allBounds&=model.Inner.Concat(model.Outer).All(v=>float.IsFinite(v)&&v>=0&&v<=1);}
 winners&=model.Winner==slot&&model.Active==slot&&model.Mode==Amgen.Deepavali.PetalWheelMode.Win;
 stages&=phases.Contains("Spin-up")&&phases.Contains("Cruise")&&phases.Contains("Slowing")&&(!windup||phases.Contains("Wind-up"));
}
Check("all_32_target_landing_combinations",winners);Check("spin_phases_and_bounds",stages&&allBounds);
p=new Amgen.Deepavali.PetalWheelPattern(t,15);p.PreviewWin(2);p.Tick(1.3f);Check("bloom_waits_for_winner",p.Bloom==0);p.Tick(1);Check("bloom_midpoint",UnityEngine.Mathf.Abs(p.Bloom-.5f)<.02f);p.Tick(4.5f);Check("returns_to_ready",p.Mode==Amgen.Deepavali.PetalWheelMode.Ready);p.Tick(1.5f);Check("result_fades_out",p.BloomAlpha==0);
bool options=true;
foreach(Amgen.Deepavali.PetalWheelWinPattern pattern in System.Enum.GetValues(typeof(Amgen.Deepavali.PetalWheelWinPattern)))
{var option=new Amgen.Deepavali.PetalWheelTiming{winPattern=pattern,bloom=false,afterHold=Amgen.Deepavali.PetalWheelAfterHold.Stay};var model=new Amgen.Deepavali.PetalWheelPattern(option,15);model.PreviewWin(4);model.Tick(3);options&=model.Outer.All(v=>float.IsFinite(v)&&v>=0&&v<=1);}
Check("all_win_patterns_valid_on_15_petals",options);
Check("legacy_controller_disabled",!seq.GetComponent<Amgen.Deepavali.FlowerLightSequence>().enabled&&!seq.GetComponent<Amgen.Deepavali.FlowerSequencePreview>().enabled);
var flower=seq.flower;seq.autoPreview=false;seq.ResetPreview();seq.Tick(2);
UnityEngine.Vector4 Levels(int i)=>flower.GetPetalRenderer(i).sharedMaterial.GetVector("_FlowerStageLevels");
var data=new UnityEditor.SerializedObject(flower);var center=(UnityEngine.Renderer)data.FindProperty("centerDisc").objectReferenceValue;
Check("live_shimmer_all_layers",Enumerable.Range(0,flower.PetalCount).All(i=>Levels(i).x>0)&&center.sharedMaterial.GetVector("_FlowerStageLevels").x>0);
var camera=UnityEngine.Camera.main;var oldRect=camera.rect;var oldAspect=camera.aspect;var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;
var rt=UnityEngine.RenderTexture.GetTemporary(1200,1000,24,UnityEngine.RenderTextureFormat.ARGBHalf,UnityEngine.RenderTextureReadWrite.Linear);
var read=new UnityEngine.Texture2D(1200,1000,UnityEngine.TextureFormat.RGBAFloat,false,true);var png=new UnityEngine.Texture2D(1200,1000,UnityEngine.TextureFormat.RGB24,false);
void Capture(string name){seq.ApplyLighting();camera.Render();UnityEngine.RenderTexture.active=rt;read.ReadPixels(new UnityEngine.Rect(0,0,1200,1000),0,0);read.Apply();png.SetPixels(read.GetPixels().Select(c=>c.gamma).ToArray());png.Apply();System.IO.File.WriteAllBytes("output/refined-flower-integration/baseline-"+name+".png",png.EncodeToPNG());}
try
{
 camera.rect=new UnityEngine.Rect(0,0,1,1);camera.aspect=1.2f;camera.targetTexture=rt;
 Capture("01-shimmer");seq.EnterWheel();seq.Tick(.4f);Capture("02-entry-pulse");seq.Tick(.41f+1.61f);seq.Tick(1);Capture("03-ready");
 seq.Spin(6);seq.Tick(2);Capture("04-spin");
 Check("inner_spin_preserves_tip_edge_mask",seq.innerClockwise.All(i=>{var m=flower.GetPetalRenderer(i).sharedMaterial;return m.GetVector("_PetalLightTravel").z==0&&m.GetVector("_PetalLightStyle").y==0&&m.GetFloat("_PetalLightTipReach")==seq.style.rouletteTipReach&&m.GetFloat("_PetalLightIconBrightness")==seq.style.rouletteIconBrightness&&m.GetFloat("_PatternLightHandoff")==0&&Levels(i).y==0;}));
 Check("outer_breathes_during_spin",seq.outerClockwise.All(i=>Levels(i).x>0)&&seq.outerClockwise.Select(i=>Levels(i).x).Max()-seq.outerClockwise.Select(i=>Levels(i).x).Min()>.05f);
 for(int n=0;n<1000&&seq.Pattern.Mode!=Amgen.Deepavali.PetalWheelMode.Win;n++)seq.Tick(1f/120);
 Check("live_selected_winner",seq.Pattern.Winner==6&&seq.Pattern.Active==6);seq.Tick(.3f);Capture("05-winner");
 seq.Tick(2);Capture("06-bloom");Check("smooth_existing_style_handoff",flower.GetPetalRenderer(0).sharedMaterial.GetFloat("_PatternLightHandoff")>0&&flower.GetPetalRenderer(0).sharedMaterial.GetFloat("_PatternLightHandoff")<1);
 seq.Tick(1);Capture("07-result");Check("palette_reveal_completed",flower.TransitionProgress==1&&flower.GetPetalRenderer(0).sharedMaterial.GetFloat("_PetalLightEnabled")==0);
 seq.Tick(5);Check("return_resets_startup_palette",flower.IsUsingStartupPalette(0)&&flower.GetPetalRenderer(0).sharedMaterial.GetFloat("_PatternPaletteAlpha")==1);Capture("08-return");
 seq.Spin(1);for(int n=0;n<1600&&seq.Pattern.Mode!=Amgen.Deepavali.PetalWheelMode.Win;n++)seq.Tick(1f/120);seq.Tick(3.3f);
 Check("second_spin_reveals_new_palette",seq.Pattern.Winner==1&&flower.SelectedState==seq.style.outcomes[1]&&flower.TransitionProgress==1);
 seq.PreviewResult(7);seq.Tick(3.3f);Check("result_preview_replaces_prior_result",flower.SelectedState==seq.style.outcomes[7]);
}
finally
{camera.rect=oldRect;camera.aspect=oldAspect;camera.targetTexture=oldTarget;UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(read);UnityEngine.Object.Destroy(png);seq.ResetPreview();}
var json=Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText("output/refined-flower-integration/baseline-validation.json",json);
return new{passed=checks.Count(kv=>kv.Value),total=checks.Count,failures=checks.Where(kv=>!kv.Value).Select(kv=>kv.Key).ToArray()};

