var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
if(LaunchSceneController.Instance!=null)throw new System.Exception("This isolated host check requires the standalone wall scene");
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var root=new GameObject("Temporary validation host");root.SetActive(false);
var host=root.AddComponent<LaunchSceneController>();host.enabled=false;root.SetActive(true);
var checks=new System.Collections.Generic.Dictionary<string,bool>();
void Check(string n,bool ok){checks[n]=ok;if(!ok)throw new System.Exception(n);}
try{
 typeof(Amgen.Deepavali.DeepavaliSessionController).GetMethod("BindHostInactivity",flags).Invoke(s,null);
 Check("Active session owns the host timeout",host.InactivityOwner==s);
 var elapsed=typeof(LaunchSceneController).GetField("timeSinceLastInput",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
 elapsed.SetValue(null,999f);typeof(LaunchSceneController).GetMethod("Update",flags).Invoke(host,null);
 Check("Legacy screensaver cannot pre-empt the 30-second fade",(float)elapsed.GetValue(null)==0);
 s.enabled=false;Check("Disabling session releases host ownership",host.InactivityOwner==null);
}finally{
 s.enabled=true;
 var controls=typeof(LaunchSceneController).GetField("controls",flags).GetValue(host) as System.IDisposable;controls?.Dispose();
 UnityEngine.Object.DestroyImmediate(root);s.endSequence();
}
var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,checks},Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText("output/session-polish/host-validation.json",json);return json;
