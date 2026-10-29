// Run after validate-finale finishes, in Play Mode. Restore all temporary settings.
var s=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliSessionController>();
var g=s.greetingOverlay;
var checks=new System.Collections.Generic.Dictionary<string,bool>();
void Check(string n,bool ok){checks[n]=ok;if(!ok)throw new System.Exception(n);}
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var begin=s.GetType().GetMethod("BeginGreeting",flags);
string saved=JsonUtility.ToJson(s.settingsPreset);
try{
 Check("Saved scene greeting reference",g!=null&&g.spawnClip!=null&&g.idleClip!=null&&g.hideClip!=null&&g.logo.texture!=null);
 foreach(var clip in new[]{g.spawnClip,g.idleClip,g.hideClip}){
  var imp=(UnityEditor.VideoClipImporter)UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(clip));
  Check(clip.name+" VP8 alpha import",imp.keepAlpha&&!imp.importAudio&&imp.defaultTargetSettings.enableTranscoding&&imp.defaultTargetSettings.codec==UnityEditor.VideoCodec.VP8);
 }
 s.greetingHoldSeconds=7.25f;
 Check("Settings save",s.SaveSettingsToPreset(out var message));
 Check("Setting written to asset",System.IO.File.ReadAllText(UnityEditor.AssetDatabase.GetAssetPath(s.settingsPreset)).Contains("greetingHoldSeconds: 7.25"));
 s.greetingHoldSeconds=0;s.settingsPreset.ApplyTo(s);Check("Settings load",s.greetingHoldSeconds==7.25f);
 s.EndSequence();s.greetingOverlay=null;begin.Invoke(s,null);
 Check("Missing overlay returns to base",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration);
 s.greetingOverlay=g;g.enabled=false;begin.Invoke(s,null);
 Check("Disabled overlay returns to base",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration);g.enabled=true;
 begin.Invoke(s,null);
 // Force the no-progress deadline, without waiting fifteen seconds or altering a clip.
 typeof(HappyDeepavaliOverlay).GetField("lastProgress",flags).SetValue(g,Time.realtimeSinceStartupAsDouble-30);
 var vp=(UnityEngine.Video.VideoPlayer)typeof(HappyDeepavaliOverlay).GetField("pending",flags).GetValue(g);
 typeof(HappyDeepavaliOverlay).GetField("lastFrame",flags).SetValue(g,vp.frame);
 typeof(HappyDeepavaliOverlay).GetMethod("Update",flags).Invoke(g,null);
 Check("Stall watchdog clears overlay",g.CurrentState==HappyDeepavaliOverlay.State.Hidden&&!string.IsNullOrEmpty(g.LastError)&&!g.GetComponent<UnityEngine.UI.RawImage>().enabled&&!g.logo.enabled);
 s.GetType().GetMethod("TickGreeting",flags).Invoke(s,null);
 Check("Stall returns session to base",s.Phase==Amgen.Deepavali.DeepavaliSessionPhase.AwaitingRestoration);
}finally{
 s.greetingOverlay=g;g.enabled=true;
 JsonUtility.FromJsonOverwrite(saved,s.settingsPreset);UnityEditor.EditorUtility.SetDirty(s.settingsPreset);UnityEditor.AssetDatabase.SaveAssetIfDirty(s.settingsPreset);
 s.settingsPreset.ApplyTo(s);s.EndSequence();
 System.IO.File.WriteAllText("output/happy-deepavali/saved-and-failure-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));
}
return checks;
