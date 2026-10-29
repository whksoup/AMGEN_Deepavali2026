// Run via eval_file in the saved integrated scene's Play Mode. Actual VideoPlayers, forced result only.
var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
if(!Application.isPlaying||s==null||s.greetingOverlay==null)throw new System.Exception("Play the wired scene first");
var g=s.greetingOverlay;var r=s.restoration;
var phase=typeof(Amgen.Deepavali.DeepavaliSessionController).GetProperty("Phase");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var checks=new System.Collections.Generic.Dictionary<string,bool>();var errors=new System.Collections.Generic.List<string>();
var timeline=new System.Collections.Generic.List<object>();
var saved=JsonUtility.ToJson(s.settingsPreset);bool background=Application.runInBackground;
bool mouse=r.useMouseInput;var touch=s.GetComponent<Amgen.Deepavali.DeepavaliTouchInput>();bool touchEnabled=touch.enabled;
Application.runInBackground=true;r.useMouseInput=false;touch.enabled=false;
void Check(string n,bool p){checks[n]=p;if(!p)throw new System.Exception(n);}
void Clean(string n){Check(n,g.CurrentState==HappyDeepavaliOverlay.State.Hidden&&!g.GetComponent<UnityEngine.UI.RawImage>().enabled&&!g.logo.enabled&&g.GetComponents<UnityEngine.Video.VideoPlayer>().All(v=>!v.isPlaying));}
Application.LogCallback log=(m,st,t)=>{if((t==LogType.Error||t==LogType.Exception)&&!m.Contains("injected validation error"))errors.Add(m);};
Application.logMessageReceived+=log;
int run=0,frame=-1;double start=Time.realtimeSinceStartupAsDouble,at=start,holdAt=0;string last="";
bool captured=false,sawClosing=false,sawGreeting=false;
void BeginResult(){s.EndSequence();s.handPrompt.CancelAndHide();r.CompleteRestoration();s.sequence.PreviewResult(0);phase.SetValue(s,Amgen.Deepavali.DeepavaliSessionPhase.Spinning);s.inactivityTimeout=180;s.scrollHoldSeconds=run==0?15:.2f;s.scrollDelayAfterResultSeconds=.1f;s.greetingHoldSeconds=run==0?4:.2f;at=Time.realtimeSinceStartupAsDouble;sawClosing=sawGreeting=false;}
Check("Saved greeting default",s.settingsPreset.greetingHoldSeconds==4);
Check("Externally owned noninteractive overlay",!g.showOnStart&&!g.toggleOnMouseClick&&!g.logo.raycastTarget&&!g.GetComponent<UnityEngine.UI.RawImage>().raycastTarget);
Check("Layer above scroll below fade",g.GetComponentInParent<Canvas>().sortingOrder>s.scrollOverlay.GetComponentInParent<Canvas>().sortingOrder&&g.GetComponentInParent<Canvas>().sortingOrder<32760);
Clean("Initially hidden");BeginResult();
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 bool done=false;
 try{
  if(!Application.isPlaying)throw new System.Exception("Play stopped");
  if(frame==Time.frameCount)return;frame=Time.frameCount;
  double now=Time.realtimeSinceStartupAsDouble;
  if(now-start>200)throw new System.Exception("Harness timeout: "+run+" "+s.Phase);
  string state=run+"/"+s.Phase+"/"+g.CurrentState;
  if(state!=last){last=state;timeline.Add(new{seconds=now-start,state,inactive=s.InactivityElapsed});}
  // Expire the idle clock only after the bounded presentation has started.
  if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.ScrollOpening)s.inactivityTimeout=.1f;
  if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.ScrollClosing){sawClosing=true;Clean("Greeting hidden through scroll closing "+run);}
  if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.GreetingOpening){
   sawGreeting=true;Check("Greeting starts after scroll hidden "+run,sawClosing&&s.scrollOverlay.CurrentState==DiwaliScrollOverlay.State.Hidden&&s.resultLogo.alpha==0);
   Check("Rangoli remains selected "+run,s.sequence.SelectedOutcome!=null&&s.sequence.IsResultFullyVisible&&r.Phase==Amgen.Deepavali.RestorationPhase.Idle);
   if(run==1){s.EndSequence();Clean("Reset during writing");run++;BeginResult();return;}
  }
  if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.GreetingHolding){
   Check("Idle reached and alpha image visible "+run,g.CurrentState==HappyDeepavaliOverlay.State.Shown&&g.GetComponent<UnityEngine.UI.RawImage>().enabled&&g.logo.enabled);
   if(run==0&&!captured){captured=true;holdAt=now;ScreenCapture.CaptureScreenshot("output/happy-deepavali/greeting-over-rangoli.png");}
   if(run==2){s.enabled=false;Clean("Disable during greeting");s.enabled=true;run++;BeginResult();return;}
   if(run==3){typeof(HappyDeepavaliOverlay).GetMethod("OnVideoError",flags).Invoke(g,new object[]{g.GetComponents<UnityEngine.Video.VideoPlayer>()[0],"injected validation error"});Clean("Injected error clears overlay");}
  }
  if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.GreetingClosing&&run==0)Check("Full idle hold elapsed",now-holdAt>=3.9);
  if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration&&sawGreeting){
   Clean("Final reset clears all greeting players "+run);Check("Reset restores dark opening "+run,r.Coverage==0&&!r.paintingEnabled);
   if(run==0){Check("Expired inactivity did not interrupt finale",sawClosing&&captured);run++;BeginResult();return;}
   if(run==3){done=true;}
  }
  UnityEditor.SessionState.SetString("Greeting.Validation",last+" checks="+checks.Count);
 }catch(System.Exception e){errors.Add(e.ToString());done=true;}
 if(!done)return;
 UnityEditor.EditorApplication.update-=tick;Application.logMessageReceived-=log;
 checks["Saved preset unchanged"]=saved==JsonUtility.ToJson(s.settingsPreset);
 s.settingsPreset.ApplyTo(s);s.EndSequence();touch.enabled=touchEnabled;r.useMouseInput=mouse;Application.runInBackground=background;
 System.IO.File.WriteAllText("output/happy-deepavali/runtime-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0&&checks.Values.All(v=>v),checks,errors,timeline},Newtonsoft.Json.Formatting.Indented));
 UnityEditor.SessionState.SetString("Greeting.Validation","Complete passed="+(errors.Count==0));
};
UnityEditor.EditorApplication.update+=tick;
return "Started finale validation; forced result, real video frames.";
