var camera = UnityEngine.Camera.main;
var layout = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.DeepavaliBackground>();
var hand = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.HandHereGraphic>();
var oldTarget = camera.targetTexture;
var oldActive = UnityEngine.RenderTexture.active;
var oldAspect = camera.aspect;
var results = new System.Collections.Generic.List<string>();
try
{
    foreach (var size in new[] { new UnityEngine.Vector2Int(1600,900), new UnityEngine.Vector2Int(2560,1118) })
    {
        var rt = UnityEngine.RenderTexture.GetTemporary(size.x,size.y,24,UnityEngine.RenderTextureFormat.ARGBHalf,UnityEngine.RenderTextureReadWrite.Linear);
        var readback = new UnityEngine.Texture2D(size.x,size.y,UnityEngine.TextureFormat.RGBAFloat,false,true);
        var png = new UnityEngine.Texture2D(size.x,size.y,UnityEngine.TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture = rt; camera.aspect = (float)size.x / size.y;
            if (layout != null) layout.RefreshLayout(camera);
            UnityEngine.Canvas.ForceUpdateCanvases();
            camera.Render(); UnityEngine.RenderTexture.active = rt;
            readback.ReadPixels(new UnityEngine.Rect(0,0,size.x,size.y),0,0); readback.Apply();
            var pixels = readback.GetPixels();
            for (int i=0;i<pixels.Length;i++) pixels[i]=pixels[i].gamma;
            png.SetPixels(pixels); png.Apply();
            System.IO.File.WriteAllBytes("output/hand-guidance/preview-"+size.x+"x"+size.y+".png",png.EncodeToPNG());
            var corners = new UnityEngine.Vector3[4]; hand.rectTransform.GetWorldCorners(corners);
            var a = camera.WorldToScreenPoint(corners[0]); var b = camera.WorldToScreenPoint(corners[2]);
            if (UnityEngine.Mathf.Abs((b.x-a.x)-(b.y-a.y))>1f) throw new System.Exception("Prompt is distorted");
            if (a.x<0 || a.y<0 || b.x>size.x || b.y>size.y) throw new System.Exception("Prompt outside screen");
            results.Add(size+": square and fully on screen; "+(b.x-a.x)+"px");
            if (size.x==1600)
            {
                var detail = new UnityEngine.Texture2D(240,240,UnityEngine.TextureFormat.RGB24,false);
                try { detail.SetPixels(png.GetPixels((int)((a.x+b.x)*0.5f)-120,(int)((a.y+b.y)*0.5f)-120,240,240)); detail.Apply(); System.IO.File.WriteAllBytes("output/hand-guidance/detail.png",detail.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(detail); }
            }
        }
        finally
        {
            camera.targetTexture=oldTarget; UnityEngine.RenderTexture.active=oldActive;
            UnityEngine.RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(readback); UnityEngine.Object.DestroyImmediate(png);
        }
    }
}
finally
{
    camera.aspect=oldAspect;
    if(layout!=null) layout.RefreshLayout(camera);
    UnityEngine.Canvas.ForceUpdateCanvases();
}
if(!hand.raycastTarget || hand.canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>()==null)
    throw new System.Exception("Clickable guidance needs UI raycasting");
System.IO.File.WriteAllLines("output/hand-guidance/validation.txt",results);
return results;
