// Natural public input reaches every phase; shortened runtime timings isolate interruption handling.
var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
if(!Application.isPlaying||s==null)throw new System.Exception("Play main scene first");
var r=s.restoration;var overlay=s.scrollOverlay;var timing=s.sequence.timing;
bool mouse=r.useMouseInput;float completion=r.completionDuration;
var durationField=typeof(Amgen.Deepavali.HandHereInteraction).GetField("disintegrateDuration",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
float handDuration=(float)durationField.GetValue(s.handPrompt);
var saved=ScriptableObject.CreateInstance<Amgen.Deepavali.DeepavaliSessionSettings>();saved.CaptureFrom(s);
var targets=new[]{"DismissingRestorationPrompt","Restoring","FinalReveal","AwaitingSpin","DismissingSpinPrompt","EnteringWheel","Spinning","RevealingResult","HoldingResult","ScrollDelay","ScrollOpening","ScrollHolding","ScrollClosing"};
var checks=new System.Collections.Generic.Dictionary<string,bool>();var errors=new System.Collections.Generic.List<string>();
Application.LogCallback logger=(m,st,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=logger;
void Check(string n,bool ok){checks[n]=ok;if(!ok)throw new System.Exception(n);}
void BeginCase(){
 s.scrollOverlay=overlay;s.endSequence();r.useMouseInput=false;r.completionDuration=.5f;
 s.openingDwellDuration=s.spinDwellDuration=.12f;s.inactivityTimeout=1000;s.inactivityFadeDuration=.4f;
 s.scrollDelayAfterResultSeconds=.5f;s.scrollHoldSeconds=.5f;s.coloredHoldDuration=10;
 durationField.SetValue(s.handPrompt,.3f);
 s.sequence.timing=JsonUtility.FromJson<Amgen.Deepavali.PetalWheelTiming>(JsonUtility.ToJson(timing));
 s.sequence.timing.pulseLength=.25f;s.sequence.timing.settleLength=.25f;s.sequence.timing.cruise=0;s.sequence.timing.slowdown=.4f;
 s.sequence.timing.bloomDelay=.2f;s.sequence.timing.bloomSpread=.4f;s.sequence.ResetPreview();
}
int index=0,stage=0,frame=Time.frameCount;bool fade=false,black=false;double start=Time.realtimeSinceStartupAsDouble,at=start;
BeginCase();
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 bool done=false;
 try{
  if(!Application.isPlaying)throw new System.Exception("Play Mode ended");
  if(frame==Time.frameCount)return;frame=Time.frameCount;double now=Time.realtimeSinceStartupAsDouble;
  if(now-start>190)throw new System.Exception("Timeout "+targets[index]+" phase "+s.Phase+" stage "+stage);
  string target=targets[index];
  if(stage==0){
   if(target=="HoldingResult")s.scrollOverlay=null;
   if(s.Phase.ToString()==target){s.inactivityTimeout=.12f;s.RegisterActivity();r.ClearPointer();at=now;stage=1;}
   else if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration||s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingSpin)
    r.SetPointerViewport(s.PromptArmed?new Vector2(.5f,.5f):new Vector2(.5f,.96f));
   else if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.Restoring)r.RevealPoint(new Vector2(.5f,.5f),4);
  }
  else if(stage==1){
   if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.ReturningToBase){
    Check(target+": timeout enters fade",now-at>=.1&&now-at<.4&&s.sequence.Paused&&!r.paintingEnabled);stage=2;at=now;
   }else if(s.Phase.ToString()!=target)throw new System.Exception("Target advanced before interruption: "+target+" to "+s.Phase);
  }
  else if(stage==2){
   if(s.InactivityFadeAlpha>0&&s.InactivityFadeAlpha<1)fade=true;
   if(s.InactivityFadeAlpha>=.99f&&r.Coverage==0&&s.lamps.LitLampCount==0)black=true;
   if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration){
    Check(target+": smooth fade and clean base",fade&&black&&now-at>=.38&&r.Coverage==0&&s.lamps.LitLampCount==0&&overlay.CurrentState==DiwaliScrollOverlay.State.Hidden&&s.handPrompt.IsAppearing&&!s.sequence.Paused&&s.DwellProgress==0);
    stage=3;at=now;
   }
  }
  else if(stage==3&&now-at>.45){
   Check(target+": no stale callback or repeated base fade",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration&&s.InactivityFadeAlpha==0&&!r.paintingEnabled);
   index++;if(index==targets.Length)done=true;
   else{fade=black=false;stage=0;BeginCase();}
  }
  UnityEditor.SessionState.SetString("Polish.Interruptions",Newtonsoft.Json.JsonConvert.SerializeObject(new{running=true,index,target,phase=s.Phase.ToString(),stage,checks=checks.Count}));
 }catch(System.Exception e){errors.Add(e.ToString());done=true;}
 finally{if(done){
  UnityEditor.EditorApplication.update-=tick;Application.logMessageReceived-=logger;
  s.scrollOverlay=overlay;s.sequence.timing=timing;r.useMouseInput=mouse;r.completionDuration=completion;durationField.SetValue(s.handPrompt,handDuration);saved.ApplyTo(s);UnityEngine.Object.Destroy(saved);s.endSequence();
  var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0,checks,errors},Newtonsoft.Json.Formatting.Indented);
  System.IO.File.WriteAllText("output/session-polish/interruption-validation.json",json);UnityEditor.SessionState.SetString("Polish.Interruptions",json);
 }}
};UnityEditor.EditorApplication.update+=tick;return "Started 13-phase interruption matrix";
