// Run with Unity eval_file in Play Mode. Real frames/video; simulated viewport input.
var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
if(!Application.isPlaying||s==null)throw new System.Exception("Play integrated scene first");
var r=s.restoration;var touch=r.GetComponent<Amgen.Deepavali.DeepavaliTouchInput>();
bool touchEnabled=touch.enabled,mouseEnabled=r.useMouseInput,background=Application.runInBackground;
var ui=s.GetComponent<Amgen.Deepavali.DeepavaliSessionDebugUI>();
var checks=new System.Collections.Generic.Dictionary<string,bool>();var errors=new System.Collections.Generic.List<string>();
var timeline=new System.Collections.Generic.List<object>();
var phase=typeof(Amgen.Deepavali.DeepavaliSessionController).GetProperty("Phase");
var saved=JsonUtility.ToJson(s.settingsPreset);
var shared=JsonUtility.ToJson(s.sequence.settingsPreset);
void Check(string name,bool pass){checks[name]=pass;if(!pass)throw new System.Exception(name);}
void Capture(string name)=>ScreenCapture.CaptureScreenshot("output/presentation-controls/"+name+".png");
Application.LogCallback logger=(m,st,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};
Application.logMessageReceived+=logger;
Application.runInBackground=true;touch.enabled=false;r.useMouseInput=false;ui.showControls=false;
s.EndSequence();s.cameraPitchOffset=-15;s.spinSpeed=8;s.spinCruiseSeconds=8;s.spinSlowdownSeconds=6;s.inactivityTimeout=180;s.scrollHoldSeconds=3;
s.ApplyPresentationSettings();
Check("Saved defaults retained",s.settingsPreset.cameraPitchOffset==0&&s.settingsPreset.spinSpeed==16&&s.settingsPreset.spinCruiseSeconds==1.2f&&s.settingsPreset.spinSlowdownSeconds==3.6f);
Check("Logo is a noninteractive screen-space image",s.resultLogo!=null&&!s.resultLogo.blocksRaycasts&&!s.resultLogo.interactable&&!s.resultLogo.GetComponent<UnityEngine.UI.Image>().raycastTarget&&s.resultLogo.GetComponentInParent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay);
for(int slot=0;slot<8;slot++)
{
 var t=new Amgen.Deepavali.PetalWheelTiming {speed=8,cruise=8,slowdown=6};
 var p=new Amgen.Deepavali.PetalWheelPattern(t,14);p.EnterWheel();p.Tick(2.5f);Check("Spin accepts slot "+slot,p.Spin(slot));
 float duration=p.PlannedDuration;Check("Extended duration bound "+slot,duration>=14.9f&&duration<15.91f);
 t.speed=24;t.cruise=0;t.slowdown=.2f;Check("Active spin duration is snapshotted "+slot,p.PlannedDuration==duration);
 p.Tick(duration+.05f);Check("Exact winner after changed live settings "+slot,p.Mode==Amgen.Deepavali.PetalWheelMode.Win&&p.Winner==slot&&Mathf.Abs(p.Position-slot-.5f-8*Mathf.Round((p.Position-slot-.5f)/8))<.001f);
}
int stage=0,run=0,frame=Time.frameCount,point=0;double started=Time.realtimeSinceStartupAsDouble,at=started;
bool sawPartial=false;string last="";Vector2 pointer=new Vector2(.5f,.97f);bool feed=true;
var points=new System.Collections.Generic.List<Vector2>();
for(int y=0;y<7;y++)for(int x=0;x<17;x++)points.Add(new Vector2(.02f+.96f*(y%2==0?x:16-x)/16,.02f+.96f*y/6));
void BeginResult(int slot){s.EndSequence();s.handPrompt.CancelAndHide();s.restoration.CompleteRestoration();s.sequence.PreviewResult(slot);phase.SetValue(s,Amgen.Deepavali.DeepavaliSessionPhase.Spinning);feed=false;r.ClearPointer();s.RegisterActivity();s.inactivityTimeout=180;stage=5;sawPartial=false;at=Time.realtimeSinceStartupAsDouble;}
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
 bool done=false;
 try
 {
  if(!Application.isPlaying)throw new System.Exception("Play Mode ended");
  if(frame==Time.frameCount)return;frame=Time.frameCount;
  double now=Time.realtimeSinceStartupAsDouble,age=now-at;
  if(now-started>155)throw new System.Exception("Timeout "+run+"/"+stage+" "+s.Phase);
  if(feed)r.SetPointerViewport(pointer,true);
  string state=run+"/"+s.Phase+"/"+s.scrollOverlay.CurrentState;
  if(last!=state){last=state;timeline.Add(new{seconds=now-started,state,alpha=s.resultLogo.alpha});}
  switch(stage)
  {
   case 0:
    if(age<1.5||s.handPrompt.IsAppearing)break;
    pointer=new Vector2(.5f,.5f);stage=1;break;
   case 1:
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.Restoring)break;
    Check("Center dwell still starts restoration at changed pitch",s.DwellProgress==0);stage=2;at=now;break;
   case 2:
    if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingSpin){pointer=new Vector2(.5f,.97f);stage=3;at=now;break;}
    if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.Restoring&&age>.08){pointer=points[point++%points.Count];at=now;}
    break;
   case 3:
    if(age<1.2||s.handPrompt.IsAppearing)break;
    Check("Coverage and all four lamps complete at changed pitch",r.Coverage>=.8f&&s.lamps.LitLampCount==4);
    pointer=new Vector2(.5f,.5f);stage=4;break;
   case 4:
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.Spinning)break;
    Check("Integrated next spin uses extended settings",s.sequence.Pattern.PlannedDuration>=14.9f&&s.sequence.Pattern.PlannedDuration<15.91f);
    Capture("pitched-spin");feed=false;r.ClearPointer();stage=5;break;
   case 5:
    if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.ScrollOpening)
    {
     Check("Logo remains hidden during opening "+run,s.resultLogo.alpha==0);
     if(run==2){s.EndSequence();Check("Reset during opening hides logo",s.resultLogo.alpha==0&&s.scrollOverlay.CurrentState==DiwaliScrollOverlay.State.Hidden);run++;BeginResult(run);break;}
    }
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.ScrollHolding)break;
    Check("Exact outcome scroll and completed reveal "+run,s.sequence.IsResultFullyVisible&&s.SelectedScrollVariant==s.sequence.SelectedOutcome.scrollVariant);
    Check("Logo starts transparent after opening "+run,s.resultLogo.alpha<.1f);stage=6;at=now;break;
   case 6:
    if(s.resultLogo.alpha>.05f&&s.resultLogo.alpha<.95f)sawPartial=true;
    if(age<1.2)break;
    Check("Logo fades progressively to opaque "+run,sawPartial&&s.resultLogo.alpha==1);
    if(run==0){Capture("logo-below-scroll");stage=7;break;}
    if(run==1){s.EndSequence();Check("Reset during hold hides logo",s.resultLogo.alpha==0);run++;BeginResult(run);break;}
    if(run==3){s.enabled=false;Check("Disable immediately hides logo",s.resultLogo.alpha==0);s.enabled=true;Check("Reenable keeps logo hidden",s.resultLogo.alpha==0);run++;BeginResult(run);break;}
    if(run==4){s.scrollOverlay.CancelImmediate();stage=8;break;}
    if(run==5){s.inactivityTimeout=.1f;s.inactivityFadeDuration=.2f;stage=9;break;}
    break;
   case 7:
    if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.ScrollClosing)Check("Logo hidden throughout closing",s.resultLogo.alpha==0);
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration)break;
    Check("Normal round resets logo and content",s.resultLogo.alpha==0&&r.Coverage==0&&s.scrollOverlay.CurrentState==DiwaliScrollOverlay.State.Hidden);
    s.cameraPitchOffset=0;s.ApplyPresentationSettings();run++;BeginResult(run);break;
   case 8:
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.HoldingResult)break;
    Check("Playback cancellation fallback hides logo",s.resultLogo.alpha==0);run++;BeginResult(run);break;
   case 9:
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration)break;
    Check("Inactivity reset clears logo and scroll",s.resultLogo.alpha==0&&s.scrollOverlay.CurrentState==DiwaliScrollOverlay.State.Hidden);
    done=true;break;
  }
  UnityEditor.SessionState.SetString("Presentation.Validation",run+"/"+stage+" "+s.Phase+" checks="+checks.Count);
 }
 catch(System.Exception e){errors.Add(e.ToString());done=true;}
 if(!done)return;
 UnityEditor.EditorApplication.update-=tick;Application.logMessageReceived-=logger;
 Check("Shared wheel preset unchanged",shared==JsonUtility.ToJson(s.sequence.settingsPreset));
 Check("Saved session asset unchanged by live tuning",saved==JsonUtility.ToJson(s.settingsPreset));
 s.settingsPreset.ApplyTo(s);s.EndSequence();touch.enabled=touchEnabled;r.useMouseInput=mouseEnabled;Application.runInBackground=background;
 System.IO.File.WriteAllText("output/presentation-controls/runtime-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0,checks,errors,timeline},Newtonsoft.Json.Formatting.Indented));
 UnityEditor.SessionState.SetString("Presentation.Validation","Complete; passed="+(errors.Count==0));
};
UnityEditor.EditorApplication.update+=tick;
return "Started frame-driven presentation validation";
