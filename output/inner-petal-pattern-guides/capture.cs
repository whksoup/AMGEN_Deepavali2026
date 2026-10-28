var seq=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalWheelLightSequence>();seq.ResetPreview();seq.Tick(2);
var camera=Camera.main;var rect=camera.rect;float aspect=camera.aspect;var target=camera.targetTexture;var active=RenderTexture.active;
int width=1400,height=1167;
var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
var read=new Texture2D(width,height,TextureFormat.RGBAFloat,false,true);var png=new Texture2D(width,height,TextureFormat.RGB24,false);
var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();bool canvasEnabled=canvas!=null&&canvas.enabled;try{if(canvas!=null)canvas.enabled=false;camera.rect=new Rect(0,0,1,1);camera.aspect=(float)width/height;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,width,height),0,0);read.Apply();png.SetPixels(read.GetPixels().Select(c=>c.gamma).ToArray());png.Apply();System.IO.File.WriteAllBytes("output/inner-petal-pattern-guides/after.png",png.EncodeToPNG());}
finally{if(canvas!=null)canvas.enabled=canvasEnabled;camera.rect=rect;camera.aspect=aspect;camera.targetTexture=target;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(read);UnityEngine.Object.Destroy(png);}
return "Captured SVG-derived inner-petal patterns in Unity";


