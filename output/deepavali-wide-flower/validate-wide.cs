if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required.");
var camera=UnityEngine.Camera.main;
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var layout=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliBackground>();
var input=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalLayerPointerInput>();
var skins=flower.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
var baked=new UnityEngine.Mesh();
var picked=new System.Collections.Generic.List<bool>();
var layers=new System.Collections.Generic.List<object>();
float maxBakeError=0;bool unclipped=true;
Amgen.Deepavali.PetalMotionController selected=null;
System.Action<Amgen.Deepavali.PetalPickTarget> onPick=p=>selected=p.Motion;
input.PetalPressed+=onPick;
try
{
    foreach(var group in skins.GroupBy(s=>s.GetComponentInParent<Amgen.Deepavali.OuterPetalLayerMotion>()!=null?"Outer":"Inner"))
    {
        float minX=1,maxX=0,minY=1,maxY=0;
        foreach(var skin in group)
        {
            skin.BakeMesh(baked,true);
            foreach(var vertex in baked.vertices)
            {
                var p=camera.WorldToViewportPoint(skin.transform.TransformPoint(vertex));
                minX=UnityEngine.Mathf.Min(minX,p.x);maxX=UnityEngine.Mathf.Max(maxX,p.x);
                minY=UnityEngine.Mathf.Min(minY,p.y);maxY=UnityEngine.Mathf.Max(maxY,p.y);
            }
            // Independent skinning reference verifies picking with a nonuniform parent scale.
            var source=skin.sharedMesh;int index=source.vertexCount/2;var w=source.boneWeights[index];
            int[] bi={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] weights={w.weight0,w.weight1,w.weight2,w.weight3};
            var expected=UnityEngine.Vector3.zero;
            for(int j=0;j<4;j++)expected+=(skin.bones[bi[j]].localToWorldMatrix*source.bindposes[bi[j]]).MultiplyPoint3x4(source.vertices[index])*weights[j];
            maxBakeError=UnityEngine.Mathf.Max(maxBakeError,UnityEngine.Vector3.Distance(expected,skin.transform.TransformPoint(baked.vertices[index])));
        }
        unclipped&=minX>0&&maxX<1&&minY>0&&maxY<1;
        layers.Add(new{layer=group.Key,count=group.Count(),widthPercent=(maxX-minX)*100,heightPercent=(maxY-minY)*100,bounds=new[]{minX,minY,maxX,maxY}});
    }
    foreach(var petal in flower.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>().OrderBy(p=>p.name))
    {
        var skin=petal.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();var source=skin.sharedMesh;
        int index=System.Array.IndexOf(source.vertices,source.vertices.OrderBy(v=>UnityEngine.Vector3.SqrMagnitude(v-new UnityEngine.Vector3(0,.21f,0))).First());
        var w=source.boneWeights[index];int[] bi={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] weights={w.weight0,w.weight1,w.weight2,w.weight3};
        var expected=UnityEngine.Vector3.zero;
        for(int j=0;j<4;j++)expected+=(skin.bones[bi[j]].localToWorldMatrix*source.bindposes[bi[j]]).MultiplyPoint3x4(source.vertices[index])*weights[j];
        var screen=camera.WorldToScreenPoint(expected);selected=null;
        picked.Add(input.TryPulseAtScreenPoint(screen)&&selected==petal);
    }
}
finally{input.PetalPressed-=onPick;UnityEngine.Object.Destroy(baked);}
bool scatterClear=layout.border.Where(p=>p.transform.gameObject.activeInHierarchy).All(p=>
{
    var position=p.transform.position;
    return new UnityEngine.Vector2(position.x/(layout.flowerClearance*layout.flowerClearanceStretch.x+p.clearance),
        position.z/(layout.flowerClearance*layout.flowerClearanceStretch.y+p.clearance)).sqrMagnitude>0.999f;
});
bool cameraPreserved=UnityEngine.Vector3.Distance(camera.transform.position,new UnityEngine.Vector3(0,2.16f,.938f))<0.000001f
    &&UnityEngine.Quaternion.Angle(camera.transform.rotation,UnityEngine.Quaternion.Euler(69.271f,180,0))<0.001f&&camera.fieldOfView==35;
var outer=flower.GetComponentInChildren<Amgen.Deepavali.OuterPetalLayerMotion>();
bool motionActive=flower.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>().All(p=>p.isActiveAndEnabled)&&outer.isActiveAndEnabled&&outer.PetalCount==15;
var report=new{passed=cameraPreserved&&unclipped&&scatterClear&&motionActive&&maxBakeError<0.00001f&&picked.Count==8&&picked.All(x=>x),
    cameraPreserved,layers,picked,maxBakeError,scatterClear,motionActive,scale=new[]{flower.transform.localScale.x,flower.transform.localScale.y,flower.transform.localScale.z}};
var json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("output/deepavali-wide-flower/validation.json",json);return json;
