// Isolated, actual Unity meshes: inner at left, outer at right. No scene changes persist.
var flower=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var sources=new[]{flower.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().First(s=>s.GetComponentInParent<Amgen.Deepavali.PetalMotionController>()!=null),
    flower.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().First(s=>s.name=="OuterPetal_01")};
var temporary=new System.Collections.Generic.List<UnityEngine.GameObject>();
var rt=UnityEngine.RenderTexture.GetTemporary(1200,800,24,UnityEngine.RenderTextureFormat.ARGBHalf,UnityEngine.RenderTextureReadWrite.Linear);
var readback=new UnityEngine.Texture2D(1200,800,UnityEngine.TextureFormat.RGBAFloat,false,true);
var png=new UnityEngine.Texture2D(1200,800,UnityEngine.TextureFormat.RGB24,false);
var oldActive=UnityEngine.RenderTexture.active;
try
{
    for(int i=0;i<2;i++)
    {
        var obj=new UnityEngine.GameObject("Temporary Petal Shape "+i);temporary.Add(obj);obj.layer=31;
        obj.transform.position=new UnityEngine.Vector3(i==0?.16f:-.16f,0,i==0?-.04f:0);
        obj.transform.rotation=UnityEngine.Quaternion.Euler(270,0,0);
        obj.AddComponent<UnityEngine.MeshFilter>().sharedMesh=sources[i].sharedMesh;
        obj.AddComponent<UnityEngine.MeshRenderer>().sharedMaterial=sources[i].sharedMaterial;
    }
    var cameraObject=new UnityEngine.GameObject("Temporary Petal Shape Camera");temporary.Add(cameraObject);
    var camera=cameraObject.AddComponent<UnityEngine.Camera>();camera.enabled=false;camera.orthographic=true;
    camera.orthographicSize=.245f;camera.aspect=1.5f;camera.cullingMask=1<<31;
    camera.transform.position=new UnityEngine.Vector3(0,1,-.17f);camera.transform.rotation=UnityEngine.Quaternion.Euler(90,180,0);
    camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.035f,.025f,.03f);
    camera.nearClipPlane=.01f;camera.farClipPlane=4;camera.allowHDR=true;
    cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=true;
    camera.targetTexture=rt;camera.Render();UnityEngine.RenderTexture.active=rt;
    readback.ReadPixels(new UnityEngine.Rect(0,0,1200,800),0,0);readback.Apply();
    var pixels=readback.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
    png.SetPixels(pixels);png.Apply();System.IO.File.WriteAllBytes("output/deepavali-center-pad/petal-shapes.png",png.EncodeToPNG());
}
finally
{
    UnityEngine.RenderTexture.active=oldActive;
    foreach(var obj in temporary)UnityEngine.Object.DestroyImmediate(obj);
    UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(readback);UnityEngine.Object.DestroyImmediate(png);
}
return "Rendered isolated inner and outer petal shapes.";
