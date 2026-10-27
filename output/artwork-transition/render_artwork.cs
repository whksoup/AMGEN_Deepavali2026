if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var c = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerPaletteController>();
var controls = c.GetComponentsInChildren<Amgen.Deepavali.PetalArtworkController>().OrderBy(x => x.PetalIndex).ToArray();
var camera = UnityEngine.Camera.main;
var motions = c.GetComponentsInChildren<Amgen.Deepavali.PetalMotionController>();
var oldEnabled = motions.Select(m => m.enabled).ToArray();
var pipeline = UnityEngine.QualitySettings.renderPipeline;
var oldTarget = camera.targetTexture; var oldActive = UnityEngine.RenderTexture.active; var oldPosition = camera.transform.position;
var rt = UnityEngine.RenderTexture.GetTemporary(640, 640, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
var tex = new UnityEngine.Texture2D(640, 640, UnityEngine.TextureFormat.RGB24, false);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var waveField = c.GetType().GetField("artworkWaves", flags);
var waveType = waveField.FieldType.GetElementType();
var start = waveType.GetField("startedAt"); var duration = waveType.GetField("duration");
var update = c.GetType().GetMethod("Update", flags);
try
{
    UnityEngine.QualitySettings.renderPipeline = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
    foreach (var m in motions) m.enabled = false;
    var skins = c.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
    var bounds = skins[0].bounds;
    foreach (var skin in skins) bounds.Encapsulate(skin.bounds);
    float halfExtent = UnityEngine.Mathf.Max(bounds.extents.x, bounds.extents.z);
    float height = halfExtent * 1.15f / UnityEngine.Mathf.Tan(camera.fieldOfView * UnityEngine.Mathf.Deg2Rad * 0.5f);
    camera.transform.position = new UnityEngine.Vector3(bounds.center.x, bounds.max.y + height, bounds.center.z); camera.targetTexture = rt;
    c.SetState(Amgen.Deepavali.FlowerPaletteState.Base);
    for (int i = 0; i < c.PetalCount; i++) { c.SetPetalState(i, (Amgen.Deepavali.FlowerPaletteState)(i % 7)); c.SetSymbol(i, -1); }
    System.IO.Directory.CreateDirectory("output/artwork-transition/frames");
    for (int leg = 0; leg < 2; leg++)
    {
        for (int i = 0; i < controls.Length; i++) controls[i].SetArtwork(leg == 0 ? (i + 1) % 4 : -1);
        for (int frame = 0; frame <= 16; frame++)
        {
            var waves = (System.Array)waveField.GetValue(c);
            for (int i = 0; i < controls.Length; i++)
            {
                var wave = waves.GetValue(i);
                start.SetValue(wave, UnityEngine.Time.timeAsDouble - (float)duration.GetValue(wave) * frame / 16f);
                waves.SetValue(wave, i);
            }
            update.Invoke(c, null);
            camera.Render(); UnityEngine.RenderTexture.active = rt; tex.ReadPixels(new UnityEngine.Rect(0, 0, 640, 640), 0, 0); tex.Apply();
            System.IO.File.WriteAllBytes($"output/artwork-transition/frames/{leg}-{frame:00}.png", tex.EncodeToPNG());
        }
    }
    return new { frames = 34, direction = "Root to tip for replacement and restoration", controls = controls.Length };
}
finally
{
    c.SetState(Amgen.Deepavali.FlowerPaletteState.Base); for (int i = 0; i < c.PetalCount; i++) c.SetSymbol(i, -1);
    for (int i = 0; i < motions.Length; i++) motions[i].enabled = oldEnabled[i];
    camera.targetTexture = oldTarget; camera.transform.position = oldPosition; UnityEngine.RenderTexture.active = oldActive;
    UnityEngine.RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.Destroy(tex); UnityEngine.QualitySettings.renderPipeline = pipeline;
}
