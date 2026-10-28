// Frame-observed integration checks through actual Input System MouseState events (no clicks).
var s = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
if (!Application.isPlaying || s == null) throw new System.Exception("Play the main wheel scene first.");
var r = s.restoration;
var oldMouse = UnityEngine.InputSystem.Mouse.current;
var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
var oldEditorInput = inputSettings.editorInputBehaviorInPlayMode;
var oldBackgroundInput = inputSettings.backgroundBehavior;
bool oldRunBackground = Application.runInBackground;
inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
Application.runInBackground = true;
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("DeepavaliSessionValidationMouse");
mouse.MakeCurrent();
var checks = new System.Collections.Generic.Dictionary<string, bool>();
var errors = new System.Collections.Generic.List<string>();
Application.LogCallback logger = (message, stack, type) => { if(type==LogType.Error || type==LogType.Exception) errors.Add(message); };
Application.logMessageReceived += logger;
var timeline = new System.Collections.Generic.List<object>();
var holds = new System.Collections.Generic.List<double>();
int resultCount = 0, restorationCount = 0, despawnCount = 0;
System.Action<int, Amgen.Deepavali.FlowerPaletteState> result = (i, state) => resultCount++;
System.Action restored = () => restorationCount++;
System.Action despawn = () => despawnCount++;
s.sequence.ResultSelected += result; r.Completed += restored; s.handPrompt.Despawned += despawn;
void Check(string name, bool pass) { checks[name] = pass; if (!pass) throw new System.Exception(name); }
void Move(Vector2 uv) { var rect = r.interactionCamera.pixelRect; UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,
    new UnityEngine.InputSystem.LowLevel.MouseState { position = new Vector2(rect.x + uv.x * rect.width, rect.y + uv.y * rect.height) }); }
void Capture(string name) => ScreenCapture.CaptureScreenshot("output/diwali-scroll-integration/mouse-cycle/" + name + ".png");
Vector2 Lamp(int i) => r.interactionCamera.WorldToViewportPoint(s.lamps.background.lamps[i].position);
void Reset() { s.endSequence(); Move(new Vector2(.5f,.96f)); }
Reset(); r.useMouseInput = true;
int stage = 0, frame = Time.frameCount, cycle = 0, brushIndex = 0;
double started = Time.realtimeSinceStartupAsDouble, stepAt = started, holdAt = 0;
string lastPhase = "";
float savedCoverage = 0;
var points = new System.Collections.Generic.List<Vector2>();
float aspect = r.interactionCamera.aspect;
// Cover the wall while intentionally leaving the fourth lamp dark.
for (int row=0;row<9;row++) for(int col=0;col<20;col++)
{
    float x = .025f + .95f * (row%2==0 ? col : 19-col)/19f;
    var uv = new Vector2(x,.025f + .95f*row/8f);
    var d = uv-Lamp(3);d.x*=aspect;
    if(d.magnitude > .255f) points.Add(uv);
}
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    bool done = false;
    try
    {
        if (!Application.isPlaying) throw new System.Exception("Play Mode ended.");
        if (Time.frameCount == frame) return;
        frame = Time.frameCount;
        double now = Time.realtimeSinceStartupAsDouble, age = now-stepAt;
        if(now-started>240) throw new System.Exception("Timed out at stage " + stage + ", phase " + s.Phase);
        if(lastPhase != s.Phase.ToString())
        {
            lastPhase=s.Phase.ToString(); timeline.Add(new{time=now-started,phase=lastPhase,coverage=r.Coverage,lamps=s.lamps.LitLampCount,resultCount});
        }
        bool next = false;
        switch(stage)
        {
            case 0:
                if(age<.4)break;
                Check("Opening has zero coverage and four extinguished lamps", r.Coverage==0 && s.lamps.LitLampCount==0 && s.lamps.background.lampLights.All(l=>!l.enabled));
                Check("Opening holds motion and wheel but retains material owner", r.idleBehaviours.All(b=>!b.enabled) && s.flower.enabled);
                Check("Opening prompt is visible above darkness", s.handPrompt.gameObject.activeInHierarchy && s.handPrompt.GetComponentInParent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay);
                Check("Review controls are absent", !UnityEngine.Object.FindObjectsByType<MonoBehaviour>(UnityEngine.FindObjectsSortMode.None).Any(b=>b.enabled && (b is Amgen.Deepavali.FlowerAppearancePreview || b is Amgen.Deepavali.PetalWheelSequencePreview || b is Amgen.Deepavali.RestorationPreview)));
                Capture("01-opening"); Move(new Vector2(.5f,.5f)); next=true;break;
            case 1:
                if(age<.22)break;
                Check("Button-free dwell drives existing loading ring",s.DwellProgress>0 && s.DwellProgress<1 && !mouse.leftButton.isPressed && s.handPrompt.DwellProgress>0);
                Check("Dwell does not paint",r.Coverage==0 && s.lamps.LitLampCount==0);
                Capture("02-dwell"); Move(new Vector2(.5f,.96f));next=true;break;
            case 2:
                if(age<.18)break;
                Check("Leaving prompt cancels partial dwell",s.DwellProgress==0 && !s.handPrompt.IsDismissing);
                Move(new Vector2(.5f,.5f));next=true;break;
            case 3:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.DismissingRestorationPrompt)break;
                Check("Disintegration still holds painting",r.Coverage==0 && !r.paintingEnabled);
                Capture("03-disintegrating"); next=true;break;
            case 4:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.Restoring)break;
                Check("Dismissal reveals the center and enables freeform restoration",r.SampleRestoration(new Vector2(.5f,.5f))>.95f && r.paintingEnabled && !s.handPrompt.gameObject.activeInHierarchy);
                next=true;break;
            case 5:
                if(age<.75)break;
                Check("Center light fades in",s.centerLight.enabled && s.centerLight.intensity>.5f);
                Capture("04-center-lit");Move(Lamp(0));next=true;break;
            case 6:
                if(age<.8)break;
                Check("First candle ignites on hover",s.lamps.LitLampCount==1);Move(Lamp(1));next=true;break;
            case 7:
                if(age<.8)break;
                Move(Lamp(2));next=true;break;
            case 8:
                if(age<.8)break;
                Check("Three candles do not finish",s.lamps.LitLampCount==3 && s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.Restoring);
                Capture("05-partial"); Move(new Vector2(-1,-1));next=true;break;
            case 9:
                if(age<.2)break;
                Check("Out-of-viewport mouse loses presence",!r.HasPointer);savedCoverage=r.Coverage;next=true;break;
            case 10:
                if(age<.4)break;
                Check("Tracking loss preserves coverage without painting",r.Coverage==savedCoverage);
                brushIndex=0;Move(points[0]);next=true;break;
            case 11:
                if(age<.13)break;
                if(r.Coverage>=s.requiredCoverage || brushIndex>=points.Count-1)
                {
                    Check("80 percent alone cannot finish without fourth candle",r.Coverage>=s.requiredCoverage && s.lamps.LitLampCount==3 && s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.Restoring);
                    Capture("06-coverage-gate");Move(Lamp(3));next=true;
                }
                else {Move(points[++brushIndex]);stepAt=now;}
                break;
            case 12:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.FinalReveal)break;
                Check("Both gates initiate final reveal once",s.lamps.LitLampCount==4 && r.Coverage>=.8f && r.Phase==Amgen.Deepavali.RestorationPhase.Completing);
                Move(new Vector2(.5f,.5f));next=true;break;
            case 13:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.AwaitingSpin)break;
                Check("Complete restoration enters shimmer and respawns prompt",r.Phase==Amgen.Deepavali.RestorationPhase.Idle && s.sequence.Pattern.Mode==Amgen.Deepavali.PetalWheelMode.Shimmer && s.handPrompt.gameObject.activeInHierarchy);
                Check("Original motion states return; direct petal picking stays disabled",r.idleBehaviours.Where(b=>!(b is Amgen.Deepavali.PetalLayerPointerInput)).All(b=>b.enabled) && r.idleBehaviours.OfType<Amgen.Deepavali.PetalLayerPointerInput>().All(b=>!b.enabled));
                Capture("07-shimmer-prompt");next=true;break;
            case 14:
                if(age<1.2)break;
                Check("Pointer remaining at center cannot retrigger respawned prompt",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingSpin && s.DwellProgress==0 && !s.PromptArmed);
                Move(new Vector2(.5f,.96f));next=true;break;
            case 15:
                if(age<.15)break;
                Move(new Vector2(.5f,.5f));next=true;break;
            case 16:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.EnteringWheel)break;
                Check("Fresh hover consumes prompt and starts existing entry",!s.handPrompt.gameObject.activeInHierarchy && s.sequence.Pattern.Mode==Amgen.Deepavali.PetalWheelMode.Pulse);
                Capture("08-entry");next=true;break;
            case 17:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.Spinning)break;
                Check("Spin waits until entry reaches Ready",s.sequence.Pattern.Mode==Amgen.Deepavali.PetalWheelMode.Spin && s.sequence.Pattern.Winner>=0 && s.sequence.Pattern.Winner<8);
                Capture("09-spin");next=true;break;
            case 18:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.RevealingResult)break;
                Check("Result reveal does not start hold early",s.ColoredHoldElapsed==0 && !s.sequence.IsResultFullyVisible);
                Capture("10-winner");next=true;break;
            case 19:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.ScrollHolding)break;
                Check("Hold starts only at fully visible colored result",s.sequence.IsResultFullyVisible && s.flower.TransitionProgress==1 && s.flower.SelectedState==s.sequence.OutcomePalette(s.sequence.Pattern.Winner));
                holdAt=now;Capture("11-colored-result");next=true;break;
            case 20:
                if(now-holdAt<6)break;
                Check("Original automatic return cannot cut the hold short",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.ScrollHolding && s.sequence.IsResultFullyVisible);
                next=true;break;
            case 21:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration)break;
                holds.Add(now-holdAt);
                Check("Scroll remains fully open fifteen seconds then closes",holds.Last()>=18.2 && holds.Last()<20);
                Check("Automatic end restores darkness, lamps, artwork and prompt",r.Coverage==0 && s.lamps.LitLampCount==0 && !s.sequence.enabled && s.flower.IsUsingStartupCenter && Enumerable.Range(0,8).All(i=>s.flower.IsUsingStartupPalette(i)) && s.handPrompt.gameObject.activeInHierarchy && s.ColoredHoldElapsed==0);
                Capture("12-reset");next=true;break;
            case 22:
                if(age<1)break;
                Check("Reset with stationary center mouse remains idle",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration && s.DwellProgress==0 && r.Coverage==0);
                Check("Dormant resets release shimmer and center lights",!s.centerLight.enabled && !UnityEngine.Object.FindObjectsByType<Light>(UnityEngine.FindObjectsSortMode.None).Any(l=>l.enabled && l.name.Contains("Shimmer Light")));
                Check("One completion, two prompt callbacks, one result per cycle",restorationCount==cycle+1 && resultCount==cycle+1 && despawnCount==2*(cycle+1));
                if(cycle==0) {cycle++;Move(new Vector2(.5f,.96f));next=true;}
                else {Check("Two complete sessions pass",cycle==1);done=true;}
                break;
            case 23:
                if(age<.2)break;
                Move(new Vector2(.5f,.5f));next=true;break;
            case 24:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.Restoring)break;
                Move(Lamp(0));next=true;break;
            case 25:
                if(age<.65)break;
                Move(Lamp(1));next=true;break;
            case 26:
                if(age<.65)break;
                Move(Lamp(2));next=true;break;
            case 27:
                if(age<.65)break;
                Move(Lamp(3));next=true;break;
            case 28:
                if(age<.65)break;
                Check("Four candles alone cannot finish below 80 percent",s.lamps.LitLampCount==4 && r.Coverage<.8f && s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.Restoring);
                brushIndex=0;Move(points[0]);next=true;break;
            case 29:
                if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.FinalReveal) {stage=12;stepAt=now;break;}
                if(age<.13)break;
                if(brushIndex>=points.Count-1) throw new System.Exception("Second sweep did not complete: " + r.Coverage);
                Move(points[++brushIndex]);stepAt=now;break;
        }
        if(next){stage++;stepAt=now;}
        UnityEditor.SessionState.SetString("Deepavali.SessionValidation", Newtonsoft.Json.JsonConvert.SerializeObject(new{running=true,stage,phase=s.Phase.ToString(),coverage=r.Coverage,lamps=s.lamps.LitLampCount,checks=checks.Count,cycle}));
    }
    catch(System.Exception e){errors.Add(e.ToString());done=true;}
    finally
    {
        if(done)
        {
            UnityEditor.EditorApplication.update-=tick;
            Application.logMessageReceived-=logger;
            s.sequence.ResultSelected-=result;r.Completed-=restored;s.handPrompt.Despawned-=despawn;
            UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);if(oldMouse!=null)oldMouse.MakeCurrent();
            inputSettings.editorInputBehaviorInPlayMode=oldEditorInput;inputSettings.backgroundBehavior=oldBackgroundInput;Application.runInBackground=oldRunBackground;
            s.endSequence();
            var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0,checks,errors,timeline,holds,resultCount,restorationCount,despawnCount},Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText("output/diwali-scroll-integration/mouse-cycle/session-validation.json",json);
            UnityEditor.SessionState.SetString("Deepavali.SessionValidation",json);
        }
    }
};
UnityEditor.EditorApplication.update+=tick;
return "Started full button-free mouse cycle validation";
