// Isolated instance: deterministic unscaled animation clocks, real lifecycle and shader state.
var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
var source=s.handPrompt;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var type=typeof(Amgen.Deepavali.HandHereInteraction);
var clock=type.GetField("startedAt",flags);var update=type.GetMethod("Update",flags);
var clone=UnityEngine.Object.Instantiate(source.gameObject,source.transform.parent);
var h=clone.GetComponent<Amgen.Deepavali.HandHereInteraction>();var g=clone.GetComponent<Amgen.Deepavali.HandHereGraphic>();
var checks=new System.Collections.Generic.Dictionary<string,bool>();
void Check(string n,bool ok){checks[n]=ok;if(!ok)throw new System.Exception(n);}
void Advance(float age){clock.SetValue(h,Time.unscaledTimeAsDouble-age);update.Invoke(h,null);}
int fired=0,unityFired=0;h.Despawned+=()=>fired++;
var evt=(UnityEngine.Events.UnityEvent)type.GetField("onDespawned",flags).GetValue(h);evt.AddListener(()=>unityFired++);
float oldScale=Time.timeScale;
try{
 h.ResetPrompt();
 Check("Instantiation starts fully dissolved with idle ring",h.IsAppearing&&g.material.GetFloat("_Dissolve")==1&&g.material.GetFloat("_Loading")==0);
 Check("Runtime material is independent",g.material!=source.GetComponent<Amgen.Deepavali.HandHereGraphic>().material);
 Advance(.4f);float middle=g.material.GetFloat("_Dissolve");h.BeginDisintegration();h.Dismiss();
 Check("Early dismissal queues once without visual jump",h.IsAppearing&&h.IsDismissing&&Mathf.Abs(g.material.GetFloat("_Dissolve")-middle)<.001f);
 Advance(.86f);Check("Queued dismissal starts after complete assembly",!h.IsAppearing&&h.IsDismissing&&g.material.GetFloat("_Dissolve")==0&&g.material.GetFloat("_LoadProgress")==1);
 Advance(.86f);Check("Completed dismissal fires both events exactly once",!clone.activeSelf&&fired==1&&unityFired==1);
 h.ResetPrompt();h.BeginDisintegration();h.ResetPrompt();Advance(.9f);
 Check("Reset cancels queued dismissal",clone.activeSelf&&!h.IsAppearing&&!h.IsDismissing&&fired==1&&unityFired==1);
 h.ResetPrompt();h.Dismiss();h.CancelAndHide();
 Check("Cancel clears pending assembly and dismissal",!clone.activeSelf&&!h.IsAppearing&&!h.IsDismissing&&h.DwellProgress==0&&fired==1);
 clone.SetActive(true);Check("Re-enable replays reverse dissolve",h.IsAppearing&&g.material.GetFloat("_Dissolve")==1);
 h.enabled=false;h.enabled=true;Check("Component re-enable replays appearance",h.IsAppearing&&g.material.GetFloat("_Dissolve")==1);
 Time.timeScale=0;Advance(.9f);Check("Appearance uses unscaled time",!h.IsAppearing&&g.material.GetFloat("_Dissolve")==0);
 h.externallyDriven=false;h.Dismiss();Advance(.4f);Check("Normal dismissal retains loading phase",h.IsDismissing&&g.material.GetFloat("_Dissolve")==0&&g.material.GetFloat("_LoadProgress")>0);
 Advance(.9f);Check("Normal dismissal retains outward dissolve",g.material.GetFloat("_Dissolve")>0&&g.material.GetFloat("_Dissolve")<1);
 Advance(1.6f);Check("Normal loading and despawn finish once",fired==2&&unityFired==2&&!clone.activeSelf);
 Check("Hand shader has no compiler errors",!UnityEditor.ShaderUtil.GetShaderMessages(g.material.shader).Any(x=>x.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error));
}finally{Time.timeScale=oldScale;UnityEngine.Object.DestroyImmediate(clone);}
var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,checks},Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("output/session-polish/hand-validation.json",json);return json;
