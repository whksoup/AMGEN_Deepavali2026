// Run in Play Mode through Unity MCP. Captures actual imported skinning for offline clearance checks.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var outer=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.OuterPetalLayerMotion>();
var root=outer.transform.parent;
var ring=root.GetComponent<Amgen.Deepavali.PetalRingPulse>();
if(ring==null)throw new System.InvalidOperationException("Missing ring pulse component.");
var tops=root.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>().OrderBy(t=>t.name).ToArray();
var lower=outer.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().OrderBy(s=>s.name).ToArray();
var upper=tops.Select(t=>t.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>()).ToArray();
var skins=upper.Concat(lower).ToArray();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var type=typeof(Amgen.Deepavali.PetalMotionController);
string[] fields={"idleFade","liftPhase","swayPhase","pulseWeight","followPulseWeight","currentLiftDegrees","currentSwayDegrees"};
var savedFields=tops.Select(t=>fields.Select(f=>type.GetField(f,flags).GetValue(t)).ToArray()).ToArray();
var transforms=root.GetComponentsInChildren<UnityEngine.Transform>();
var rotations=transforms.Select(t=>t.localRotation).ToArray();
var positions=transforms.Select(t=>t.localPosition).ToArray();
var scales=transforms.Select(t=>t.localScale).ToArray();
string[] boneFields={"baseBone","middleBone","tipBone"},restFields={"baseRest","middleRest","tipRest"};
var apply=type.GetMethod("ApplyBone",flags);
var baked=new UnityEngine.Mesh();
var definitions=new System.Collections.Generic.List<object>();
foreach(var s in skins)
{
    s.BakeMesh(baked);
    definitions.Add(new {name=lower.Contains(s)?s.name:tops[System.Array.IndexOf(upper,s)].name,layer=lower.Contains(s)?"outer":"top",triangles=baked.triangles});
}
var cases=new System.Collections.Generic.List<(string name,float time,int mask,int kind)>();
cases.Add(("rest",0,0,1));
for(int i=0;i<16;i++)cases.Add(("idle_"+i,i*3.13f,0,0));
for(int i=0;i<8;i++)cases.Add(("single_"+i,i*.43f,1<<i,0));
for(int i=0;i<8;i++)cases.Add(("adjacent_"+i,i*.71f,(1<<i)|(1<<((i+1)%8)),0));
for(int i=0;i<8;i++)cases.Add(("alternating_"+i,i*.68f,i%2==0?85:170,0));
for(int i=0;i<4;i++)cases.Add(("all_"+i,i*1.4f,255,0));
for(int i=0;i<16;i++)cases.Add(("independent_release_"+i,i*.317f,0,2));
cases.Add(("startup_pulse",0,255,1));
var random=new System.Random(20261028);
var snapshots=new System.Collections.Generic.List<object>();
float maxRootDrift=0,maxBoundsOverflow=0,maxSyncError=0,maxLocalPositionDrift=0,maxLocalScaleDrift=0;
try
{
    foreach(var test in cases)
    {
        for(int i=0;i<tops.Length;i++)
        {
            var t=tops[i];
            float phase=(float)type.GetField("phaseOffset",flags).GetValue(t);
            float frequency=(float)type.GetField("idleFrequency",flags).GetValue(t);
            type.GetField("idleFade",flags).SetValue(t,test.kind==1?0f:1f);
            type.GetField("liftPhase",flags).SetValue(t,test.time*frequency*2*UnityEngine.Mathf.PI+phase);
            type.GetField("swayPhase",flags).SetValue(t,test.time*frequency*.67f*2*UnityEngine.Mathf.PI+phase);
            type.GetField("pulseWeight",flags).SetValue(t,(test.mask&(1<<i))!=0?1f:0f);
            if(test.kind==2)type.GetField("pulseWeight",flags).SetValue(t,(float)random.NextDouble());
        }
        ring.SynchronizePose();
        outer.SynchronizePose();
        var minimum=new UnityEngine.Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
        foreach(var top in tops) minimum=UnityEngine.Vector3.Min(minimum,top.CurrentLiftDegrees);
        for(int k=0;k<3;k++)maxSyncError=UnityEngine.Mathf.Max(maxSyncError,UnityEngine.Mathf.Abs(outer.CurrentLiftDegrees[k]-minimum[k]*(minimum[k]>=0?.72f:1.35f)));
        var geometry=new System.Collections.Generic.List<float[]>();
        foreach(var s in skins)
        {
            s.BakeMesh(baked);
            var vertices=baked.vertices;
            var xyz=new float[vertices.Length*3];
            float closest=float.PositiveInfinity;
            var baseBone=s.bones.Single(b=>b.name.EndsWith("_Base"));
            for(int i=0;i<vertices.Length;i++)
            {
                var world=s.transform.TransformPoint(vertices[i]);
                xyz[3*i]=world.x; xyz[3*i+1]=world.z; xyz[3*i+2]=world.y;
                closest=UnityEngine.Mathf.Min(closest,UnityEngine.Vector3.Distance(world,baseBone.position));
                maxBoundsOverflow=UnityEngine.Mathf.Max(maxBoundsOverflow,UnityEngine.Vector3.Distance(vertices[i],s.localBounds.ClosestPoint(vertices[i])));
            }
            maxRootDrift=UnityEngine.Mathf.Max(maxRootDrift,closest);
            geometry.Add(xyz);
        }
        snapshots.Add(new{name=test.name,directPulse=tops.Select(t=>t.PulseWeight).ToArray(),effectivePulse=tops.Select(t=>t.EffectivePulseWeight).ToArray(),outerLift=new[]{outer.CurrentLiftDegrees.x,outer.CurrentLiftDegrees.y,outer.CurrentLiftDegrees.z},geometry});
    }
    for(int i=0;i<transforms.Length;i++)
    {
        maxLocalPositionDrift=UnityEngine.Mathf.Max(maxLocalPositionDrift,UnityEngine.Vector3.Distance(positions[i],transforms[i].localPosition));
        maxLocalScaleDrift=UnityEngine.Mathf.Max(maxLocalScaleDrift,UnityEngine.Vector3.Distance(scales[i],transforms[i].localScale));
    }
    string path=System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,"output/flower-rustle/pose-snapshots.json");
    var report=new{meshDefinitions=definitions,poses=snapshots,checks=new{maxRootDrift,maxBoundsOverflow,maxSyncError,maxLocalPositionDrift,maxLocalScaleDrift}};
    System.IO.File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(report));
    return new{path,poses=cases.Count,meshes=skins.Length,maxRootDrift,maxBoundsOverflow,maxSyncError,maxLocalPositionDrift,maxLocalScaleDrift};
}
finally
{
    for(int i=0;i<tops.Length;i++)for(int f=0;f<fields.Length;f++)type.GetField(fields[f],flags).SetValue(tops[i],savedFields[i][f]);
    for(int i=0;i<transforms.Length;i++)transforms[i].localRotation=rotations[i];
    ring.SynchronizePose();
    outer.SynchronizePose();
    UnityEngine.Object.Destroy(baked);
}

