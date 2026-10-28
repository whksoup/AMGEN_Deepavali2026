// Input System TouchState events exercise the actual EnhancedTouch -> adapter -> session path.
var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();var r=s.restoration;
var source=r.GetComponent<Amgen.Deepavali.DeepavaliTouchInput>();var ui=s.GetComponent<Amgen.Deepavali.DeepavaliSessionDebugUI>();
if(!Application.isPlaying||source==null)throw new System.Exception("Play the configured wall scene");
var input=UnityEngine.InputSystem.InputSystem.settings;var oldEditor=input.editorInputBehaviorInPlayMode;var oldBackground=input.backgroundBehavior;
input.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
input.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var oldMouse=UnityEngine.InputSystem.Mouse.current;var oldScreen=UnityEngine.InputSystem.Touchscreen.current;
var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("WallValidationMouse");mouse.MakeCurrent();
var screen=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>("WallValidationSensor");
var checks=new System.Collections.Generic.Dictionary<string,bool>();var errors=new System.Collections.Generic.List<string>();
Application.LogCallback logger=(m,st,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=logger;
var oldRect=r.interactionCamera.rect;float brush=r.brushRadius,timeout=s.inactivityTimeout,fade=s.inactivityFadeDuration;
void Check(string n,bool ok){checks[n]=ok;if(!ok)throw new System.Exception(n);}
Vector2 Pixels(Vector2 uv){var rect=r.interactionCamera.pixelRect;return new Vector2(rect.x+uv.x*rect.width,rect.y+uv.y*rect.height);}
void Send(int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 uv,int display=0)=>UnityEngine.InputSystem.InputSystem.QueueStateEvent(screen,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=id,phase=phase,position=Pixels(uv),displayIndex=(byte)display});
void Mouse(Vector2 uv)=>UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=Pixels(uv)});
var center=new Vector2(.5f,.5f);var outside=new Vector2(.5f,.96f);var a=new Vector2(.2f,.3f);var b=new Vector2(.38f,.3f);
var points=new System.Collections.Generic.List<Vector2>();for(int y=0;y<7;y++)for(int x=0;x<17;x++)points.Add(new Vector2(.02f+.96f*(y%2==0?x:16-x)/16,.02f+.96f*y/6));
int stage=0,frame=Time.frameCount,point=0;float coverage=0;double start=Time.realtimeSinceStartupAsDouble,at=start;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 bool done=false;
 try{
  if(!Application.isPlaying)throw new System.Exception("Play Mode ended");if(frame==Time.frameCount)return;frame=Time.frameCount;
  double now=Time.realtimeSinceStartupAsDouble,age=now-at;bool next=false;
  if(now-start>85)throw new System.Exception("Timeout stage "+stage+" phase "+s.Phase);
  switch(stage){
   case 0:s.endSequence();source.mousePreview=false;ui.showControls=false;Mouse(center);next=true;break;
   case 1:if(age<.2)break;Check("Sensor is default with no phantom mouse pointer",!r.useMouseInput&&!r.HasPointer&&s.DwellProgress==0);Send(1,UnityEngine.InputSystem.TouchPhase.Began,outside);next=true;break;
   case 2:if(age<.08)break;Check("Native touch XY reaches the shared viewport pointer",r.HasPointer&&source.ContactId==1&&(r.PointerViewport-outside).sqrMagnitude<.000001f);Check("Sensor outside prompt arms entry",s.PromptArmed);Send(1,UnityEngine.InputSystem.TouchPhase.Moved,center);next=true;break;
   case 3:if(s.handPrompt.IsAppearing)break;next=true;break;
   case 4:if(age<.25)break;Check("Stationary contact feeds hover dwell",s.DwellProgress>.2f&&s.DwellProgress<.8f);Send(2,UnityEngine.InputSystem.TouchPhase.Began,center);next=true;break;
   case 5:if(age<.1)break;Check("Second contact cannot steal dwell",source.ContactId==1);Send(1,UnityEngine.InputSystem.TouchPhase.Ended,center);next=true;break;
   case 6:if(age<.08)break;Check("Contact handover restarts dwell",source.ContactId==2&&s.DwellProgress<.25f);next=true;break;
   case 7:if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.Restoring)break;Check("Sensor dwell dismisses guidance and starts restoration",r.paintingEnabled&&!s.handPrompt.gameObject.activeSelf);Send(2,UnityEngine.InputSystem.TouchPhase.Ended,center);next=true;break;
   case 8:if(age<.12)break;Check("Contact release clears pointer immediately",!r.HasPointer&&!source.HasSensorContact);coverage=r.Coverage;next=true;break;
   case 9:if(age<.3)break;Check("Released XY cannot keep painting",r.Coverage==coverage);r.ResetRestoration();r.brushRadius=.06f;Send(3,UnityEngine.InputSystem.TouchPhase.Began,a);next=true;break;
   case 10:if(age<.5)break;Check("Held sensor paints continuously",r.SampleRestoration(a)>.5f);Send(4,UnityEngine.InputSystem.TouchPhase.Began,b);next=true;break;
   case 11:if(age<.12)break;Check("Second hand cannot redirect a stroke",source.ContactId==3&&r.SampleRestoration(b)<.01f);Send(3,UnityEngine.InputSystem.TouchPhase.Ended,a);next=true;break;
   case 12:if(age<.5)break;Check("New contact begins a separate stroke without a joining line",source.ContactId==4&&r.SampleRestoration(b)>.5f&&r.SampleRestoration((a+b)*.5f)<.01f);Send(4,UnityEngine.InputSystem.TouchPhase.Canceled,b);next=true;break;
   case 13:if(age<.1)break;Check("Canceled contact clears pointer",!r.HasPointer);Send(5,UnityEngine.InputSystem.TouchPhase.Began,new Vector2(-.2f,.5f));next=true;break;
   case 14:if(age<.1)break;Check("Out-of-camera contacts are ignored",!r.HasPointer);Send(5,UnityEngine.InputSystem.TouchPhase.Ended,new Vector2(-.2f,.5f));Send(6,UnityEngine.InputSystem.TouchPhase.Began,center,1);next=true;break;
   case 15:if(age<.1)break;Check("Other display contacts are ignored",!r.HasPointer);Send(6,UnityEngine.InputSystem.TouchPhase.Ended,center,1);r.interactionCamera.rect=new Rect(.1f,.2f,.7f,.6f);Send(7,UnityEngine.InputSystem.TouchPhase.Began,new Vector2(.2f,.7f));next=true;break;
   case 16:if(age<.1)break;Check("Screen XY respects camera offset and size",r.HasPointer&&(r.PointerViewport-new Vector2(.2f,.7f)).sqrMagnitude<.000001f);Send(7,UnityEngine.InputSystem.TouchPhase.Ended,new Vector2(.2f,.7f));r.interactionCamera.rect=oldRect;ui.showControls=true;Send(8,UnityEngine.InputSystem.TouchPhase.Began,new Vector2(.95f,.9f));next=true;break;
   case 17:if(age<.1)break;Check("Debug panel blocks sensor brushing and dwell",!r.HasPointer);ScreenCapture.CaptureScreenshot("output/sensor-input/01-sensor-debug.png");Send(8,UnityEngine.InputSystem.TouchPhase.Ended,new Vector2(.95f,.9f));ui.showControls=false;Send(9,UnityEngine.InputSystem.TouchPhase.Began,a);next=true;break;
   case 18:if(age<.1)break;Check("Sensor resumes outside the debug panel",r.HasPointer);UnityEngine.InputSystem.InputSystem.RemoveDevice(screen);next=true;break;
   case 19:if(age<.1)break;Check("Device removal clears tracking",!r.HasPointer&&!source.HasSensorContact);screen=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>("WallValidationSensor");source.mousePreview=true;UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();Mouse(center);next=true;break;
   case 20:if(age<.1)break;Check("Optional mouse preview retains hover interaction",r.useMouseInput&&r.HasPointer);source.mousePreview=false;next=true;break;
   case 21:if(age<.1)break;Check("Returning to sensor mode has no mouse fallback",!r.useMouseInput&&!r.HasPointer);r.brushRadius=brush;r.ResetRestoration();Send(10,UnityEngine.InputSystem.TouchPhase.Began,points[0]);next=true;break;
   case 22:
    if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.FinalReveal){next=true;break;}
    if(age<.12)break;if(++point>=points.Count)throw new System.Exception("Sensor sweep incomplete: "+r.Coverage);Send(10,UnityEngine.InputSystem.TouchPhase.Moved,points[point]);at=now;break;
   case 23:if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.AwaitingSpin)break;Check("Touch sweep restores candles and finishes directly in shimmer",r.Coverage>=s.requiredCoverage&&s.lamps.LitLampCount==4&&s.sequence.Pattern.Mode==Amgen.Deepavali.PetalWheelMode.Shimmer);ScreenCapture.CaptureScreenshot("output/sensor-input/02-sensor-restored.png");Send(10,UnityEngine.InputSystem.TouchPhase.Moved,outside);next=true;break;
   case 24:
    if(s.PromptArmed&&r.PointerViewport.y>.9f)Send(10,UnityEngine.InputSystem.TouchPhase.Moved,center);
    if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.Spinning)break;Check("Sensor activates the second prompt and starts the wheel",s.sequence.Pattern.Mode==Amgen.Deepavali.PetalWheelMode.Spin);next=true;break;
   case 25:if(age<.25)break;Send(10,UnityEngine.InputSystem.TouchPhase.Moved,new Vector2(.7f,.7f));next=true;break;
   case 26:if(age<.08)break;Check("Sensor movement refreshes inactivity outside restoration",s.InactivityElapsed<.2f);Send(10,UnityEngine.InputSystem.TouchPhase.Ended,new Vector2(.7f,.7f));s.inactivityTimeout=.5f;s.inactivityFadeDuration=.4f;next=true;break;
   case 27:if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.ReturningToBase)break;Check("Released sensor allows inactivity return",!r.HasPointer);next=true;break;
   case 28:if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration)break;Check("Sensor session resets cleanly for the next visitor",r.Coverage==0&&s.lamps.LitLampCount==0&&!r.HasPointer&&s.handPrompt.IsAppearing);done=true;break;
  }
  if(next){stage++;at=now;}
  UnityEditor.SessionState.SetString("Sensor.Validation",Newtonsoft.Json.JsonConvert.SerializeObject(new{running=true,stage,phase=s.Phase.ToString(),contact=source.ContactId,checks=checks.Count}));
 }catch(System.Exception e){errors.Add(e.ToString());done=true;}
 finally{if(done){
  UnityEditor.EditorApplication.update-=tick;Application.logMessageReceived-=logger;
  if(screen.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(screen);UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
  if(oldMouse!=null)oldMouse.MakeCurrent();if(oldScreen!=null&&oldScreen.added)oldScreen.MakeCurrent();
  input.editorInputBehaviorInPlayMode=oldEditor;input.backgroundBehavior=oldBackground;
  r.interactionCamera.rect=oldRect;r.brushRadius=brush;s.inactivityTimeout=timeout;s.inactivityFadeDuration=fade;source.mousePreview=false;ui.showControls=false;s.endSequence();
  var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0,checks,errors},Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText("output/sensor-input/touch-validation.json",json);UnityEditor.SessionState.SetString("Sensor.Validation",json);
 }}
};UnityEditor.EditorApplication.update+=tick;return "Started native-touch route, lifecycle, continuity, dwell and full restoration checks";
