if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required.");
var c=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var preview=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerAppearancePreview>();
var canvas=UnityEngine.GameObject.Find("Test Instructions").GetComponent<UnityEngine.Canvas>();
var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
var camera=UnityEngine.Camera.main;
var oldMode=canvas.renderMode;var oldCanvasCamera=canvas.worldCamera;var oldPlane=canvas.planeDistance;var oldScale=canvas.scaleFactor;bool oldScaler=scaler.enabled;
var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;
var rt=UnityEngine.RenderTexture.GetTemporary(1280,800,24,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.sRGB);
var tex=new UnityEngine.Texture2D(1280,800,UnityEngine.TextureFormat.RGB24,false);
try
{
    for(int i=0;i<c.PetalCount;i++){c.SetPetalState(i,(Amgen.Deepavali.FlowerPaletteState)(i%7));c.SetSymbol(i,i%4);}
    preview.SelectAll();
    camera.targetTexture=rt;
    scaler.enabled=false;canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=0.5f;canvas.scaleFactor=1;
    UnityEngine.Canvas.ForceUpdateCanvases();
    camera.Render();UnityEngine.RenderTexture.active=rt;tex.ReadPixels(new UnityEngine.Rect(0,0,1280,800),0,0);tex.Apply();
    System.IO.File.WriteAllBytes("output/flower-appearance/06-review-controls.png",tex.EncodeToPNG());
    return "Captured review controls with mixed palettes and sample symbols.";
}
finally
{
    canvas.renderMode=oldMode;canvas.worldCamera=oldCanvasCamera;canvas.planeDistance=oldPlane;scaler.enabled=oldScaler;canvas.scaleFactor=oldScale;
    camera.targetTexture=oldTarget;UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(tex);
    c.SetState(Amgen.Deepavali.FlowerPaletteState.Base);for(int i=0;i<c.PetalCount;i++)c.SetSymbol(i,-1);preview.SelectAll();
}
