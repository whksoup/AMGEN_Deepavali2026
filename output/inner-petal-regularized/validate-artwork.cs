if(!Application.isPlaying)throw new System.Exception("Play Mode required");
var c=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var preview=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerAppearancePreview>(FindObjectsInactive.Include);
bool active=preview.gameObject.activeSelf;preview.gameObject.SetActive(true);
var controls=c.GetComponentsInChildren<Amgen.Deepavali.PetalArtworkController>().OrderBy(a=>a.PetalIndex).ToArray();
var motion=c.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>()[0];
var surface=(SkinnedMeshRenderer)c.GetPetalRenderer(0);var bone=surface.bones.Single(b=>b.name.EndsWith("_Base"));var root=bone.position;
var data=new SerializedObject(c);var entries=data.FindProperty("petalAppearances");
var durations=Enumerable.Range(0,22).Select(i=>entries.GetArrayElementAtIndex(i).FindPropertyRelative("artworkDuration").floatValue).ToArray();
var checks=new Dictionary<string,bool>();var errors=new List<string>();
Application.LogCallback logger=(m,t,k)=>{if(k==LogType.Error||k==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=logger;
c.SetState(Amgen.Deepavali.FlowerPaletteState.Base);
for(int i=0;i<22;i++){c.SetSymbol(i,-1);entries.GetArrayElementAtIndex(i).FindPropertyRelative("artworkDuration").floatValue=.6f;}data.ApplyModifiedPropertiesWithoutUndo();
preview.SelectAll();preview.CycleSymbol();
var bindings=new SerializedObject(preview).FindProperty("artworkControls");
checks["reviewUiBindsEveryPetalComponent"]=bindings.arraySize==22&&Enumerable.Range(0,22).All(i=>bindings.GetArrayElementAtIndex(i).objectReferenceValue==controls[i]);
checks["reviewUiStartsAllPetalReveals"]=controls.All(a=>a.IsTransitioning&&a.RequestedArtwork==0);
controls[0].NextArtwork();controls[0].NextArtwork();controls[1].SetArtwork(3);c.TransitionTo(Amgen.Deepavali.FlowerPaletteState.Blue);motion.TriggerPulse();
var mapping=surface.sharedMaterial.GetVector("_ArtworkUvToRoot");
checks["liveArtworkAndPaletteAdvanceTogether"]=false;checks["motionPreservesArtworkRootCoordinates"]=false;
double started=Time.realtimeSinceStartupAsDouble;int frame=-1;
SessionState.SetString("Refined.Artwork","running");
EditorApplication.CallbackFunction tick=null;
tick=()=>{
 bool done=false;
 try{
  if(!Application.isPlaying)throw new System.Exception("Play Mode stopped");if(Time.frameCount==frame)return;frame=Time.frameCount;
  checks["liveArtworkAndPaletteAdvanceTogether"]|=controls[0].Progress>0&&controls[0].Progress<1&&c.TransitionProgress>0&&c.IsTransitioning;
  if(motion.PulseWeight>.05f)checks["motionPreservesArtworkRootCoordinates"]|=surface.sharedMaterial.GetVector("_ArtworkUvToRoot")==mapping&&Vector3.Distance(bone.position,root)<.000001f;
  if(controls.Any(a=>a.IsTransitioning)||c.IsTransitioning){if(Time.realtimeSinceStartupAsDouble-started>8)throw new System.Exception("Timed out");return;}
  checks["liveLatestChoiceWinsPerPetal"]=controls[0].CurrentArtwork==2&&controls[1].CurrentArtwork==3&&controls.Skip(2).All(a=>a.CurrentArtwork==0);
  controls[0].SetArtwork(1);controls[0].SetArtwork(3);c.enabled=false;c.enabled=true;
  checks["reenableAppliesLatestQueuedArtwork"]=controls[0].CurrentArtwork==3&&!controls[0].IsTransitioning&&!controls[0].HasPendingArtwork;
  checks["runtimeMaterialsMatchPetalsPlusCenter"]=Enumerable.Range(0,22).Select(i=>c.GetPetalRenderer(i).sharedMaterial).Distinct().Count()==22;
  done=true;
 }catch(Exception ex){errors.Add(ex.ToString());done=true;}
 finally{if(done){EditorApplication.update-=tick;Application.logMessageReceived-=logger;c.ResetToInitialState();data.Update();entries=data.FindProperty("petalAppearances");for(int i=0;i<22;i++)entries.GetArrayElementAtIndex(i).FindPropertyRelative("artworkDuration").floatValue=durations[i];data.ApplyModifiedPropertiesWithoutUndo();preview.SelectAll();preview.gameObject.SetActive(active);checks["noRuntimeErrors"]=errors.Count==0;var report=new{passed=checks.Values.All(v=>v),petalCount=22,checks,errors};var json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText("output/inner-petal-regularized/artwork-validation.json",json);SessionState.SetString("Refined.Artwork",json);}}
};EditorApplication.update+=tick;return "Started frame-observed artwork/queue/motion validation";
