if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var flower = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var data = new UnityEditor.SerializedObject(flower);
var petals = data.FindProperty("petals");
var surfaces = Enumerable.Range(0, flower.PetalCount).Select(i => (UnityEngine.Renderer)petals.GetArrayElementAtIndex(i).objectReferenceValue)
    .Concat(new[] { (UnityEngine.Renderer)data.FindProperty("centerDisc").objectReferenceValue }).ToArray();
var label = "after";
var camera = UnityEngine.Camera.main;
var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
var oldOrtho = camera.orthographic; var oldSize = camera.orthographicSize; var oldAspect = camera.aspect;
var oldTarget = camera.targetTexture; var oldActive = UnityEngine.RenderTexture.active;
var motions = flower.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>();
var oldMotion = motions.Select(m => m.enabled).ToArray();
var outer = flower.GetComponentInChildren<Amgen.Deepavali.OuterPetalLayerMotion>();
bool oldOuter = outer != null && outer.enabled;
var rt = UnityEngine.RenderTexture.GetTemporary(1400, 1400, 24, UnityEngine.RenderTextureFormat.ARGBHalf, UnityEngine.RenderTextureReadWrite.Linear);
var readback = new UnityEngine.Texture2D(1400, 1400, UnityEngine.TextureFormat.RGBAFloat, false, true);
var png = new UnityEngine.Texture2D(1400, 1400, UnityEngine.TextureFormat.RGB24, false);
try
{
    flower.ResetToInitialState();
    foreach (var m in motions) m.enabled = false;
    if (outer != null) outer.enabled = false;
    camera.transform.position = new UnityEngine.Vector3(0, 1.5f, 0);
    camera.transform.rotation = UnityEngine.Quaternion.Euler(90, 180, 0);
    camera.orthographic = true; camera.orthographicSize = 0.48f; camera.aspect = 1; camera.targetTexture = rt;
    camera.Render(); UnityEngine.RenderTexture.active = rt;
    readback.ReadPixels(new UnityEngine.Rect(0, 0, 1400, 1400), 0, 0); readback.Apply();
    var pixels = readback.GetPixels();
    for (int i = 0; i < pixels.Length; i++) pixels[i] = pixels[i].gamma;
    png.SetPixels(pixels); png.Apply();
    System.IO.File.WriteAllBytes("output/flower-tonal-refinement/" + label + ".png", png.EncodeToPNG());
    var floatNames = new[] { "_EdgeIntensity", "_TargetEdgeIntensity", "_EmissionScale", "_MainPatternIntensity", "_TargetMainPatternIntensity", "_AccentPatternIntensity", "_TargetAccentPatternIntensity", "_CenterRingGlow", "_CenterIconGlow", "_CenterDotGlow", "_StartupAppearance", "_TargetStartupAppearance", "_WaveProgress", "_ArtworkProgress", "_RimGlowScale", "_OrnamentGlowScale", "_BodyShadeStrength", "_BodyOcclusionStrength", "_BodySpecularStrength" };
    var colorNames = new[] { "_BaseColor", "_TargetBaseColor", "_PatternColor", "_TargetPatternColor", "_AccentColor", "_TargetAccentColor", "_TertiaryColor", "_TargetTertiaryColor", "_EdgeColor", "_TargetEdgeColor", "_CenterRingColor", "_TargetCenterRingColor" };
    var emission = surfaces.Select(r => new {
        name = r.name,
        floats = floatNames.ToDictionary(p => p, p => r.sharedMaterial.GetFloat(p)),
        colors = colorNames.ToDictionary(p => p, p => { var c = r.sharedMaterial.GetColor(p); return new[] { c.r, c.g, c.b, c.a }; })
    }).ToArray();
    System.IO.File.WriteAllText("output/flower-tonal-refinement/" + label + "-emission.json", Newtonsoft.Json.JsonConvert.SerializeObject(emission, Newtonsoft.Json.Formatting.Indented));
    var settings = new {
        label, camera = "1400x1400 / orthographic 0.48 / position 0,1.5,0 / rotation 90,180,0 / rest pose",
        ambient = UnityEngine.RenderSettings.ambientLight.ToString(),
        lights = UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None).OrderBy(l => l.name).Select(l => new { name = l.name, l.intensity, color = l.color.ToString() }).ToArray(),
        volumes = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(UnityEngine.FindObjectsSortMode.None).Select(v => new { name = v.name, v.weight, profile = UnityEditor.AssetDatabase.GetAssetPath(v.sharedProfile), components = v.sharedProfile.components.Select(c => UnityEngine.JsonUtility.ToJson(c)).ToArray() }).ToArray(),
        errors = UnityEditor.ShaderUtil.GetShaderMessages(surfaces[0].sharedMaterial.shader).Select(m => m.message).ToArray()
    };
    var json = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
    System.IO.File.WriteAllText("output/flower-tonal-refinement/" + label + "-settings.json", json);
    return json;
}
finally
{
    camera.transform.position = oldPosition; camera.transform.rotation = oldRotation;
    camera.orthographic = oldOrtho; camera.orthographicSize = oldSize; camera.aspect = oldAspect; camera.targetTexture = oldTarget;
    UnityEngine.RenderTexture.active = oldActive; UnityEngine.RenderTexture.ReleaseTemporary(rt);
    UnityEngine.Object.Destroy(readback); UnityEngine.Object.Destroy(png);
    for (int i = 0; i < motions.Length; i++) motions[i].enabled = oldMotion[i];
    if (outer != null) outer.enabled = oldOuter;
}
