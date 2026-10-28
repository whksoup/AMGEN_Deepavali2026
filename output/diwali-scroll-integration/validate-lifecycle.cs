var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
if(!Application.isPlaying)throw new System.Exception("Play main scene first.");
Application.runInBackground=true;
var o=s.scrollOverlay;var r=s.restoration;
var checks=new System.Collections.Generic.Dictionary<string,bool>();
var errors=new System.Collections.Generic.List<string>();
void Check(string name,bool ok){checks[name]=ok;if(!ok)throw new System.Exception(name);}
bool mouse=r.useMouseInput;float complete=r.completionDuration,delay=s.scrollDelayAfterResultSeconds,hold=s.scrollHoldSeconds;
string timing=JsonUtility.ToJson(s.sequence.timing);
var phase=typeof(Amgen.Deepavali.DeepavaliSessionController).GetProperty("Phase");
int callbacks=0,countAtReset=0,iteration=0,stage=0,lastFrame=Time.frameCount;
System.Action opened=()=>callbacks++;System.Action hidden=()=>callbacks++;
o.Spawned+=opened;o.HideFinished+=hidden;
var targets=new[]{Amgen.Deepavali.DeepavaliSessionPhase.ScrollDelay,Amgen.Deepavali.DeepavaliSessionPhase.ScrollOpening,
    Amgen.Deepavali.DeepavaliSessionPhase.ScrollHolding,Amgen.Deepavali.DeepavaliSessionPhase.ScrollClosing};
double started=Time.realtimeSinceStartupAsDouble,at=started;
r.useMouseInput=false;r.completionDuration=.05f;
s.sequence.timing.bloomDelay=.05f;s.sequence.timing.bloomSpread=.1f;
Check("Negative duration sanitized",Amgen.Deepavali.DeepavaliSessionSettings.Safe(-1,2)==0);
Check("Non-finite duration sanitized",Amgen.Deepavali.DeepavaliSessionSettings.Safe(float.NaN,2)==2);
string savedSettings=JsonUtility.ToJson(s.settingsPreset);
try
{
    s.scrollDelayAfterResultSeconds=1.25f;s.scrollHoldSeconds=12.5f;
    Check("Save settings succeeds",s.SaveSettingsToPreset(out var message));
    string disk=System.IO.File.ReadAllText(UnityEditor.AssetDatabase.GetAssetPath(s.settingsPreset));
    Check("Both public timings persisted to disk",disk.Contains("scrollDelayAfterResultSeconds: 1.25")&&disk.Contains("scrollHoldSeconds: 12.5"));
    s.scrollDelayAfterResultSeconds=99;s.scrollHoldSeconds=99;s.settingsPreset.ApplyTo(s);
    Check("Reload restores both timings",s.scrollDelayAfterResultSeconds==1.25f&&s.scrollHoldSeconds==12.5f);
}
finally
{
    JsonUtility.FromJsonOverwrite(savedSettings,s.settingsPreset);UnityEditor.EditorUtility.SetDirty(s.settingsPreset);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(s.settingsPreset);s.scrollDelayAfterResultSeconds=delay;s.scrollHoldSeconds=hold;
}
var original=o.variants[0].hideClip;o.variants[0].hideClip=null;
Check("Missing clip rejected",!o.ValidateVariants(out _));o.variants[0].hideClip=original;
Check("Unknown name never selects a different scroll",o.FindVariant("missing")==-1);
var set=s.flower.outcomeSet;var duplicate=set.entries[1];set.entries[1]=set.entries[0];
Check("Duplicate outcome rejected",!set.Validate(out _));set.entries[1]=duplicate;
void Begin(){s.endSequence();s.handPrompt.CancelAndHide();r.CompleteRestoration();stage=1;at=Time.realtimeSinceStartupAsDouble;}
Begin();
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    bool done=false;
    try
    {
        if(!Application.isPlaying)throw new System.Exception("Play Mode ended.");
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        UnityEditor.SessionState.SetString("Scroll.Lifecycle", iteration+"/"+stage+" "+s.Phase);
        double now=Time.realtimeSinceStartupAsDouble;
        if(now-started>90)throw new System.Exception("Lifecycle timed out at "+iteration+"/"+stage);
        if(stage==1 && r.Phase==Amgen.Deepavali.RestorationPhase.Idle)
        {
            s.handPrompt.CancelAndHide();s.sequence.enabled=true;s.sequence.ResetPreview();s.sequence.HoldResult=true;
            s.scrollDelayAfterResultSeconds=iteration==0?2:.05f;s.scrollHoldSeconds=iteration==2?2:.05f;
            phase.SetValue(s,Amgen.Deepavali.DeepavaliSessionPhase.Spinning);s.sequence.PreviewResult(iteration);
            stage=2;
        }
        else if(stage==2 && s.Phase==targets[iteration])
        {
            s.endSequence();countAtReset=callbacks;at=now;stage=3;
        }
        else if(stage==3 && now-at>.8)
        {
            Check("Cancel from "+targets[iteration],s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration &&
                o.CurrentState==DiwaliScrollOverlay.State.Hidden && callbacks==countAtReset && !o.GetComponent<UnityEngine.UI.RawImage>().enabled);
            Check("All decoders stopped from "+targets[iteration],o.GetComponents<UnityEngine.Video.VideoPlayer>().All(v=>!v.isPlaying));
            if(++iteration==4)
            {
                Check("Prepare after repeated cancellation",o.PrepareVariant("Cyan"));
                o.enabled=false;o.enabled=true;
                Check("Disable clears pending preparation",o.CurrentState==DiwaliScrollOverlay.State.Hidden && !o.IsPrepared);
                Check("Re-enable can prepare again",o.PrepareVariant("Cyan"));o.CancelImmediate();done=true;
            }
            else Begin();
        }
    }
    catch(System.Exception e){errors.Add(e.ToString());done=true;}
    if(!done)return;
    UnityEditor.EditorApplication.update-=tick;o.Spawned-=opened;o.HideFinished-=hidden;
    r.useMouseInput=mouse;r.completionDuration=complete;
    s.scrollDelayAfterResultSeconds=delay;s.scrollHoldSeconds=hold;JsonUtility.FromJsonOverwrite(timing,s.sequence.timing);s.endSequence();
    System.IO.File.WriteAllText("output/diwali-scroll-integration/lifecycle-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=iteration==4&&errors.Count==0,checks,errors},Newtonsoft.Json.Formatting.Indented));
};
UnityEditor.EditorApplication.update+=tick;return "Lifecycle and persistence validation started.";
