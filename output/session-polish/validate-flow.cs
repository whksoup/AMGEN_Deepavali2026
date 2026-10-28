// Real Input System mouse/keyboard events, rendered frames, production scroll and a real 30-second idle wait.
var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
if(!Application.isPlaying||s==null)throw new System.Exception("Play main scene first");
var r=s.restoration;var ui=s.GetComponent<Amgen.Deepavali.DeepavaliSessionDebugUI>();
var input=UnityEngine.InputSystem.InputSystem.settings;
var oldEditor=input.editorInputBehaviorInPlayMode;var oldBackground=input.backgroundBehavior;
var oldMouse=UnityEngine.InputSystem.Mouse.current;var oldKeyboard=UnityEngine.InputSystem.Keyboard.current;
bool oldRun=Application.runInBackground;
input.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
input.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;Application.runInBackground=true;
var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("PolishValidationMouse");mouse.MakeCurrent();
var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("PolishValidationKeyboard");keyboard.MakeCurrent();
var checks=new System.Collections.Generic.Dictionary<string,bool>();var errors=new System.Collections.Generic.List<string>();var timeline=new System.Collections.Generic.List<object>();
Application.LogCallback logger=(m,st,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=logger;
void Check(string n,bool ok){checks[n]=ok;if(!ok)throw new System.Exception(n);}
void Move(Vector2 uv){var rect=r.interactionCamera.pixelRect;UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=new Vector2(rect.x+uv.x*rect.width,rect.y+uv.y*rect.height)});}
void Key(bool down)=>UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,down?new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.M):new UnityEngine.InputSystem.LowLevel.KeyboardState());
void Capture(string name)=>ScreenCapture.CaptureScreenshot("output/session-polish/"+name+".png");
string savedSettings=JsonUtility.ToJson(s.settingsPreset);
int resultCount=0;System.Action<int,Amgen.Deepavali.FlowerPaletteState> result=(i,p)=>resultCount++;s.sequence.ResultSelected+=result;
var points=new System.Collections.Generic.List<Vector2>();
for(int y=0;y<7;y++)for(int x=0;x<17;x++)points.Add(new Vector2(.02f+.96f*(y%2==0?x:16-x)/16,.02f+.96f*y/6));
s.endSequence();r.useMouseInput=true;ui.showControls=false;Move(new Vector2(.5f,.96f));
var initialPattern=s.sequence.Pattern;
int stage=-1,frame=Time.frameCount,pointIndex=0;float heldCoverage=0,clockBefore=0;
double start=Time.realtimeSinceStartupAsDouble,at=start,readyAt=0,idleAt=0,timeoutAt=0,keepAliveAt=0;
bool sawFade=false,sawBlackReset=false;string oldPhase="";
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
 bool done=false;
 try
 {
  if(!Application.isPlaying)throw new System.Exception("Play Mode ended");
  if(Time.frameCount==frame)return;frame=Time.frameCount;
  double now=Time.realtimeSinceStartupAsDouble,age=now-at;bool next=false;
  if(now-start>170)throw new System.Exception("Timeout stage "+stage+" phase "+s.Phase);
  if(oldPhase!=s.Phase.ToString()){oldPhase=s.Phase.ToString();timeline.Add(new{time=now-start,phase=oldPhase,idle=s.InactivityElapsed,coverage=r.Coverage,clock=s.sequence.Pattern?.Clock});}
  switch(stage)
  {
   case -1:
    s.endSequence();initialPattern=s.sequence.Pattern;Move(new Vector2(.5f,.96f));next=true;break;
   case 0:
    if(age<.2)break;
    Check("Saved doubled brush and default inactivity load",r.brushRadius==.32f&&s.inactivityTimeout==30&&s.inactivityFadeDuration==2);
    Check("Dormant mask already contains live shimmer",s.sequence.enabled&&s.sequence.Pattern.Mode==Amgen.Deepavali.PetalWheelMode.Shimmer&&r.Coverage==0&&r.idleBehaviours.All(b=>!b.enabled));
    Check("Hand assembles with reverse dissolve",s.handPrompt.IsAppearing&&s.handPrompt.GetComponent<Amgen.Deepavali.HandHereGraphic>().material.GetFloat("_Dissolve")>0);
    Capture("01-hand-assembling");Move(new Vector2(.5f,.5f));next=true;break;
   case 1:
    if(age<.25)break;
    Check("Hover during appearance cannot start dwell or paint",s.handPrompt.IsAppearing&&s.DwellProgress==0&&r.Coverage==0);next=true;break;
   case 2:
    if(s.handPrompt.IsAppearing)break;
    readyAt=now;next=true;break;
   case 3:
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.DismissingRestorationPrompt)break;
    Check("Dwell starts after assembly and requires no click",now-readyAt>=.60&&!mouse.leftButton.isPressed&&r.Coverage==0);next=true;break;
   case 4:
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.Restoring)break;
    next=true;break;
   case 5:
    if(age<.8)break;
    Check("Large brush reveals beyond old radius",r.SampleRestoration(new Vector2(.5f+.23f/r.interactionCamera.aspect,.5f))>.1f);
    Check("Partial restoration renders shimmer without resetting it",ReferenceEquals(initialPattern,s.sequence.Pattern)&&s.sequence.Pattern.Mode==Amgen.Deepavali.PetalWheelMode.Shimmer);
    Capture("02-shimmer-restoration");Key(true);next=true;break;
   case 6:
    Check("M opens hidden debug menu",ui.showControls);Key(false);Move(new Vector2(.93f,.9f));next=true;break;
   case 7:
    if(age<.15)break;
    Check("Debug panel blocks the painting and dwell pointer",!r.HasPointer&&ui.ContainsScreenPoint(new Vector2(Screen.width*.93f,Screen.height*.9f)));
    heldCoverage=r.Coverage;Capture("03-debug-menu");next=true;break;
   case 8:
    if(age<.35)break;
    Check("Open untouched debug panel does not paint or refresh inactivity",r.Coverage==heldCoverage&&s.InactivityElapsed>.25f);
    r.brushRadius=.29f;s.inactivityTimeout=47;s.inactivityFadeDuration=3;
    Check("Settings save succeeds",s.SaveSettingsToPreset(out var message));
    string disk=System.IO.File.ReadAllText(UnityEditor.AssetDatabase.GetAssetPath(s.settingsPreset));
    Check("Brush and idle are persisted to disk",disk.Contains("restorationBrushRadius: 0.29")&&disk.Contains("inactivityTimeout: 47"));
    r.brushRadius=.1f;s.inactivityTimeout=1;s.inactivityFadeDuration=.1f;s.settingsPreset.ApplyTo(s);
    Check("Saved brush idle and fade round-trip",r.brushRadius==.29f&&s.inactivityTimeout==47&&s.inactivityFadeDuration==3);
    JsonUtility.FromJsonOverwrite(savedSettings,s.settingsPreset);UnityEditor.EditorUtility.SetDirty(s.settingsPreset);UnityEditor.AssetDatabase.SaveAssetIfDirty(s.settingsPreset);s.settingsPreset.ApplyTo(s);
    Key(true);next=true;break;
   case 9:
    Check("M closes debug menu",!ui.showControls);Key(false);Move(points[0]);next=true;break;
   case 10:
    if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.FinalReveal){clockBefore=s.sequence.Pattern.Clock;Capture("04-final-reveal");Move(new Vector2(.5f,.5f));next=true;break;}
    if(age<.12)break;
    if(++pointIndex>=points.Count)throw new System.Exception("Sweep failed to restore wall: "+r.Coverage);
    Move(points[pointIndex]);at=now;break;
   case 11:
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.AwaitingSpin)break;
    Check("Completion keeps the same Shimmer object and clock",ReferenceEquals(initialPattern,s.sequence.Pattern)&&s.sequence.Pattern.Clock>clockBefore&&s.sequence.Pattern.Mode==Amgen.Deepavali.PetalWheelMode.Shimmer);
    Check("Completion requires four candles and coverage",s.lamps.LitLampCount==4&&r.Coverage>=.8f);
    Check("Shimmer prompt begins assembly",s.handPrompt.IsAppearing);Capture("05-shimmer-arrival");next=true;break;
   case 12:
    if(age<1.2)break;
    Check("Respawned prompt does not activate on a stationary pointer",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingSpin&&s.DwellProgress==0&&!s.PromptArmed);
    Move(new Vector2(.5f,.96f));next=true;break;
   case 13:
    if(age<.12)break;Move(new Vector2(.5f,.5f));next=true;break;
   case 14:
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.Spinning)break;
    Check("Existing entry and randomized spin still run",s.sequence.Pattern.Mode==Amgen.Deepavali.PetalWheelMode.Spin&&s.sequence.Pattern.Winner>=0);next=true;keepAliveAt=now;break;
   case 15:
    if(now-keepAliveAt>1){Move(new Vector2(((int)(now-start)%2==0)?.35f:.65f,.96f));keepAliveAt=now;}
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.ScrollHolding)break;
    Check("Result reaches its matching fully visible scroll",s.sequence.IsResultFullyVisible&&s.scrollOverlay.CurrentState==DiwaliScrollOverlay.State.Shown&&s.SelectedScrollVariant==s.sequence.SelectedOutcome.scrollVariant);
    Capture("06-result-scroll");next=true;break;
   case 16:
    if(now-keepAliveAt>1){Move(new Vector2(((int)(now-start)%2==0)?.35f:.65f,.96f));keepAliveAt=now;}
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration)break;
    Check("Active visitor retains normal scroll close and reset",s.scrollOverlay.CurrentState==DiwaliScrollOverlay.State.Hidden&&resultCount==1&&r.Coverage==0);
    Move(new Vector2(.5f,.96f));next=true;break;
   case 17:
    if(age<1)break;Move(new Vector2(.5f,.5f));next=true;break;
   case 18:
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.Restoring)break;
    Move(new Vector2(.3f,.5f));next=true;break;
   case 19:
    if(age<.15)break;
    idleAt=now-s.InactivityElapsed;next=true;break;
   case 20:
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.ReturningToBase)
    {Check("Stationary mouse does not refresh idle clock",s.InactivityElapsed>=now-idleAt-.15);break;}
    timeoutAt=now;
    Check("Default inactivity waits thirty seconds",now-idleAt>=29.9&&now-idleAt<30.5);
    Check("Timeout pauses wheel without clearing visible state",s.sequence.Paused&&!r.paintingEnabled);next=true;break;
   case 21:
    if(s.InactivityFadeAlpha>.2f&&s.InactivityFadeAlpha<.85f&&!sawFade){sawFade=true;Capture("07-inactivity-fade");}
    if(s.InactivityFadeAlpha>.99f&&r.Coverage==0&&s.lamps.LitLampCount==0)sawBlackReset=true;
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration)break;
    Check("Inactivity return fades gradually through black before reset",sawFade&&sawBlackReset&&now-timeoutAt>=1.95);
    Check("Idle return resets all content and reassembles hand",r.Coverage==0&&s.lamps.LitLampCount==0&&s.scrollOverlay.CurrentState==DiwaliScrollOverlay.State.Hidden&&s.handPrompt.IsAppearing&&!s.sequence.Paused&&s.sequence.Pattern.Mode==Amgen.Deepavali.PetalWheelMode.Shimmer);
    next=true;break;
   case 22:
    if(age<1.1)break;
    Check("Reset holds at dark base and clears pending timers",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration&&r.Coverage==0&&s.DwellProgress==0&&resultCount==1);
    Capture("08-returned-base");done=true;break;
  }
  if(next){stage++;at=now;}
  UnityEditor.SessionState.SetString("Polish.Flow",Newtonsoft.Json.JsonConvert.SerializeObject(new{running=true,stage,phase=s.Phase.ToString(),idle=s.InactivityElapsed,coverage=r.Coverage,checks=checks.Count}));
 }
 catch(System.Exception e){errors.Add(e.ToString());done=true;}
 finally
 {
  if(done)
  {
   UnityEditor.EditorApplication.update-=tick;Application.logMessageReceived-=logger;s.sequence.ResultSelected-=result;
   UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);if(oldMouse!=null)oldMouse.MakeCurrent();if(oldKeyboard!=null)oldKeyboard.MakeCurrent();
   input.editorInputBehaviorInPlayMode=oldEditor;input.backgroundBehavior=oldBackground;Application.runInBackground=oldRun;
   JsonUtility.FromJsonOverwrite(savedSettings,s.settingsPreset);UnityEditor.EditorUtility.SetDirty(s.settingsPreset);UnityEditor.AssetDatabase.SaveAssetIfDirty(s.settingsPreset);s.settingsPreset.ApplyTo(s);ui.showControls=false;s.endSequence();
   var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0,checks,errors,timeline,idleWaitSeconds=timeoutAt-idleAt},Newtonsoft.Json.Formatting.Indented);
   System.IO.File.WriteAllText("output/session-polish/flow-validation.json",json);UnityEditor.SessionState.SetString("Polish.Flow",json);
  }
 }
};
UnityEditor.EditorApplication.update+=tick;return "Started mouse/keyboard, saved settings, scroll and 30-second inactivity checks";
