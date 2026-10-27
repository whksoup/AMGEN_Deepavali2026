// Technical region packing, using the exact exported UV boundary. The source artwork stays unchanged.
const string root="Assets/HK_Signature/Deepavali/PetalPrototype";
const string output=root+"/Textures/OuterPetal_Regions_RGBA.png";
if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(root+"/Models/Deepavali_OuterPetals.fbx");
var mesh=model.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>().sharedMesh;
var uv=mesh.uv;var tris=mesh.triangles;
var edges=new System.Collections.Generic.Dictionary<(int,int),int>();
for(int i=0;i<tris.Length;i+=3)for(int k=0;k<3;k++)
{
    int a=tris[i+k],b=tris[i+(k+1)%3];var key=(System.Math.Min(a,b),System.Math.Max(a,b));
    edges[key]=edges.TryGetValue(key,out int count)?count+1:1;
}
var boundary=edges.Where(e=>e.Value==1).Select(e=>e.Key).ToArray();
var adjacent=new System.Collections.Generic.Dictionary<int,System.Collections.Generic.List<int>>();
foreach(var edge in boundary)foreach(var pair in new[]{edge,(edge.Item2,edge.Item1)})
{
    if(!adjacent.ContainsKey(pair.Item1))adjacent[pair.Item1]=new System.Collections.Generic.List<int>();
    adjacent[pair.Item1].Add(pair.Item2);
}
int first=adjacent.Keys.OrderBy(i=>uv[i].y).First(),previous=-1,current=first;
var outline=new System.Collections.Generic.List<UnityEngine.Vector2>();
do
{
    outline.Add(UnityEngine.Vector2.Scale(uv[current],new UnityEngine.Vector2(.202f,.346f)));
    int next=adjacent[current].First(i=>i!=previous);previous=current;current=next;
}while(current!=first);
int width=1024,height=2048;
float Smooth(float a,float b,float value){float t=UnityEngine.Mathf.Clamp01((value-a)/(b-a));return t*t*(3-2*t);}
float Band(float value,float center,float halfWidth,float aa)=>1-Smooth(halfWidth-aa,halfWidth+aa,UnityEngine.Mathf.Abs(value-center));
var source=new UnityEngine.Texture2D(2,2,UnityEngine.TextureFormat.RGBA32,false,true);
var packed=new UnityEngine.Texture2D(width,height,UnityEngine.TextureFormat.RGBA32,false,true);
try
{
    source.LoadImage(System.IO.File.ReadAllBytes(root+"/Textures/OuterPetal_Ornament_Source.png"));
    var lengths=new float[outline.Count];var starts=new float[outline.Count];float total=0;
    for(int i=0;i<outline.Count;i++){starts[i]=total;lengths[i]=UnityEngine.Vector2.Distance(outline[i],outline[(i+1)%outline.Count]);total+=lengths[i];}
    var pixels=new UnityEngine.Color32[width*height];int mainPixels=0,accentPixels=0,borderPixels=0;
    for(int y=0;y<height;y++)for(int x=0;x<width;x++)
    {
        float u=(x+.5f)/width,v=(y+.5f)/height,px=u*.202f,py=v*.346f;
        float distance2=float.PositiveInfinity,arc=0;bool inside=false;
        for(int e=0;e<outline.Count;e++)
        {
            var a=outline[e];var b=outline[(e+1)%outline.Count];float dx=b.x-a.x,dy=b.y-a.y;
            float t=UnityEngine.Mathf.Clamp01(((px-a.x)*dx+(py-a.y)*dy)/(dx*dx+dy*dy));
            float ex=px-a.x-t*dx,ey=py-a.y-t*dy,d=ex*ex+ey*ey;
            if(d<distance2){distance2=d;arc=starts[e]+t*lengths[e];}
            if((a.y>py)!=(b.y>py) && px<dx*(py-a.y)/dy+a.x)inside=!inside;
        }
        if(!inside){pixels[y*width+x]=new UnityEngine.Color(0,0,1/1.15f,0);continue;}
        float distance=UnityEngine.Mathf.Sqrt(distance2),aa=.00015f;
        float outer=Band(distance,.0028f,.00085f,aa),inner=Band(distance,.0068f,.00036f,aa);
        float along=UnityEngine.Mathf.Repeat(arc+.0038f,.0076f)-.0038f;
        float beadDistance=UnityEngine.Mathf.Sqrt((distance-.0118f)*(distance-.0118f)+along*along);
        float beads=(1-Smooth(.0011f-aa,.0011f+aa,beadDistance))*Smooth(.10f,.16f,v)*(1-Smooth(.965f,.99f,v));
        float border=UnityEngine.Mathf.Max(outer,UnityEngine.Mathf.Max(inner,beads));
        float artU=.5f+(u-.5f)/.84f;
        var sample=source.GetPixelBilinear(UnityEngine.Mathf.Clamp01(artU),v);
        float ratio=sample.g/UnityEngine.Mathf.Max(sample.r,.001f);
        float coverage=Smooth(.28f,.43f,ratio)*Smooth(.014f,.019f,distance);
        float main=Smooth(.61f,.79f,ratio);
        float ornament=UnityEngine.Mathf.Max(coverage,border);
        float mainWeight=ornament>.0001f?UnityEngine.Mathf.Clamp01((coverage*main*(1-border)+border)/ornament):0;
        float detail=UnityEngine.Mathf.Clamp(sample.r/UnityEngine.Mathf.Lerp(.36f,1f,coverage),.76f,1.1f)/1.15f;
        detail=UnityEngine.Mathf.Lerp(detail,1/1.15f,border);
        // Alpha is the border light/protection mask. Both ornament channels glow independently in the shader.
        pixels[y*width+x]=new UnityEngine.Color(ornament,mainWeight,detail,border);
        if(ornament*mainWeight>.5f)mainPixels++;
        if(ornament*(1-mainWeight)>.5f)accentPixels++;
        if(border>.5f)borderPixels++;
    }
    packed.SetPixels32(pixels);packed.Apply();System.IO.File.WriteAllBytes(output,packed.EncodeToPNG());
    UnityEditor.AssetDatabase.ImportAsset(output,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(output);
    importer.sRGBTexture=false;importer.alphaSource=UnityEditor.TextureImporterAlphaSource.FromInput;
    importer.alphaIsTransparency=false;importer.mipmapEnabled=true;importer.isReadable=false;
    importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;importer.filterMode=UnityEngine.FilterMode.Trilinear;
    importer.npotScale=UnityEditor.TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
    importer.textureCompression=UnityEditor.TextureImporterCompression.CompressedHQ;
    importer.SetPlatformTextureSettings(new UnityEditor.TextureImporterPlatformSettings{name="Standalone",overridden=true,maxTextureSize=2048,format=UnityEditor.TextureImporterFormat.BC7,compressionQuality=100});
    importer.SetPlatformTextureSettings(new UnityEditor.TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=UnityEditor.TextureImporterFormat.ETC2_RGBA8,compressionQuality=100});
    importer.SaveAndReimport();
    return new{output,width,height,boundaryVertices=outline.Count,mainPixels,accentPixels,borderPixels};
}
finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(packed);}
