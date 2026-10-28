// Sensor-route dwell tests and reset interruption matrix. No direct session phase manipulation.
var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
if(!Application.isPlaying || s==null)throw new System.Exception("Play main scene first.");
var r=s.restoration;
bool oldMouse=r.useMouseInput, oldBackground=Application.runInBackground;
float oldCompletion=r.completionDuration;
string oldTiming=JsonUtility.ToJson(s.sequence.timing);
var checks=new System.Collections.Generic.Dictionary<string,bool>();
var errors=new System.Collections.Generic.List<string>();
void Check(string name,bool pass){checks[name]=pass;if(!pass)throw new System.Exception(name);}
int callbacks=0;
System.Action despawn=()=>callbacks++;
System.Action restored=()=>callbacks++;
System.Action<int,Amgen.Deepavali.FlowerPaletteState> result=(i,p)=>callbacks++;
s.handPrompt.Despawned+=despawn;r.Completed+=restored;s.sequence.ResultSelected+=result;
Application.runInBackground=true;r.useMouseInput=false;
s.endSequence();
Vector2 center=new Vector2(.5f,.5f),outside=new Vector2(.5f,.95f),point=outside;
bool feed=true;
int stage=0,frame=Time.frameCount,targetIndex=0,countAtReset=0;
double started=Time.realtimeSinceStartupAsDouble,stepAt=started;
var targets=new[]{Amgen.Deepavali.DeepavaliSessionPhase.Restoring,Amgen.Deepavali.DeepavaliSessionPhase.FinalReveal,
    Amgen.Deepavali.DeepavaliSessionPhase.AwaitingSpin,Amgen.Deepavali.DeepavaliSessionPhase.DismissingSpinPrompt,
    Amgen.Deepavali.DeepavaliSessionPhase.EnteringWheel,Amgen.Deepavali.DeepavaliSessionPhase.Spinning,
    Amgen.Deepavali.DeepavaliSessionPhase.RevealingResult,Amgen.Deepavali.DeepavaliSessionPhase.HoldingResult};
bool seeded=false,spinOutside=false;
double spinOutsideAt=0;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    bool done=false;
    try
    {
        if(!Application.isPlaying)throw new System.Exception("Play Mode ended");
        if(Time.frameCount==frame)return;frame=Time.frameCount;
        double now=Time.realtimeSinceStartupAsDouble,age=now-stepAt;
        if(now-started>180)throw new System.Exception("Lifecycle timeout stage "+stage+" target "+targetIndex);
        bool next=false;
        switch(stage)
        {
            case 0:if(age<.15)break;point=center;next=true;break;
            case 1:
                if(age<.2)break;
                Check("External XY and presence drive the same dwell",s.DwellProgress>.1f && r.Coverage==0);
                r.SetPointerViewport(center,false);r.SetPointerViewport(center,true);next=true;break;
            case 2:
                if(age<.03)break;
                Check("Loss and reacquisition within one frame cancels accumulated dwell",s.DwellProgress<.2f);
                next=true;break;
            case 3:
                if(age<.2)break;
                r.SetPointerViewport(new Vector2(float.NaN,.5f),true);r.SetPointerViewport(center,true);next=true;break;
            case 4:
                if(age<.03)break;
                Check("Invalid sample between valid samples breaks dwell",s.DwellProgress<.2f);
                feed=false;next=true;break;
            case 5:
                if(age<r.inputTimeout+.15)break;
                Check("Stale sensor samples cancel dwell",!r.HasPointer && s.DwellProgress==0 && r.Coverage==0);
                feed=true;point=center;next=true;break;
            case 6:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.DismissingRestorationPrompt)break;
                s.endSequence();countAtReset=callbacks;next=true;break;
            case 7:
                if(age<1.1)break;
                Check("External reset cancels prompt disintegration and its callback",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration && callbacks==countAtReset && !s.handPrompt.IsDismissing && r.Coverage==0);
                Check("Tracking at center cannot rearm after external reset",!s.PromptArmed && s.DwellProgress==0);
                point=outside;next=true;break;
            case 8:
                if(age<.1)break;
                point=center;next=true;break;
            case 9:
                if(age<.2)break;
                var rect=r.interactionCamera.pixelRect;
                r.SetPointerScreen(new Vector2(rect.x+rect.width*.2f,rect.y+rect.height*.2f),true);
                feed=false;next=true;break;
            case 10:
                if(age<.03)break;
                Check("Screen-pixel adapter cancels dwell outside prompt",s.DwellProgress==0 && Vector2.Distance(r.PointerViewport,new Vector2(.2f,.2f))<.001f);
                feed=true;s.endSequence();point=outside;stage=19;stepAt=now;break;
            case 19:
                if(age<.12)break;
                point=center;seeded=spinOutside=false;next=true;break;
            case 20:
                if(s.Phase==targets[targetIndex])
                {
                    s.endSequence();countAtReset=callbacks;point=center;next=true;break;
                }
                if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.Restoring && !seeded)
                {
                    // Other checks cover real painting and both gates. Seed only to reach interruption targets promptly.
                    r.RevealPoint(center,4);seeded=true;
                }
                if(s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingSpin)
                {
                    if(!spinOutside){point=outside;spinOutside=true;spinOutsideAt=now;}
                    else if(now-spinOutsideAt>.12)point=center;
                }
                break;
            case 21:
                if(age<(targets[targetIndex]==Amgen.Deepavali.DeepavaliSessionPhase.HoldingResult?10.3:1.1))break;
                Check("Reset interrupts "+targets[targetIndex]+" without delayed callbacks",callbacks==countAtReset && s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration && r.Coverage==0 && s.lamps.LitLampCount==0 && s.DwellProgress==0 && s.ColoredHoldElapsed==0 && !s.sequence.enabled && s.handPrompt.gameObject.activeInHierarchy);
                if(++targetIndex==targets.Length){next=true;break;}
                s.endSequence();point=outside;stage=19;stepAt=now;break;
            case 22:
                s.enabled=false;s.enabled=true;point=center;countAtReset=callbacks;next=true;break;
            case 23:
                if(age<1.1)break;
                Check("Session re-enable resets without duplicate listeners or immediate activation",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration && callbacks==countAtReset && s.DwellProgress==0 && r.Coverage==0);
                done=true;break;
        }
        if(feed)r.SetPointerViewport(point,true);
        if(next){stage++;stepAt=now;}
        UnityEditor.SessionState.SetString("Deepavali.LifecycleValidation",Newtonsoft.Json.JsonConvert.SerializeObject(new{running=true,stage,targetIndex,phase=s.Phase.ToString(),checks=checks.Count}));
    }
    catch(System.Exception e){errors.Add(e.ToString());done=true;}
    finally
    {
        if(done)
        {
            UnityEditor.EditorApplication.update-=tick;
            s.handPrompt.Despawned-=despawn;r.Completed-=restored;s.sequence.ResultSelected-=result;
            r.useMouseInput=oldMouse;r.completionDuration=oldCompletion;Application.runInBackground=oldBackground;
            JsonUtility.FromJsonOverwrite(oldTiming,s.sequence.timing);s.endSequence();
            var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=errors.Count==0,checks,errors},Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText("output/restoration-session/lifecycle-validation.json",json);
            UnityEditor.SessionState.SetString("Deepavali.LifecycleValidation",json);
        }
    }
};
UnityEditor.EditorApplication.update+=tick;
return "Started external tracking and all-phase reset validation";
