// Runs real VideoPlayers and the integrated session through eight outcomes, plus a repeated outcome.
var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
if(!Application.isPlaying || s==null) throw new System.Exception("Play the main scene first.");
Application.runInBackground=true;
var checks=new System.Collections.Generic.Dictionary<string,bool>();
var errors=new System.Collections.Generic.List<string>();
var timeline=new System.Collections.Generic.List<object>();
var results=new System.Collections.Generic.List<object>();
var phaseProperty=typeof(Amgen.Deepavali.DeepavaliSessionController).GetProperty("Phase");
Application.LogCallback logger=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception)errors.Add(message);};
Application.logMessageReceived+=logger;
bool originalMouse=s.restoration.useMouseInput; s.restoration.useMouseInput=false;
float originalDelay=s.scrollDelayAfterResultSeconds,originalHold=s.scrollHoldSeconds;
var timing=s.sequence.timing;float originalBloomDelay=timing.bloomDelay,originalBloomSpread=timing.bloomSpread;
int run=0,stage=0,lastFrame=Time.frameCount;double started=Time.realtimeSinceStartupAsDouble,at=started,resultAt=0,openingAt=0,shownAt=0,hideAt=0;
string previous="";
void Check(string name,bool pass){checks[name]=pass;if(!pass)throw new System.Exception(name);}
void Begin(int slot)
{
    s.endSequence();s.handPrompt.CancelAndHide();s.restoration.CompleteRestoration();
    stage=1;at=Time.realtimeSinceStartupAsDouble;
}
void Capture(string name)=>ScreenCapture.CaptureScreenshot("output/diwali-scroll-integration/"+name+".png");
Check("Main settings default to 2-second delay and 15-second fully-open hold",originalDelay==2 && originalHold==15);
Check("Eight explicit outcomes and scroll variants",s.flower.outcomeSet.entries.Length==8 && s.scrollOverlay.variants.Length==8);
Check("Demo toggles disabled",!s.scrollOverlay.toggleOnMouseClick && !s.scrollOverlay.showOnStart && !s.scrollOverlay.waitForIdleLoopEnd);
Check("Overlay does not intercept input",!s.scrollOverlay.GetComponent<UnityEngine.UI.RawImage>().raycastTarget);
Begin(0);
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    bool done=false;
    try
    {
        if(!Application.isPlaying)throw new System.Exception("Play Mode ended.");
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        double now=Time.realtimeSinceStartupAsDouble;
        if(now-started>240)throw new System.Exception("Validation timed out: "+s.Phase+" / "+stage);
        string state=s.Phase+"/"+s.scrollOverlay.CurrentState;
        UnityEditor.SessionState.SetString("Scroll.Playback", "Run "+run+" stage "+stage+" "+state);
        if(state!=previous){timeline.Add(new{run,time=now-started,state});previous=state;}
        switch(stage)
        {
            case 1:
                if(s.restoration.Phase!=Amgen.Deepavali.RestorationPhase.Idle)break;
                s.sequence.enabled=true;s.sequence.ResetPreview();s.sequence.HoldResult=true;s.handPrompt.CancelAndHide();
                s.scrollDelayAfterResultSeconds=run==0?2:run==8?0:.12f;
                s.scrollHoldSeconds=run==0?15:run==8?0:.25f;
                timing.bloomDelay=.1f;timing.bloomSpread=run==1?.8f:.25f;
                int slot=run==8?7:run;
                var entry=s.flower.outcomeSet.At(slot);
                Check("Slot "+slot+" maps opening symbol and color",s.sequence.OutcomePalette(slot)==entry.palette && s.flower.GetStartupSymbol(s.sequence.innerClockwise[slot])==entry.openingSymbol);
                Check("Slot "+slot+" resolves complete scroll",s.scrollOverlay.FindVariant(entry.scrollVariant)>=0);
                for(int i=0;i<8;i++)
                {
                    var e=s.flower.outcomeSet.At(i);s.flower.PaletteSet.TryGet(e.palette,out var palette);
                    var c=s.flower.GetPetalRenderer(s.sequence.innerClockwise[i]).sharedMaterial.GetColor("_BaseColor");
                    Check("Opening fill "+i+" equals its result",Vector4.Distance(c,palette.fill.linear)<.001f);
                }
                phaseProperty.SetValue(s,Amgen.Deepavali.DeepavaliSessionPhase.Spinning);
                Check("Force result "+slot,s.sequence.PreviewResult(slot));
                stage=2;at=now;break;
            case 2:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.ScrollDelay)break;
                Check("Result is fully visible before delay "+run,s.sequence.IsResultFullyVisible);
                var outcome=s.sequence.SelectedOutcome;
                Check("Selected variant stays matched "+run,s.SelectedScrollVariant==outcome.scrollVariant);
                Check("Center has correct real icon "+run,s.flower.GetCenterIcon(outcome.palette)==outcome.icon);
                foreach(int index in s.sequence.innerClockwise)
                {
                    var mat=s.flower.GetPetalRenderer(index).sharedMaterial;
                    s.flower.outcomeSet.icons.TryGet(outcome.icon,out var icon);var r=icon.uvRect;
                    Check("Result glyph "+run+"/"+index,mat.GetFloat("_OutcomeIconEnabled")==1 &&
                        Vector4.Distance(mat.GetVector("_OutcomeIconRect"),new Vector4(r.width,r.height,r.x,r.y))<.001f);
                }
                resultAt=now;Capture("result-"+run+"-"+outcome.scrollVariant);stage=3;break;
            case 3:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.ScrollOpening)break;
                openingAt=now;
                Check("Delay starts after completed result "+run,now-resultAt>=s.scrollDelayAfterResultSeconds-.08);
                Check("Explicit scroll color "+run,s.scrollOverlay.variants[s.scrollOverlay.CurrentVariant].name==s.SelectedScrollVariant);
                stage=4;break;
            case 4:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.ScrollHolding)break;
                shownAt=now;Capture("scroll-"+run+"-"+s.SelectedScrollVariant);
                Check("Opening clip completes before hold "+run,now-openingAt>5);
                Check("Flower stays settled beneath scroll "+run,s.sequence.IsResultFullyVisible);
                stage=5;break;
            case 5:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.ScrollClosing)break;
                hideAt=now;
                Check("Fully-open hold duration "+run,now-shownAt>=s.scrollHoldSeconds-.08 && now-shownAt<s.scrollHoldSeconds+.7);
                stage=6;break;
            case 6:
                if(s.Phase!=Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration)break;
                Check("Closing completed before reset "+run,now-hideAt>3);
                Check("Reset clears overlay and result "+run,s.scrollOverlay.CurrentState==DiwaliScrollOverlay.State.Hidden &&
                    !s.scrollOverlay.GetComponent<UnityEngine.UI.RawImage>().enabled && s.flower.CurrentOutcome==null && s.SelectedScrollVariant==null);
                Check("Reset restores darkness and prompt "+run,s.restoration.Coverage==0 && s.lamps.LitLampCount==0 && s.handPrompt.gameObject.activeInHierarchy);
                Check("Exactly three reusable video players "+run,s.scrollOverlay.GetComponents<UnityEngine.Video.VideoPlayer>().Length==3);
                results.Add(new{run,delay=openingAt-resultAt,opening=shownAt-openingAt,hold=hideAt-shownAt,closing=now-hideAt});
                if(++run>=9)done=true;else Begin(run==8?7:run);
                break;
        }
    }
    catch(System.Exception e){errors.Add(e.ToString());done=true;}
    if(!done)return;
    UnityEditor.EditorApplication.update-=tick;Application.logMessageReceived-=logger;
    s.scrollDelayAfterResultSeconds=originalDelay;s.scrollHoldSeconds=originalHold;
    timing.bloomDelay=originalBloomDelay;timing.bloomSpread=originalBloomSpread;
    s.restoration.useMouseInput=originalMouse;s.endSequence();
    System.IO.File.WriteAllText("output/diwali-scroll-integration/playback-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(
        new{completedAt=System.DateTime.Now,passed=run==9&&errors.Count==0,checks,errors,results,timeline},Newtonsoft.Json.Formatting.Indented));
};
UnityEditor.EditorApplication.update+=tick;
return new{started=true,expectedSeconds=130};
