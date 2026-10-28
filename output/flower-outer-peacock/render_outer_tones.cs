if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Edit Mode required.");
var f=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();var so=new UnityEditor.SerializedObject(f);
var source=(UnityEngine.SkinnedMeshRenderer)so.FindProperty("petals").GetArrayElementAtIndex(8).objectReferenceValue;
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var mesh=new UnityEngine.Mesh();source.BakeMesh(mesh);mesh.RecalculateBounds();
var rt=UnityEngine.RenderTexture.GetTemporary(700,1000,24,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.sRGB);
var image=new UnityEngine.Texture2D(700,1000,UnityEngine.TextureFormat.RGB24,false);var oldActive=UnityEngine.RenderTexture.active;
UnityEngine.GameObject Make(string name){var go=new UnityEngine.GameObject(name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);return go;}
try
{
    var go=Make("Outer peacock review");go.AddComponent<UnityEngine.MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<UnityEngine.MeshRenderer>();
    var camera=Make("Review camera").AddComponent<UnityEngine.Camera>();camera.enabled=false;camera.scene=scene;
    camera.orthographic=true;camera.orthographicSize=.19f;camera.aspect=.7f;camera.nearClipPlane=.01f;camera.farClipPlane=3;
    camera.transform.position=mesh.bounds.center+new UnityEngine.Vector3(0,0,1);camera.transform.rotation=UnityEngine.Quaternion.Euler(0,180,0);
    camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.06f,.045f,.08f);camera.targetTexture=rt;
    camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
    var light=Make("Review light").AddComponent<UnityEngine.Light>();light.type=UnityEngine.LightType.Directional;light.intensity=1.2f;light.transform.rotation=UnityEngine.Quaternion.Euler(15,160,0);
    for(int tone=1;tone<=2;tone++)
    {
        renderer.sharedMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/HK_Signature/Deepavali/PetalPrototype/Materials/Peacock Outer Tone "+tone+".mat");
        camera.Render();UnityEngine.RenderTexture.active=rt;image.ReadPixels(new UnityEngine.Rect(0,0,700,1000),0,0);image.Apply();
        System.IO.File.WriteAllBytes("output/flower-outer-peacock/outer-tone-"+tone+".png",image.EncodeToPNG());
    }
    return new{rendered=2,bounds=mesh.bounds};
}
finally
{
    UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Object.DestroyImmediate(mesh);
}

