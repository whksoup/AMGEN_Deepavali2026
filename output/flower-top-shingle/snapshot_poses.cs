// Run in Play Mode through Unity MCP. Captures actual imported skinning for offline clearance checks.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var outer=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.OuterPetalLayerMotion>();
var root=outer.transform.parent;
var tops=root.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>().OrderBy(t=>t.name).ToArray();
var lower=outer.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().OrderBy(s=>s.name).ToArray();
var upper=tops.Select(t=>t.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>()).ToArray();
var skins=upper.Concat(lower).ToArray();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var type=typeof(Amgen.Deepavali.PetalMotionController);
string[] fields={"idleFade","liftPhase","swayPhase","pulseWeight","currentLiftDegrees","currentSwayDegrees"};
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
    definitions.Add(new {name=s.name+"_"+s.transform.parent.name,layer=lower.Contains(s)?"outer":"top",triangles=baked.triangles});
}
var cases=new System.Collections.Generic.List<(string name,float time,int mask,int kind)>();
cases.Add(("rest",0,0,1));
for(int i=0;i<16;i++)cases.Add(("breeze_"+i,i*.75f,0,0));
for(int i=0;i<8;i++)cases.Add(("single_pulse_"+i,i*.43f,1<<i,0));
for(int i=0;i<8;i++)cases.Add(("mixed_pulse_"+i,i*.68f,i%2==0?85:170,0));
for(int i=0;i<4;i++)cases.Add(("all_pulse_"+i,i*1.4f,255,0));
cases.Add(("startup_pulse",0,255,1));
cases.Add(("opposing_sway_positive",0,255,3));
cases.Add(("opposing_sway_negative",0,255,4));
for(int i=0;i<24;i++)cases.Add(("long_idle_"+i,i*3.13f,0,0));
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
            for(int k=0;k<3;k++)
            {
                var bone=(UnityEngine.Transform)type.GetField(boneFields[k],flags).GetValue(t);
                var rest=(UnityEngine.Quaternion)type.GetField(restFields[k],flags).GetValue(t);
                apply.Invoke(t,new object[]{bone,rest,k});
            }
            if(test.kind>=2)
            {
                var lift=new UnityEngine.Vector3(.7f,3.1f,13.8f);
                // Alternate minimal/maximum lift and opposing sway to stress independent upper petals.
                if(test.kind>=3 && i%2==1)lift=new UnityEngine.Vector3(.15f,.4f,.2f);
                var sway=new UnityEngine.Vector3(.05f,.3f,1.5f)*(i%2==0?1:-1)*(test.kind==4?-1:1);
                type.GetField("currentLiftDegrees",flags).SetValue(t,lift);
                type.GetField("currentSwayDegrees",flags).SetValue(t,sway);
                for(int k=0;k<3;k++)
                {
                    var bone=(UnityEngine.Transform)type.GetField(boneFields[k],flags).GetValue(t);
                    var rest=(UnityEngine.Quaternion)type.GetField(restFields[k],flags).GetValue(t);
                    bone.localRotation=rest*UnityEngine.Quaternion.AngleAxis(lift[k],UnityEngine.Vector3.right)*UnityEngine.Quaternion.AngleAxis(sway[k],UnityEngine.Vector3.forward);
                }
            }
        }
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
        snapshots.Add(new{name=test.name,outerLift=new[]{outer.CurrentLiftDegrees.x,outer.CurrentLiftDegrees.y,outer.CurrentLiftDegrees.z},geometry});
    }
    for(int i=0;i<transforms.Length;i++)
    {
        maxLocalPositionDrift=UnityEngine.Mathf.Max(maxLocalPositionDrift,UnityEngine.Vector3.Distance(positions[i],transforms[i].localPosition));
        maxLocalScaleDrift=UnityEngine.Mathf.Max(maxLocalScaleDrift,UnityEngine.Vector3.Distance(scales[i],transforms[i].localScale));
    }
    string path=System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,"output/flower-top-shingle/pose-snapshots.json");
    var report=new{meshDefinitions=definitions,poses=snapshots,checks=new{maxRootDrift,maxBoundsOverflow,maxSyncError,maxLocalPositionDrift,maxLocalScaleDrift}};
    System.IO.File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(report));
    return new{path,poses=cases.Count,meshes=skins.Length,maxRootDrift,maxBoundsOverflow,maxSyncError,maxLocalPositionDrift,maxLocalScaleDrift};
}
finally
{
    for(int i=0;i<tops.Length;i++)for(int f=0;f<fields.Length;f++)type.GetField(fields[f],flags).SetValue(tops[i],savedFields[i][f]);
    for(int i=0;i<transforms.Length;i++)transforms[i].localRotation=rotations[i];
    outer.SynchronizePose();
    UnityEngine.Object.Destroy(baked);
}

