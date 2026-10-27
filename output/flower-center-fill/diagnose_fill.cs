if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode required.");
var c=UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var r=(UnityEngine.Renderer)new UnityEditor.SerializedObject(c).FindProperty("centerDisc").objectReferenceValue;
var mat=r.sharedMaterial;var camera=UnityEngine.Camera.main;
var extra=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
var oldPos=camera.transform.position;var oldOrtho=camera.orthographic;var oldSize=camera.orthographicSize;var oldPP=extra.renderPostProcessing;
var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;
var oldState=c.SelectedState;var oldPalettes=Enumerable.Range(0,c.PetalCount).Select(i=>c.GetRequestedPetalState(i)).ToArray();
var oldReflection=UnityEngine.RenderSettings.reflectionIntensity;var oldSmoothness=mat.GetFloat("_Smoothness");
var rt=UnityEngine.RenderTexture.GetTemporary(256,256,24,UnityEngine.RenderTextureFormat.ARGBHalf,UnityEngine.RenderTextureReadWrite.Linear);
var tex=new UnityEngine.Texture2D(256,256,UnityEngine.TextureFormat.RGBAFloat,false,true);
var unlit=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Unlit"));
float[] Sample()
{
    camera.Render();UnityEngine.RenderTexture.active=rt;tex.ReadPixels(new UnityEngine.Rect(0,0,256,256),0,0);tex.Apply();
    var color=tex.GetPixel(128,196);return new[]{color.r,color.g,color.b};
}
try
{
    camera.targetTexture=rt;camera.orthographic=true;camera.orthographicSize=0.145f;camera.transform.position=r.bounds.center+UnityEngine.Vector3.up;extra.renderPostProcessing=false;
    c.SetState(Amgen.Deepavali.FlowerPaletteState.Gold);c.PaletteSet.TryGet(Amgen.Deepavali.FlowerPaletteState.Gold,out var palette);
    var original=Sample();mat.SetFloat("_Smoothness",0);var rough=Sample();
    UnityEngine.RenderSettings.reflectionIntensity=0;var noReflection=Sample();
    r.sharedMaterial=unlit;unlit.SetColor("_BaseColor",palette.center.linear);var unlitLinear=Sample();
    unlit.SetColor("_BaseColor",palette.center);var unlitSrgb=Sample();
    return new{expectedLinear=new[]{palette.center.linear.r,palette.center.linear.g,palette.center.linear.b},original,rough,noReflection,unlitLinear,unlitSrgb};
}
finally
{
    r.sharedMaterial=mat;mat.SetFloat("_Smoothness",oldSmoothness);UnityEngine.RenderSettings.reflectionIntensity=oldReflection;
    c.SetState(oldState);for(int i=0;i<c.PetalCount;i++)c.SetPetalState(i,oldPalettes[i]);
    camera.targetTexture=oldTarget;camera.orthographic=oldOrtho;camera.orthographicSize=oldSize;camera.transform.position=oldPos;extra.renderPostProcessing=oldPP;
    UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(tex);UnityEngine.Object.Destroy(unlit);
}
