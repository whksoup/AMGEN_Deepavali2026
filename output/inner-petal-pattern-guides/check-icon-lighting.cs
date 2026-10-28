if (!Application.isPlaying) throw new System.Exception("Play Mode required.");
var seq = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalWheelLightSequence>();
var flower = seq.flower;
var camera = Camera.main;
var oldRect = camera.rect; var oldAspect = camera.aspect; var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
var canvasStates = canvases.Select(c => c.enabled).ToArray();
const int width = 720, height = 600;
var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
var read = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
var results = new System.Collections.Generic.List<object>();
bool pass = true;
Color[] Capture() { camera.Render(); RenderTexture.active = rt; read.ReadPixels(new Rect(0,0,width,height),0,0); read.Apply(); return read.GetPixels(); }
Vector3 SurfacePoint(SkinnedMeshRenderer renderer, Vector2 point)
{
    var baked = new Mesh(); renderer.BakeMesh(baked, true);
    try
    {
        var uv = renderer.sharedMesh.uv; var tri = renderer.sharedMesh.triangles; var vertices = baked.vertices;
        for (int k=0;k<tri.Length;k+=3)
        {
            int a=tri[k],b=tri[k+1],c=tri[k+2]; var ab=uv[b]-uv[a]; var ac=uv[c]-uv[a]; var ap=point-uv[a];
            float d=ab.x*ac.y-ab.y*ac.x; if (Mathf.Abs(d)<1e-8f) continue;
            float v=(ap.x*ac.y-ap.y*ac.x)/d, w=(ab.x*ap.y-ab.y*ap.x)/d;
            if(v>=-1e-4f&&w>=-1e-4f&&v+w<=1.0001f) return renderer.transform.TransformPoint(vertices[a]*(1-v-w)+vertices[b]*v+vertices[c]*w);
        }
        throw new System.Exception("UV outside petal: "+renderer.name);
    }
    finally { UnityEngine.Object.Destroy(baked); }
}
try
{
    foreach(var canvas in canvases) canvas.enabled=false;
    camera.rect=new Rect(0,0,1,1); camera.aspect=(float)width/height; camera.targetTexture=rt;
    for(int i=0;i<8;i++)
    {
        seq.ResetPreview(); seq.PreviewResult(i); seq.Tick(.12f);
        var renderer=(SkinnedMeshRenderer)flower.GetPetalRenderer(i); var mat=renderer.sharedMaterial;
        float brightness=mat.GetFloat("_PetalLightIconBrightness");
        mat.SetFloat("_PetalLightIconBrightness",0); var dark=Capture();
        mat.SetFloat("_PetalLightIconBrightness",brightness); var lit=Capture();
        var region=mat.GetVector("_PetalLightIconRegion");
        var center=camera.WorldToViewportPoint(SurfacePoint(renderer,new Vector2(region.x,region.y)));
        var above=camera.WorldToViewportPoint(SurfacePoint(renderer,new Vector2(region.x,region.y+region.z)));
        var centerPx=new Vector2(center.x*width,center.y*height);
        float radius=Vector2.Distance(centerPx,new Vector2(above.x*width,above.y*height));
        double sum=0, sx=0,sy=0;
        for(int k=0;k<lit.Length;k++)
        {
            var delta=lit[k]-dark[k]; double energy=Math.Max(0,delta.r+delta.g+delta.b); energy*=energy;
            sum+=energy;sx+=(k%width+.5)*energy;sy+=(k/width+.5)*energy;
        }
        var centroid=sum>0?new Vector2((float)(sx/sum),(float)(sy/sum)):Vector2.zero;
        float error=Vector2.Distance(centerPx,centroid);
        bool ok=sum>.001&&error<radius*.55f; pass &= ok;
        results.Add(new {index=i,renderer=renderer.name,pass=ok,lightEnergy=sum,centroidErrorPixels=error,medallionRadiusPixels=radius});
    }
}
finally
{
    camera.rect=oldRect; camera.aspect=oldAspect;camera.targetTexture=oldTarget;RenderTexture.active=oldActive;
    for(int i=0;i<canvases.Length;i++) canvases[i].enabled=canvasStates[i];
    RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.Destroy(read); seq.ResetPreview();
}
System.IO.File.WriteAllText("output/inner-petal-pattern-guides/icon-lighting-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {pass,results},Newtonsoft.Json.Formatting.Indented));
return new {pass,checkedPetals=results.Count};

