if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required");
var outer=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.OuterPetalLayerMotion>();
var tops=outer.transform.parent.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>().OrderBy(t=>t.name).ToArray();
var input=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalLayerPointerInput>();
var bones=outer.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().SelectMany(s=>s.bones).Distinct().ToArray();
var positions=bones.Select(b=>b.localPosition).ToArray();
var scales=bones.Select(b=>b.localScale).ToArray();
var sourceRoot=tops[0].GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>().bones.Single(b=>b.name.EndsWith("_Base"));
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var rest=(UnityEngine.Quaternion[])typeof(Amgen.Deepavali.OuterPetalLayerMotion).GetField("restRotations",flags).GetValue(outer);
var orderedBones=(UnityEngine.Transform[])typeof(Amgen.Deepavali.OuterPetalLayerMotion).GetField("outerBones",flags).GetValue(outer);
double start=UnityEngine.Time.realtimeSinceStartupAsDouble;
int lastFrame=-1,samples=0;
bool clicked=false,attempted=false,allTriggered=false,resetChecked=false;
float singlePeak=0,allPeak=0,maxError=0,maxPositionDrift=0,maxScaleDrift=0,resetError=0;
float minLift=float.PositiveInfinity,maxLift=float.NegativeInfinity;
UnityEditor.SessionState.SetString("OuterLayer.RuntimeValidation","running");
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    try
    {
        if(!UnityEditor.EditorApplication.isPlaying || outer==null)throw new System.InvalidOperationException("Play Mode stopped during validation");
        if(lastFrame==UnityEngine.Time.frameCount)return;
        lastFrame=UnityEngine.Time.frameCount;samples++;
        double elapsed=UnityEngine.Time.realtimeSinceStartupAsDouble-start;
        if(elapsed>.5 && !attempted)
        {
            attempted=true;
            var point=UnityEngine.Camera.main.WorldToScreenPoint(sourceRoot.TransformPoint(new UnityEngine.Vector3(0,.16f,0)));
            clicked=input.TryPulseAtScreenPoint(new UnityEngine.Vector2(point.x,point.y));
        }
        if(elapsed<3)singlePeak=UnityEngine.Mathf.Max(singlePeak,tops[0].PulseWeight);
        if(elapsed>=3 && !allTriggered){allTriggered=true;foreach(var top in tops)top.TriggerPulse();}
        if(elapsed>=3 && elapsed<5)allPeak=UnityEngine.Mathf.Max(allPeak,tops.Min(t=>t.PulseWeight));
        var minimum=new UnityEngine.Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
        foreach(var top in tops)minimum=UnityEngine.Vector3.Min(minimum,top.CurrentLiftDegrees);
        for(int k=0;k<3;k++)maxError=UnityEngine.Mathf.Max(maxError,UnityEngine.Mathf.Abs(outer.CurrentLiftDegrees[k]-minimum[k]*(minimum[k]>=0?.72f:1.35f)));
        minLift=UnityEngine.Mathf.Min(minLift,outer.CurrentLiftDegrees.x);
        maxLift=UnityEngine.Mathf.Max(maxLift,outer.CurrentLiftDegrees.x);
        for(int i=0;i<bones.Length;i++)
        {
            maxPositionDrift=UnityEngine.Mathf.Max(maxPositionDrift,UnityEngine.Vector3.Distance(positions[i],bones[i].localPosition));
            maxScaleDrift=UnityEngine.Mathf.Max(maxScaleDrift,UnityEngine.Vector3.Distance(scales[i],bones[i].localScale));
        }
        if(elapsed>=7 && !resetChecked)
        {
            outer.enabled=false;
            for(int i=0;i<orderedBones.Length;i++)resetError=UnityEngine.Mathf.Max(resetError,UnityEngine.Quaternion.Angle(rest[i],orderedBones[i].localRotation));
            outer.enabled=true;outer.SynchronizePose();resetChecked=true;
        }
        if(elapsed<10)return;
        bool passed=samples>30 && clicked && singlePeak>.2f && allPeak>.2f && maxError<.0001f && maxPositionDrift<1e-7f && maxScaleDrift<1e-7f && resetError<.001f && maxLift-minLift>.2f && outer.isActiveAndEnabled;
        var report=new{passed,samples,clicked,singlePeak,allPeak,maxError,maxPositionDrift,maxScaleDrift,resetError,outerLiftTravelDegrees=maxLift-minLift};
        string json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
        UnityEditor.SessionState.SetString("OuterLayer.RuntimeValidation",json);
        System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,"output/flower-petal-prototype/unity-outer-runtime-validation.json"),json);
        UnityEditor.EditorApplication.update-=tick;
    }
    catch(System.Exception e)
    {
        UnityEditor.SessionState.SetString("OuterLayer.RuntimeValidation","FAILED: "+e);
        UnityEditor.EditorApplication.update-=tick;
    }
};
UnityEditor.EditorApplication.update+=tick;
return "Runtime validation started: 10 seconds, real pointer picking, single/all lift, phase synchronization, and disable/enable reset.";
