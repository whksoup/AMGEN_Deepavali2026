// Render the new assets on copies of the real petal/disc meshes in a disposable preview scene.
// The user's open scene, source materials, palettes, and controller are never changed.
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Run symbol review in Edit Mode.");
var flower = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var data = new UnityEditor.SerializedObject(flower);
var sourcePetal = (UnityEngine.SkinnedMeshRenderer)data.FindProperty("petals").GetArrayElementAtIndex(0).objectReferenceValue;
var sourceCenter = (UnityEngine.Renderer)data.FindProperty("centerDisc").objectReferenceValue;
const string root = "Assets/HK_Signature/Deepavali/PetalPrototype/Symbols/Initial State";
var petalSet = UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerSymbolSet>(root + "/Initial Petal Symbols.asset");
var centerSet = UnityEditor.AssetDatabase.LoadAssetAtPath<Amgen.Deepavali.FlowerSymbolSet>(root + "/Initial Center Mandala.asset");
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var mesh = new UnityEngine.Mesh();
var material = new UnityEngine.Material(sourcePetal.sharedMaterial);
var rt = UnityEngine.RenderTexture.GetTemporary(512, 640, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
var image = new UnityEngine.Texture2D(512, 640, UnityEngine.TextureFormat.RGB24, false);
var oldActive = UnityEngine.RenderTexture.active;
UnityEngine.GameObject Make(string name)
{
    var go = new UnityEngine.GameObject(name);
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
    return go;
}
try
{
    sourcePetal.BakeMesh(mesh);
    mesh.RecalculateBounds();
    var go = Make("Symbol review mesh");
    go.transform.rotation = sourcePetal.transform.rotation;
    var filter = go.AddComponent<UnityEngine.MeshFilter>(); filter.sharedMesh = mesh;
    var renderer = go.AddComponent<UnityEngine.MeshRenderer>(); renderer.sharedMaterial = material;
    var camera = Make("Symbol review camera").AddComponent<UnityEngine.Camera>();
    camera.enabled = false; camera.scene = scene;
    camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
    camera.backgroundColor = new UnityEngine.Color(0.055f, 0.045f, 0.075f);
    camera.orthographic = true; camera.orthographicSize = 0.151f;
    camera.nearClipPlane = 0.01f; camera.farClipPlane = 3;
    camera.transform.rotation = UnityEngine.Quaternion.Euler(90, 180, 0);
    camera.transform.position = renderer.bounds.center + UnityEngine.Vector3.up;
    camera.targetTexture = rt;
    var extra = camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
    extra.renderPostProcessing = false;
    var light = Make("Review key light").AddComponent<UnityEngine.Light>();
    light.type = UnityEngine.LightType.Directional; light.intensity = 1.2f;
    light.transform.rotation = UnityEngine.Quaternion.Euler(65, 25, 0);
    material.SetColor("_BaseColor", new UnityEngine.Color(0.28f, 0.09f, 0.35f).linear);
    material.SetColor("_TargetBaseColor", material.GetColor("_BaseColor"));
    material.SetColor("_PatternColor", new UnityEngine.Color(1f, 0.83f, 0.46f).linear);
    material.SetColor("_TargetPatternColor", material.GetColor("_PatternColor"));
    material.SetColor("_AccentColor", material.GetColor("_PatternColor"));
    material.SetColor("_TargetAccentColor", material.GetColor("_PatternColor"));
    material.SetFloat("_WaveProgress", 1); material.SetFloat("_WaveRadius", 2); material.SetFloat("_WaveSoftness", 0.01f);
    material.SetFloat("_ArtworkProgress", 1); material.SetFloat("_ArtworkRadius", 2); material.SetFloat("_ArtworkSoftness", 0.04f);
    material.SetVector("_ArtworkUvToRoot", new UnityEngine.Vector4(0.182f/0.263f, 1, -0.091f/0.263f, 0));
    material.SetFloat("_EdgeIntensity", 2); material.SetFloat("_TargetEdgeIntensity", 2);
    material.SetFloat("_EmissionScale", 1); material.SetFloat("_Smoothness", 0);
    material.SetFloat("_MainPatternIntensity", 0.5f); material.SetFloat("_TargetMainPatternIntensity", 0.5f);
    material.SetFloat("_SymbolEnabled", 1); material.SetFloat("_TargetSymbolEnabled", 1);
    var counts = new System.Collections.Generic.List<int>();
    for (int i = 0; i < 9; i++)
    {
        var set = i < 8 ? petalSet : centerSet;
        set.TryGet(i < 8 ? i : 0, out var symbol);
        var rect = symbol.uvRect;
        material.SetTexture("_SymbolAtlas", set.Atlas);
        material.SetFloat("_SymbolMaxMip", set.MaxMipLevel);
        material.SetVector("_SymbolRect", new UnityEngine.Vector4(rect.width, rect.height, rect.x, rect.y));
        material.SetVector("_TargetSymbolRect", material.GetVector("_SymbolRect"));
        if (i == 8)
        {
            filter.sharedMesh = sourceCenter.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
            go.transform.rotation = sourceCenter.transform.rotation;
            go.transform.localScale = sourceCenter.transform.lossyScale;
            material.SetFloat("_CenterSurface", 1);
            material.SetFloat("_CenterIconScale", 0.8f);
            material.SetFloat("_CenterRingRadius", 0.92f); material.SetFloat("_CenterRingWidth", 0.012f);
            material.SetFloat("_CenterRingGlow", 0.5f); material.SetFloat("_CenterIconGlow", 0.5f); material.SetFloat("_CenterDotGlow", 0.25f);
            material.SetColor("_CenterRingColor", material.GetColor("_PatternColor"));
            material.SetColor("_TargetCenterRingColor", material.GetColor("_PatternColor"));
            camera.transform.position = renderer.bounds.center + UnityEngine.Vector3.up;
            camera.orthographicSize = 0.16f;
        }
        camera.Render(); UnityEngine.RenderTexture.active = rt;
        image.ReadPixels(new UnityEngine.Rect(0, 0, 512, 640), 0, 0); image.Apply();
        System.IO.File.WriteAllBytes($"output/flower-symbols/unity-{i:00}.png", image.EncodeToPNG());
        int goldPixels = image.GetPixels32().Count(p => p.r > 120 && p.g > 75 && p.r > p.b * 1.2f);
        if (goldPixels < 100) throw new System.Exception("Missing visible symbol at index " + i);
        counts.Add(goldPixels);
    }
    var shaderMessages = UnityEditor.ShaderUtil.GetShaderMessages(material.shader).Select(m => new { message = m.message, severity = m.severity.ToString() }).ToArray();
    var result = new { rendered = 9, nonemptyGoldPixels = counts.ToArray(), shaderMessages, sceneDirty = flower.gameObject.scene.isDirty };
    System.IO.File.WriteAllText("output/flower-symbols/render-validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
    return result;
}
finally
{
    UnityEngine.RenderTexture.active = oldActive;
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
    UnityEngine.RenderTexture.ReleaseTemporary(rt);
    UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(mesh);
}
