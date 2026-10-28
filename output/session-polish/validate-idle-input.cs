var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();var r=s.restoration;
var checks=new System.Collections.Generic.Dictionary<string,bool>();var errors=new System.Collections.Generic.List<string>();
bool oldMouse=r.useMouseInput;float timeout=s.inactivityTimeout,fadeDuration=s.inactivityFadeDuration;
void Check(string n,bool ok){checks[n]=ok;if(!ok)throw new System.Exception(n);}
s.endSequence();r.useMouseInput=false;s.inactivityTimeout=1000;
int stage=0,frame=Time.frameCount;double start=Time.realtimeSinceStartupAsDouble,at=start;float elapsed=0;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 bool done=false;
 try{
  if(!Application.isPlaying)throw new System.Exception("Play Mode ended");if(frame==Time.frameCount)return;frame=Time.frameCount;
  double now=Time.realtimeSinceStartupAsDouble,age=now-at;bool next=false;
  if(now-start>12)throw new System.Exception("Timeout stage "+stage);
  switch(stage){
   case 0:r.SetPointerViewport(new Vector2(.5f,.96f));next=true;break;
   case 1:r.SetPointerViewport(new Vector2(.5f,.96f));if(age<.4)break;Check("Repeated stationary sensor samples do not refresh idle",s.InactivityElapsed>.3);elapsed=s.InactivityElapsed;next=true;break;
   case 2:r.SetPointerViewport(new Vector2(.5f+(Time.frameCount%2==0?.0001f:0),.96f));if(age<.3)break;Check("Sub-pixel sensor jitter does not refresh idle",s.InactivityElapsed>elapsed+.2f);r.SetPointerViewport(new Vector2(.55f,.96f));next=true;break;
   case 3:Check("Meaningful movement refreshes idle",s.InactivityElapsed<.1f);next=true;break;
   case 4:if(age<.35)break;Check("Stale tracking expires without faking activity",!r.HasPointer&&s.InactivityElapsed>.3f);r.SetPointerViewport(new Vector2(.55f,.96f));next=true;break;
   case 5:Check("Tracking reacquisition refreshes idle",r.HasPointer&&s.InactivityElapsed<.1f);next=true;break;
   case 6:r.SetPointerViewport(new Vector2(.5f,.5f));if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.Restoring)break;r.ClearPointer();s.inactivityTimeout=.12f;s.inactivityFadeDuration=.6f;s.RegisterActivity();next=true;break;
   case 7:if(s.InactivityFadeAlpha<.1f)break;Check("Explicit reset can interrupt a gradual return",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.ReturningToBase);s.endSequence();next=true;break;
   case 8:if(age<.8)break;Check("Reset cancels pending fade and leaves reusable base",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration&&s.InactivityFadeAlpha==0&&r.Coverage==0&&!s.sequence.Paused&&!s.handPrompt.IsDismissing);done=true;break;
  }
  if(next){stage++;at=now;}
 }catch(System.Exception e){errors.Add(e.ToString());done=true;}
 finally{if(done){UnityEditor.EditorApplication.update-=tick;r.useMouseInput=oldMouse;s.inactivityTimeout=timeout;s.inactivityFadeDuration=fadeDuration;s.endSequence();var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0,checks,errors},Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText("output/session-polish/idle-input-validation.json",json);UnityEditor.SessionState.SetString("Polish.IdleInput",json);}}
};UnityEditor.EditorApplication.update+=tick;return "Started sensor activity and fade cancellation checks";
