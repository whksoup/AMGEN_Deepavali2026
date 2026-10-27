if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var c = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var data = new UnityEditor.SerializedObject(c);
var center = (UnityEngine.Renderer)data.FindProperty("centerDisc").objectReferenceValue;
var camera = UnityEngine.Camera.main;
var oldTarget = camera.targetTexture; var oldActive = UnityEngine.RenderTexture.active;
var oldPosition = camera.transform.position; var oldOrtho = camera.orthographic; var oldSize = camera.orthographicSize;
var oldMask = camera.cullingMask; var oldLayer = center.gameObject.layer;
var oldPipeline = UnityEngine.QualitySettings.renderPipeline;
var oldState = c.SelectedState;
var oldPalettes = Enumerable.Range(0,c.PetalCount).Select(i=>c.GetRequestedPetalState(i)).ToArray();
var motions = c.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>(); var oldEnabled = motions.Select(m=>m.enabled).ToArray();
var rt = UnityEngine.RenderTexture.GetTemporary(600,600,24,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.sRGB);
var tex = new UnityEngine.Texture2D(600,600,UnityEngine.TextureFormat.RGB24,false);
var sheet = new UnityEngine.Texture2D(1800,1200,UnityEngine.TextureFormat.RGB24,false);
void Capture(string file)
{
    camera.Render(); UnityEngine.RenderTexture.active = rt;
    tex.ReadPixels(new UnityEngine.Rect(0,0,600,600),0,0); tex.Apply();
    System.IO.File.WriteAllBytes("output/flower-center/"+file,tex.EncodeToPNG());
}
try
{
    UnityEngine.QualitySettings.renderPipeline = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
    foreach (var motion in motions) motion.enabled = false;
    camera.targetTexture = rt;
    for (int i=0;i<7;i++)
    {
        var state=(Amgen.Deepavali.FlowerPaletteState)i; c.SetState(state);
        camera.orthographic = oldOrtho; camera.orthographicSize = oldSize; camera.transform.position = oldPosition;
        camera.cullingMask = oldMask; center.gameObject.layer = oldLayer;
        Capture($"flower-{state}.png");
        camera.orthographic = true; camera.orthographicSize = 0.145f;
        camera.transform.position = center.bounds.center + UnityEngine.Vector3.up;
        center.gameObject.layer=31; camera.cullingMask=1<<31;
        Capture($"center-{state}.png");
        if(i>0)sheet.SetPixels32(((i-1)%3)*600,(1-(i-1)/3)*600,600,600,tex.GetPixels32());
    }
    sheet.Apply();System.IO.File.WriteAllBytes("output/flower-center/six-center-states.png",sheet.EncodeToPNG());
    var shader=center.sharedMaterial.shader;
    return new { rendered=14, shaderErrors=UnityEditor.ShaderUtil.GetShaderMessages(shader).Select(m=>m.message).ToArray(), centerMaterial=center.sharedMaterial.name };
}
finally
{
    center.gameObject.layer=oldLayer; camera.cullingMask=oldMask;
    c.SetState(oldState); for(int i=0;i<c.PetalCount;i++)c.SetPetalState(i,oldPalettes[i]);
    for(int i=0;i<motions.Length;i++)motions[i].enabled=oldEnabled[i];
    camera.targetTexture=oldTarget; camera.transform.position=oldPosition; camera.orthographic=oldOrtho; camera.orthographicSize=oldSize;
    UnityEngine.RenderTexture.active=oldActive; UnityEngine.RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.Destroy(tex);
    UnityEngine.Object.Destroy(sheet);
    UnityEngine.QualitySettings.renderPipeline=oldPipeline;
}
