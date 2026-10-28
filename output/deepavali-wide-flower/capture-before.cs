var camera=UnityEngine.Camera.main;
var layout=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliBackground>();
var oldAspect=camera.aspect;var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;
try
{
    foreach(var size in new[]{new UnityEngine.Vector2Int(1600,900),new UnityEngine.Vector2Int(2560,1118)})
    {
        var rt=UnityEngine.RenderTexture.GetTemporary(size.x,size.y,24,UnityEngine.RenderTextureFormat.ARGBHalf,UnityEngine.RenderTextureReadWrite.Linear);
        var readback=new UnityEngine.Texture2D(size.x,size.y,UnityEngine.TextureFormat.RGBAFloat,false,true);
        var png=new UnityEngine.Texture2D(size.x,size.y,UnityEngine.TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.aspect=size.x/(float)size.y;layout.RefreshLayout(camera);
            camera.Render();UnityEngine.RenderTexture.active=rt;
            readback.ReadPixels(new UnityEngine.Rect(0,0,size.x,size.y),0,0);readback.Apply();
            var pixels=readback.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
            png.SetPixels(pixels);png.Apply();
            System.IO.File.WriteAllBytes("output/deepavali-wide-flower/before-"+size.x+"x"+size.y+".png",png.EncodeToPNG());
        }
        finally { camera.targetTexture=oldTarget;UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(readback);UnityEngine.Object.DestroyImmediate(png); }
    }
}
finally {camera.aspect=oldAspect;layout.RefreshLayout(camera);}
return "Captured 16:9 and current wide-wall composition.";


