var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
if(!Application.isPlaying)throw new System.Exception("Play main scene first.");
var o=s.scrollOverlay;var r=s.restoration;
var checks=new System.Collections.Generic.Dictionary<string,bool>();var errors=new System.Collections.Generic.List<string>();
var expectedErrors=new System.Collections.Generic.List<string>();
void Check(string name,bool ok){checks[name]=ok;if(!ok)throw new System.Exception(name);}
var phase=typeof(Amgen.Deepavali.DeepavaliSessionController).GetProperty("Phase");
bool mouse=r.useMouseInput;float completion=r.completionDuration,fallback=s.coloredHoldDuration,delay=s.scrollDelayAfterResultSeconds;
string timing=JsonUtility.ToJson(s.sequence.timing);
var variant=o.variants[o.FindVariant("Red")];var hide=variant.hideClip;
Application.LogCallback logger=(m,stack,t)=>{if(t==LogType.Error && m.StartsWith("DiwaliScrollOverlay:"))expectedErrors.Add(m);else if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};
Application.logMessageReceived+=logger;
r.useMouseInput=false;r.completionDuration=.05f;s.coloredHoldDuration=.15f;s.scrollDelayAfterResultSeconds=.05f;
s.sequence.timing.bloomDelay=.05f;s.sequence.timing.bloomSpread=.1f;
int stage=0,iteration=0,lastFrame=Time.frameCount;double started=Time.realtimeSinceStartupAsDouble,at=started;
void Begin(){s.endSequence();s.handPrompt.CancelAndHide();r.CompleteRestoration();stage=1;at=Time.realtimeSinceStartupAsDouble;}
Begin();
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    bool done=false;
    try
    {
        if(!Application.isPlaying)throw new System.Exception("Play Mode ended.");
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;double now=Time.realtimeSinceStartupAsDouble;
        if(now-started>45)throw new System.Exception("Failure recovery timed out at "+iteration+"/"+stage);
        if(stage==1&&r.Phase==Amgen.Deepavali.RestorationPhase.Idle)
        {
            s.handPrompt.CancelAndHide();s.sequence.enabled=true;s.sequence.ResetPreview();s.sequence.HoldResult=true;
            if(iteration==0)variant.hideClip=null;
            phase.SetValue(s,Amgen.Deepavali.DeepavaliSessionPhase.Spinning);s.sequence.PreviewResult(0);stage=2;
        }
        else if(stage==2)
        {
            if(iteration==0&&s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.HoldingResult)
            {Check("Missing clip preserves selected flower",s.flower.CurrentOutcome.palette==Amgen.Deepavali.FlowerPaletteState.Red);variant.hideClip=hide;stage=4;}
            else if(iteration==1&&s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.ScrollOpening && now-at>1)
            {
                foreach(var vp in o.GetComponents<UnityEngine.Video.VideoPlayer>())if(vp.isPlaying)vp.Pause();
                stage=3;
            }
            else if(iteration==2&&s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.ScrollHolding)
            {Check("Valid same-color playback recovers after errors",o.CurrentState==DiwaliScrollOverlay.State.Shown&&s.SelectedScrollVariant=="Red");done=true;}
        }
        else if(stage==3 && s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.HoldingResult)
        {Check("Stall watchdog reaches bounded fallback",o.CurrentState==DiwaliScrollOverlay.State.Hidden);stage=4;}
        else if(stage==4&&s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration)
        {Check("Failure resets cleanly "+iteration,r.Coverage==0&&!o.GetComponent<UnityEngine.UI.RawImage>().enabled);iteration++;Begin();}
    }
    catch(System.Exception e){errors.Add(e.ToString());done=true;}
    if(!done)return;
    UnityEditor.EditorApplication.update-=tick;Application.logMessageReceived-=logger;
    variant.hideClip=hide;r.useMouseInput=mouse;r.completionDuration=completion;s.coloredHoldDuration=fallback;s.scrollDelayAfterResultSeconds=delay;
    JsonUtility.FromJsonOverwrite(timing,s.sequence.timing);s.endSequence();
    System.IO.File.WriteAllText("output/diwali-scroll-integration/failure-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=iteration==2&&errors.Count==0&&expectedErrors.Count==2,checks,errors,expectedErrors},Newtonsoft.Json.Formatting.Indented));
};
UnityEditor.EditorApplication.update+=tick;return "Failure and recovery checks started.";
