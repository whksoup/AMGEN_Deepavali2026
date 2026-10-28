// Frame-observed Play Mode integration checks; run only in the restoration review scene.
if (!Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Deepavali_RestorationTest")
    throw new System.InvalidOperationException("Open the restoration preview in Play Mode first.");
var r = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.ScreenRestorationController>();
var lamps = r.GetComponent<Amgen.Deepavali.RestorationLampResponse>();
var preview = r.GetComponent<Amgen.Deepavali.RestorationPreview>();
var sequence = preview.sequence;
var flower = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var checks = new System.Collections.Generic.Dictionary<string, bool>();
var errors = new System.Collections.Generic.List<string>();
var metrics = new System.Collections.Generic.Dictionary<string, double>();
var path = "output/restoration-session/prototype-regression/";
System.IO.Directory.CreateDirectory(path);
float originalTimeScale = Time.timeScale;
Time.timeScale = 0; r.useMouseInput = false; preview.showControls = false;
int completed = 0;
UnityEngine.Events.UnityAction completedListener = () => completed++;
r.onRestored.AddListener(completedListener);
Application.LogCallback logger = (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception) errors.Add(message); };
Application.logMessageReceived += logger;
void Check(string name, bool pass) { checks[name] = pass; if (!pass) throw new System.Exception(name); }
void Capture(string name)
{
    // Use the same composited Game-view capture as the Unity MCP tool, outside end-of-frame callbacks.
    var captureType=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
    var method=captureType.GetMethod("EncodeScreenToPng",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
    var bytes=(byte[])method.Invoke(null,new object[]{1600,700});
    System.IO.File.WriteAllBytes(path+name+".png",bytes);
}
Texture2D Read(string name)
{
    var image = new Texture2D(2,2); image.LoadImage(System.IO.File.ReadAllBytes(path+name+".png")); return image;
}
float Chroma(Texture2D image, Vector2 point)
{
    float sum=0; int count=0;
    for(int y=-6;y<=6;y++) for(int x=-6;x<=6;x++)
    {
        Color c=image.GetPixelBilinear(point.x+x*.003f,point.y+y*.003f);
        sum+=Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b));count++;
    }
    return sum/count;
}
void Dwell(Vector2 p) { for(int i=0;i<12;i++) r.Advance(.05f,p,true); r.Advance(0,Vector2.zero,false); }
void PaintCorner(int i)
{
    r.ResetRestoration();
    Vector3 p=r.interactionCamera.WorldToViewportPoint(lamps.background.lamps[i].position);
    Dwell(p);
}
void CheckCorner(int i)
{
    Capture("corner-"+i);
    var image=Read("corner-"+i);
    try
    {
        Vector3 p=r.interactionCamera.WorldToViewportPoint(lamps.background.lamps[i].position);
        float target=Chroma(image,p), mirrored=Chroma(image,new Vector2(p.x,1-p.y));
        metrics["corner"+i+"Chroma"]=target; metrics["corner"+i+"MirroredChroma"]=mirrored;
        Check("Rendered reveal aligns with corner "+i,target>.03f && mirrored<.01f);
        Check("Only touched candle ignites "+i,lamps.LitLampCount==1 && lamps.background.lampLights.Select((l,j)=>l.enabled==(i==j)).All(x=>x));
    }
    finally { UnityEngine.Object.Destroy(image); }
}
r.ResetRestoration();
Check("Starts dormant with all idle behaviours paused", r.Phase==Amgen.Deepavali.RestorationPhase.Restoring && r.Coverage==0 && r.idleBehaviours.All(b=>!b.enabled));
Check("Material owner remains active",flower.enabled);
float before=r.Coverage;
r.Advance(.05f,new Vector2(float.NaN,0),true);r.Advance(.05f,new Vector2(-1,0),true);
Check("Invalid coordinates are ignored",r.Coverage==before);
Dwell(new Vector2(.15f,.5f));Dwell(new Vector2(.85f,.5f));
Check("Presence gaps do not paint connecting lines",r.SampleRestoration(new Vector2(.5f,.5f))==0);
r.ResetRestoration();r.Advance(.05f,new Vector2(.15f,.5f),true);r.Advance(.05f,new Vector2(.85f,.5f),true);
Check("Large tracking jumps do not bridge",r.SampleRestoration(new Vector2(.5f,.5f))==0);
r.ResetRestoration();
var rect=r.interactionCamera.rect;
r.interactionCamera.rect=new Rect(.1f,.15f,.7f,.6f);
var pixel=r.interactionCamera.pixelRect;
r.SetPointerScreen(new Vector2(pixel.x+.3f*pixel.width,pixel.y+.4f*pixel.height));
var external=(Vector2)typeof(Amgen.Deepavali.ScreenRestorationController).GetField("externalPoint",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(r);
Check("Pixel input respects camera viewport offset",Vector2.Distance(external,new Vector2(.3f,.4f))<.0001f);
r.interactionCamera.rect=rect;r.ResetRestoration();
int stage=0, frame=Time.frameCount;
double deadline=Time.realtimeSinceStartupAsDouble+.2, started=Time.realtimeSinceStartupAsDouble;
float heldCoverage=0;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    bool done=false;
    try
    {
        if(!Application.isPlaying)throw new System.Exception("Play Mode ended during validation.");
        if(Time.realtimeSinceStartupAsDouble-started>55)throw new System.Exception("Validation timed out.");
        if(Time.frameCount==frame || Time.realtimeSinceStartupAsDouble<deadline)return;
        frame=Time.frameCount;
        switch(stage)
        {
            case 0:
                Capture("01-dormant");
                var dormant=Read("01-dormant");
                var colors=dormant.GetPixels32();
                // The camera's packed HDR target has unequal RGB precision before the final display conversion.
                var greyErrors=colors.Select(c=>Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))).ToArray();
                metrics["DormantMeanByteChroma"]=greyErrors.Average();
                Check("Dormant rendering is monochrome",greyErrors.Max()<=2 && greyErrors.Average()<.1);
                UnityEngine.Object.Destroy(dormant);
                PaintCorner(0);break;
            case 1:case 2:case 3:case 4:
                CheckCorner(stage-1);
                if(stage<4)PaintCorner(stage);
                else
                {
                    r.ResetRestoration();
                    for(int i=0;i<120;i++)r.Advance(1f/60,Vector2.Lerp(new Vector2(.085f,.84f),new Vector2(.65f,.35f),i/119f),true);
                    Dwell(new Vector2(.085f,.84f));
                }
                break;
            case 5:
                Capture("02-partial");
                heldCoverage=r.Coverage;r.Advance(2,Vector2.zero,false);
                Check("Paint persists after presence ends",r.Coverage==heldCoverage);
                r.ResetRestoration();lamps.enabled=false;
                var white=Enumerable.Repeat((byte)255,r.MaskTexture.width*r.MaskTexture.height).ToArray();
                r.MaskTexture.SetPixelData(white,0);r.MaskTexture.Apply(false,false);
                break;
            case 6:
                Capture("full-mask-reference");
                typeof(Amgen.Deepavali.ScreenRestorationController).GetField("<Phase>k__BackingField",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                    .SetValue(r,Amgen.Deepavali.RestorationPhase.Idle);
                break;
            case 7:
                Capture("bypass-reference");
                var full=Read("full-mask-reference");var bypass=Read("bypass-reference");
                var fc=full.GetPixels32();var bc=bypass.GetPixels32();double error=0;int max=0;
                for(int i=0;i<fc.Length;i++){int dr=Mathf.Abs(fc[i].r-bc[i].r),dg=Mathf.Abs(fc[i].g-bc[i].g),db=Mathf.Abs(fc[i].b-bc[i].b);error+=dr+dg+db;max=Mathf.Max(max,Mathf.Max(dr,Mathf.Max(dg,db)));}
                error/=fc.Length*3.0;metrics["FullMaskMeanByteError"]=error;metrics["FullMaskMaxByteError"]=max;
                UnityEngine.Object.Destroy(full);UnityEngine.Object.Destroy(bypass);
                // An active fullscreen pass adds one packed-HDR encode/decode; allow <1 display level on
                // average and 4 at isolated pixels. Completed gameplay bypasses the pass altogether.
                Check("White mask preserves camera color within HDR precision",error<.15 && max<=4);
                r.ResetRestoration();lamps.enabled=true;
                r.SetPointerViewport(new Vector2(.2f,.35f));
                break;
            case 8:
                Check("External XY input paints",r.Coverage>0);
                Check("Stale tracking stops painting",!r.HasPointer);
                heldCoverage=r.Coverage;
                break;
            case 9:
                Check("Stale input does not accumulate",r.Coverage==heldCoverage);
                r.ResetRestoration();
                for(float y=.04f;y<1 && r.Phase==Amgen.Deepavali.RestorationPhase.Restoring;y+=.1f)
                    for(float x=.02f;x<1 && r.Phase==Amgen.Deepavali.RestorationPhase.Restoring;x+=.045f)
                        for(int i=0;i<4 && r.Phase==Amgen.Deepavali.RestorationPhase.Restoring;i++)r.Advance(.1f,new Vector2(x,y),true);
                Check("Coverage triggers completion",r.Coverage>=r.completionCoverage && r.Phase==Amgen.Deepavali.RestorationPhase.Completing);
                r.Advance(.45f,Vector2.zero,false);
                Check("Completion blends remaining gaps gradually",r.CompletionReveal>0 && r.CompletionReveal<1);
                r.Advance(2,Vector2.zero,false);
                break;
            case 10:
                Check("Completion fires once",completed==1 && r.Phase==Amgen.Deepavali.RestorationPhase.Idle);
                Check("All four lamps restored",lamps.LitLampCount==4 && lamps.background.lampLights.All(l=>l.enabled));
                Check("Idle motion resumes",r.idleBehaviours.Where(b=>b is Amgen.Deepavali.PetalMotionController || b is Amgen.Deepavali.OuterPetalLayerMotion || b is Amgen.Deepavali.PetalRingPulse).All(b=>b.enabled));
                Check("Previously disabled petal input remains disabled",r.idleBehaviours.Where(b=>b is Amgen.Deepavali.PetalLayerPointerInput).All(b=>!b.enabled));
                Check("Completion enters existing Shimmer",sequence.enabled && sequence.Pattern!=null && sequence.Pattern.Mode==Amgen.Deepavali.PetalWheelMode.Shimmer);
                Capture("03-restored");
                Check("Existing wheel entry still works",sequence.EnterWheel());
                r.CompleteRestoration();r.Advance(10,Vector2.zero,false);Check("Repeated completion does not fire twice",completed==1);
                r.ResetRestoration();
                Check("Replay resets coverage and pauses idle",r.Coverage==0 && r.CompletionReveal==0 && r.idleBehaviours.All(b=>!b.enabled));
                r.CompleteRestoration();r.Advance(2,Vector2.zero,false);Check("Next session can complete once again",completed==2);
                r.ResetRestoration();
                r.enabled=false;
                Check("Disable releases runtime mask",r.MaskTexture==null);
                r.enabled=true;
                Check("Re-enable starts a clean session",r.Coverage==0 && r.Phase==Amgen.Deepavali.RestorationPhase.Restoring);
                done=true;break;
        }
        stage++;deadline=Time.realtimeSinceStartupAsDouble+.85;
    }
    catch(System.Exception e){errors.Add(e.ToString());done=true;}
    finally
    {
        if(done)
        {
            UnityEditor.EditorApplication.update-=tick;Application.logMessageReceived-=logger;
            r.onRestored.RemoveListener(completedListener);
            Time.timeScale=originalTimeScale;lamps.enabled=true;r.ResetRestoration();
            checks["No runtime errors"]=errors.Count==0;
            var report=new{passed=checks.Values.All(x=>x),checks,metrics,errors};
            var json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(path+"runtime-validation.json",json);
            UnityEditor.SessionState.SetString("Restoration.Validation",json);
        }
    }
};
UnityEditor.SessionState.SetString("Restoration.Validation","running");
UnityEditor.EditorApplication.update+=tick;
return "Started frame-observed restoration validation";

