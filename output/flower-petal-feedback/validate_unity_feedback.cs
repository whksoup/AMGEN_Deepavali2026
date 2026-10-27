// Integration checks in the live Editor, outside shipped Assets.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Enter Play Mode first.");
var driver = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.FlowerEmissionController>();
var input = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalLayerPointerInput>();
var targets = UnityEngine.Object.FindObjectsByType<Amgen.Deepavali.PetalPickTarget>(UnityEngine.FindObjectsSortMode.None).OrderBy(p => p.name).ToArray();
var motions = targets.Select(t => t.Motion).ToArray();
var skins = targets.Select(t => t.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>()).ToArray();
var camera = UnityEngine.Camera.main;
var volume = UnityEngine.GameObject.Find("Petal Bloom - Mobile").GetComponent<UnityEngine.Rendering.Volume>();
volume.profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var runtimeBloom);
float originalBloomIntensity = runtimeBloom.intensity.value;
var output = System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName, "output/flower-petal-feedback");
System.IO.Directory.CreateDirectory(output);
var baselineColor = driver.EdgeColor;
float baselineIntensity = driver.EdgeIntensity;
var assetMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/HK_Signature/Deepavali/PetalPrototype/Materials/Petal Deepavali Lit.mat");
var baseMap = assetMaterial.GetTexture("_BaseMap");
var disc = UnityEngine.GameObject.Find("Center Disc").transform;
var discPosition = disc.position;
var rootPositions = skins.Select(s => s.bones[0].position).ToArray();
var mesh = new UnityEngine.Mesh();
UnityEngine.Vector2 BodyPoint(int p)
{
    skins[p].BakeMesh(mesh);
    var vertices = mesh.vertices;
    var triangles = mesh.triangles;
    var desired = motions[p].transform.TransformPoint(new UnityEngine.Vector3(0f, 0.02f, -0.24f));
    float nearest = float.PositiveInfinity;
    var point = UnityEngine.Vector3.zero;
    for (int i = 0; i < triangles.Length; i += 3)
    {
        var center = skins[p].transform.TransformPoint((vertices[triangles[i]] + vertices[triangles[i+1]] + vertices[triangles[i+2]]) / 3f);
        float d = (center - desired).sqrMagnitude;
        if (d < nearest) { nearest = d; point = center; }
    }
    return camera.WorldToScreenPoint(point);
}
UnityEngine.Color32[] Capture(string name)
{
    var target = UnityEngine.RenderTexture.GetTemporary(1100, 850, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
    var priorTarget = camera.targetTexture;
    var priorActive = UnityEngine.RenderTexture.active;
    var texture = new UnityEngine.Texture2D(1100, 850, UnityEngine.TextureFormat.RGB24, false);
    try
    {
        camera.targetTexture = target;
        camera.Render();
        UnityEngine.RenderTexture.active = target;
        texture.ReadPixels(new UnityEngine.Rect(0, 0, 1100, 850), 0, 0);
        texture.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(output, name), texture.EncodeToPNG());
        return texture.GetPixels32();
    }
    finally
    {
        camera.targetTexture = priorTarget;
        UnityEngine.RenderTexture.active = priorActive;
        UnityEngine.RenderTexture.ReleaseTemporary(target);
        UnityEngine.Object.Destroy(texture);
    }
}
float ColorError(UnityEngine.Color a, UnityEngine.Color b) => UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(a.r-b.r), UnityEngine.Mathf.Abs(a.g-b.g), UnityEngine.Mathf.Abs(a.b-b.b));
void ResetMotions() { foreach (var motion in motions) { motion.enabled = false; motion.enabled = true; } }
ResetMotions();
driver.enabled = false; driver.enabled = true;
var originalPipeline = UnityEngine.QualitySettings.renderPipeline;
UnityEngine.QualitySettings.renderPipeline = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
var originalMouse = UnityEngine.InputSystem.Mouse.current;
var originalTouch = UnityEngine.InputSystem.Touchscreen.current;
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("FeedbackValidationMouse");
var touch = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>("FeedbackValidationTouch");
mouse.MakeCurrent(); touch.MakeCurrent();
var errors = new System.Collections.Generic.List<string>();
UnityEngine.Application.LogCallback logger = (message, trace, kind) =>
{
    if (kind == UnityEngine.LogType.Error || kind == UnityEngine.LogType.Exception || kind == UnityEngine.LogType.Assert) errors.Add(message);
};
UnityEngine.Application.logMessageReceived += logger;
int stage = 0, lastFrame = -1, samples = 0, haloPixels = 0;
double stageStart = UnityEngine.Time.realtimeSinceStartupAsDouble;
bool entered = false, released = false, retriggered = false, captured = false, baselineChanged = false;
float mouseFlashPeak = 0, touchFlashPeak = 0, mouseMotionPeak = 0, otherMotionPeak = 0, retriggerSnap = 0;
float maxUniformError = 0, maxWeight = 0, minWeight = 1, maxRootDrift = 0;
float mouseSettledError = 0, retriggerSettledError = 0, changedBaselineError = 0;
bool oneSharedMaterial = true, artworkUnchanged = true, missesIgnored = false, disableRestored = false, reenabledReset = false;
var touchMotionPeaks = new float[8];
var touchPoints = new UnityEngine.Vector2[2];
UnityEditor.SessionState.SetString("PetalFeedback.Validation", "running");
UnityEditor.EditorApplication.CallbackFunction update = null;
update = () =>
{
    bool finished = false;
    try
    {
        if (!UnityEditor.EditorApplication.isPlaying || driver == null) throw new System.InvalidOperationException("Play Mode stopped during validation.");
        if (lastFrame == UnityEngine.Time.frameCount) return;
        lastFrame = UnityEngine.Time.frameCount;
        samples++;
        double elapsed = UnityEngine.Time.realtimeSinceStartupAsDouble - stageStart;
        if (!entered)
        {
            entered = true; released = false;
            if (stage == 1)
            {
                runtimeBloom.intensity.value = 0f;
                var before = Capture("mobile-bloom-off.png");
                runtimeBloom.intensity.value = originalBloomIntensity;
                var after = Capture("mobile-gold-bloom.png");
                for (int i = 0; i < before.Length; i++)
                    if (before[i].b >= before[i].r && after[i].r > before[i].r + 1) haloPixels++;
                missesIgnored = !input.TryPulseAtScreenPoint(camera.WorldToScreenPoint(new UnityEngine.Vector3(0f, 0.018f, 0f)))
                    && !input.TryPulseAtScreenPoint(new UnityEngine.Vector2(-1f, -1f)) && driver.FlashWeight == 0f;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { buttons = 1, position = BodyPoint(0) });
                stageStart = UnityEngine.Time.realtimeSinceStartupAsDouble;
                elapsed = 0;
            }
            if (stage == 2)
            {
                ResetMotions();
                touchPoints[0] = BodyPoint(2); touchPoints[1] = BodyPoint(6);
                for (int i = 0; i < 2; i++) UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,
                    new UnityEngine.InputSystem.LowLevel.TouchState { touchId = i + 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = touchPoints[i] });
            }
            if (stage == 3) driver.TriggerFlash();
            if (stage == 4) driver.TriggerFlash();
        }
        if (stage == 1)
        {
            if (elapsed > 0.06 && !released)
            {
                released = true;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = BodyPoint(0) });
            }
            mouseFlashPeak = UnityEngine.Mathf.Max(mouseFlashPeak, driver.FlashWeight);
            mouseMotionPeak = UnityEngine.Mathf.Max(mouseMotionPeak, motions[0].PulseWeight);
            for (int p = 1; p < 8; p++) otherMotionPeak = UnityEngine.Mathf.Max(otherMotionPeak, motions[p].PulseWeight);
            if (driver.FlashWeight > 0.99f && !captured) { captured = true; Capture("mobile-rose-flash.png"); }
        }
        if (stage == 2)
        {
            if (elapsed > 0.08 && !released)
            {
                released = true;
                for (int i = 0; i < 2; i++) UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,
                    new UnityEngine.InputSystem.LowLevel.TouchState { touchId = i + 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = touchPoints[i] });
            }
            touchFlashPeak = UnityEngine.Mathf.Max(touchFlashPeak, driver.FlashWeight);
            for (int p = 0; p < 8; p++) touchMotionPeaks[p] = UnityEngine.Mathf.Max(touchMotionPeaks[p], motions[p].PulseWeight);
            if (elapsed > 0.35 && !retriggered)
            {
                retriggered = true;
                var before = skins[0].sharedMaterial.GetColor("_EmissionColor");
                for (int i = 0; i < 12; i++) driver.TriggerFlash();
                retriggerSnap = ColorError(before, skins[0].sharedMaterial.GetColor("_EmissionColor"));
            }
        }
        if (stage == 3 && elapsed > 0.25 && !baselineChanged)
        {
            baselineChanged = true;
            driver.SetEdgeColor(new UnityEngine.Color(0.25f, 0.8f, 1f));
        }
        var current = skins[0].sharedMaterial.GetColor("_EmissionColor");
        oneSharedMaterial &= skins.All(s => s.sharedMaterial == skins[0].sharedMaterial);
        artworkUnchanged &= skins.All(s => s.sharedMaterial.GetTexture("_BaseMap") == baseMap);
        foreach (var skin in skins) maxUniformError = UnityEngine.Mathf.Max(maxUniformError, ColorError(current, skin.sharedMaterial.GetColor("_EmissionColor")));
        maxWeight = UnityEngine.Mathf.Max(maxWeight, driver.FlashWeight);
        minWeight = UnityEngine.Mathf.Min(minWeight, driver.FlashWeight);
        for (int i = 0; i < skins.Length; i++) maxRootDrift = UnityEngine.Mathf.Max(maxRootDrift, UnityEngine.Vector3.Distance(rootPositions[i], skins[i].bones[0].position));
        double duration = stage == 0 ? 0.4 : stage == 2 ? 1.3 : stage == 4 ? 0.1 : 1.0;
        if (elapsed < duration) return;
        if (stage == 1) mouseSettledError = ColorError(current, baselineColor.linear * baselineIntensity) + driver.FlashWeight;
        if (stage == 2) retriggerSettledError = ColorError(current, baselineColor.linear * baselineIntensity) + driver.FlashWeight;
        if (stage == 3)
        {
            changedBaselineError = ColorError(current, driver.EdgeColor.linear * driver.EdgeIntensity) + driver.FlashWeight;
            driver.SetEdgeColor(baselineColor);
        }
        if (stage++ < 4)
        {
            entered = false;
            stageStart = UnityEngine.Time.realtimeSinceStartupAsDouble;
            return;
        }
        driver.enabled = false;
        disableRestored = skins.All(s => s.sharedMaterial == assetMaterial) && driver.FlashWeight == 0f;
        driver.enabled = true;
        reenabledReset = driver.FlashWeight == 0f && ColorError(skins[0].sharedMaterial.GetColor("_EmissionColor"), baselineColor.linear * baselineIntensity) < 0.00001f;
        var bloom = UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.Bloom>();
        bool mobileBloom = bloom.IsActive() && bloom.downscale.value == UnityEngine.Rendering.Universal.BloomDownscaleMode.Quarter && bloom.maxIterations.value == 3 && !bloom.highQualityFiltering.value
            && camera.allowHDR && (UnityEngine.QualitySettings.renderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).supportsHDR;
        bool pass = mouseFlashPeak > 0.99f && touchFlashPeak > 0.99f && mouseMotionPeak > 0.2f && otherMotionPeak == 0f
            && touchMotionPeaks[2] > 0.2f && touchMotionPeaks[6] > 0.2f && touchMotionPeaks.Where((p,i) => i != 2 && i != 6).All(p => p == 0f)
            && mouseSettledError < 0.00001f && retriggerSettledError < 0.00001f && changedBaselineError < 0.00001f && retriggerSnap == 0f
            && maxUniformError == 0f && oneSharedMaterial && artworkUnchanged && minWeight >= 0f && maxWeight <= 1f && missesIgnored
            && disableRestored && reenabledReset && maxRootDrift < 0.000001f && disc.position == discPosition && captured && haloPixels > 20 && mobileBloom && errors.Count == 0;
        string N(float n) => n.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        var values = new System.Collections.Generic.Dictionary<string, float> {
            {"samples",samples},{"mouseFlashPeak",mouseFlashPeak},{"touchFlashPeak",touchFlashPeak},{"mouseMotionPeak",mouseMotionPeak},
            {"unselectedMotionPeak",otherMotionPeak},{"mouseSettledError",mouseSettledError},{"retriggerSettledError",retriggerSettledError},
            {"changedBaselineReturnError",changedBaselineError},{"retriggerColorSnap",retriggerSnap},{"maxUniformColorError",maxUniformError},
            {"minFlashWeight",minWeight},{"maxFlashWeight",maxWeight},{"maxRootDriftMetres",maxRootDrift},{"backgroundHaloPixels",haloPixels},{"runtimeErrors",errors.Count}
        };
        string report = "{\"passed\":"+pass.ToString().ToLower()+",\"mobileBloomActive\":"+mobileBloom.ToString().ToLower()+",\"oneSharedMaterial\":"+oneSharedMaterial.ToString().ToLower()+
            ",\"artworkUnchanged\":"+artworkUnchanged.ToString().ToLower()+",\"missesIgnored\":"+missesIgnored.ToString().ToLower()+",\"disableRestored\":"+disableRestored.ToString().ToLower()+
            ",\"reenabledReset\":"+reenabledReset.ToString().ToLower()+",\"touchMotionPeaks\":["+string.Join(",",touchMotionPeaks.Select(N))+"],"+
            string.Join(",",values.Select(p=>"\""+p.Key+"\":"+N(p.Value)))+"}";
        System.IO.File.WriteAllText(System.IO.Path.Combine(output,"feedback-validation.json"),report);
        UnityEditor.SessionState.SetString("PetalFeedback.Validation",report);
        finished = true;
    }
    catch (System.Exception exception) { UnityEditor.SessionState.SetString("PetalFeedback.Validation","FAILED: "+exception); finished = true; }
    finally
    {
        if (finished)
        {
            UnityEditor.EditorApplication.update -= update;
            UnityEngine.Application.logMessageReceived -= logger;
            UnityEngine.QualitySettings.renderPipeline = originalPipeline;
            runtimeBloom.intensity.value = originalBloomIntensity;
            driver.SetEdgeColor(baselineColor); driver.SetEdgeIntensity(baselineIntensity);
            driver.enabled = false; driver.enabled = true;
            UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
            UnityEngine.InputSystem.InputSystem.RemoveDevice(touch);
            if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
            if (originalTouch != null && originalTouch.added) originalTouch.MakeCurrent();
            UnityEngine.Object.Destroy(mesh);
        }
    }
};
UnityEditor.EditorApplication.update += update;
return "Testing mobile bloom pixels, real pointer feedback, fade return, retriggers, multi-touch, shared material and lifecycle.";
