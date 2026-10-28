// Rebuilds from the existing source meshes; edits only the current Peacock Flower prefab.
if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Edit Mode required.");
const string root="Assets/HK_Signature/Deepavali/PetalPrototype/";
const string folder=root+"Models/Center Pad";
System.IO.Directory.CreateDirectory(folder);UnityEditor.AssetDatabase.Refresh();
float Smooth(float a,float b,float x){float t=UnityEngine.Mathf.InverseLerp(a,b,x);return t*t*(3-2*t);}
UnityEngine.Mesh BroadRoot(UnityEngine.Mesh source,string name,float baseHalfWidth,float shorten,float arch)
{
    var input=source.vertices;var inputUV=source.uv;var inputUV2=source.uv2;var inputWeights=source.boneWeights;
    float length=input.Max(v=>v.y),first=input.Where(v=>v.y>0.00001f).Min(v=>v.y);
    var rows=input.GroupBy(v=>System.Math.Round(v.y,6)).ToDictionary(g=>g.Key,g=>g.Max(v=>UnityEngine.Mathf.Abs(v.x)));
    float firstHalf=rows[System.Math.Round(first,6)];
    var vertices=new System.Collections.Generic.List<UnityEngine.Vector3>();
    var uv=new System.Collections.Generic.List<UnityEngine.Vector2>();var uv2=new System.Collections.Generic.List<UnityEngine.Vector2>();
    var weights=new System.Collections.Generic.List<UnityEngine.BoneWeight>();var map=new int[input.Length];
    float slope=(source.name.StartsWith("Outer")?-1:1)*UnityEngine.Mathf.Tan(6*UnityEngine.Mathf.Deg2Rad);
    for(int i=0;i<input.Length;i++)
    {
        var p=input[i];
        // Remove the old single-point root and its triangle fan. The first cross-section
        // becomes a broad base with two corners, while the outer tip stays pointed.
        if(p.y<first*0.5f){map[i]=-1;continue;}
        map[i]=vertices.Count;
        float t=UnityEngine.Mathf.InverseLerp(first,length,p.y);
        float rowHalf=rows[System.Math.Round(p.y,6)];
        float u=rowHalf>0.00001f?p.x/rowHalf:0;
        float blend=1-Smooth(0,0.34f,t);
        float width=rowHalf+(baseHalfWidth-firstHalf)*blend*blend;
        // A shallow inward curve across the root makes its two corners distinct.
        float baseCornerReach=source.name.StartsWith("Outer")?0.005f:0.008f;
        float x=u*width,y=t*(length-shorten)-baseCornerReach*u*u*(1-Smooth(0,0.15f,t));
        float camber=p.z-p.x*slope;
        // Preserve the sideways shingle across the broad roots so adjacent bases
        // overlap in a consistent order instead of sharing the same plane.
        float roll=x*slope;
        float z=camber*Smooth(0,0.12f,t)+roll+arch*UnityEngine.Mathf.Sin(UnityEngine.Mathf.PI*t);
        vertices.Add(new UnityEngine.Vector3(x,y,z));
        // The old root texel runs across the new base, keeping its gold edge continuous.
        uv.Add(t<0.00001f?new UnityEngine.Vector2(0.5f,0):inputUV[i]);
        uv2.Add(t<0.00001f?new UnityEngine.Vector2(inputUV2[i].x,0):inputUV2[i]);
        weights.Add(t<0.00001f?new UnityEngine.BoneWeight{boneIndex0=0,weight0=1}:inputWeights[i]);
    }
    var triangles=new System.Collections.Generic.List<int>();var originalTriangles=source.triangles;
    for(int i=0;i<originalTriangles.Length;i+=3)
    {
        int a=map[originalTriangles[i]],b=map[originalTriangles[i+1]],c=map[originalTriangles[i+2]];
        if(a<0||b<0||c<0)continue;
        triangles.Add(a);triangles.Add(b);triangles.Add(c);
    }
    string path=folder+"/"+name+".asset";
    var mesh=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(path);
    bool create=mesh==null;if(create)mesh=new UnityEngine.Mesh();else mesh.Clear();
    mesh.name=name;mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,uv2);
    mesh.boneWeights=weights.ToArray();mesh.bindposes=source.bindposes;mesh.SetTriangles(triangles,0);
    mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();mesh.UploadMeshData(false);
    if(create)UnityEditor.AssetDatabase.CreateAsset(mesh,path);else UnityEditor.EditorUtility.SetDirty(mesh);
    return mesh;
}
var innerSource=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(root+"Models/Inner Petal Shingled.asset");
var outerSource=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(root+"Models/Deepavali_OuterPetals.fbx").OfType<UnityEngine.Mesh>().Single(m=>m.name=="OuterPetal_01");
var inner=BroadRoot(innerSource,"Inner Petal Broad Base",0.055f,0.027f,0.012f);
var outer=BroadRoot(outerSource,"Outer Petal Broad Base",0.045f,0,0);
string prefabPath=root+"Prefabs/Deepavali Peacock Flower.prefab";
var prefab=UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var disc=prefab.transform.Find("Center Disc");
    // Keep the pad top at the petal roots and extend its underside down to the soil.
    disc.localPosition=new UnityEngine.Vector3(0,-0.0506f,0);disc.localScale=new UnityEngine.Vector3(1,1,1.3f);
    foreach(var motion in prefab.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>())
    {
        var wrapper=motion.transform;
        wrapper.localPosition=wrapper.localRotation*new UnityEngine.Vector3(0,-0.031f,-0.027f);
        var skin=motion.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();skin.sharedMesh=inner;
        skin.rootBone.localRotation=UnityEngine.Quaternion.Euler(9,0,0);
    }
    var outerLayer=prefab.GetComponentInChildren<Amgen.Deepavali.OuterPetalLayerMotion>();
    outerLayer.transform.localPosition=new UnityEngine.Vector3(0,-0.0215f,0);
    foreach(var skin in outerLayer.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
    {
        skin.sharedMesh=outer;skin.rootBone.localRotation=UnityEngine.Quaternion.Euler(3.3f,0,0);
    }
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
}
finally{UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);}
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return new{innerVertices=inner.vertexCount,outerVertices=outer.vertexCount,innerBaseWidth=0.11,outerBaseWidth=0.09,innerTiltDegrees=9,outerTiltDegrees=3.3,padLocalHeight=-0.0506,padThicknessScale=1.3};
