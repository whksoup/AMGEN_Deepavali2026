// Play Mode integration check. Temporary devices, baked meshes and callbacks are cleaned up.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Enter Play Mode first.");
var targets = UnityEngine.Object.FindObjectsByType<Amgen.Deepavali.PetalPickTarget>(UnityEngine.FindObjectsSortMode.None).OrderBy(t => t.name).ToArray();
if (targets.Length != 8) throw new System.InvalidOperationException("Expected eight petals.");
var motions = targets.Select(t => t.Motion).ToArray();
var skins = targets.Select(t => t.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>()).ToArray();
var input = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalLayerPointerInput>();
var camera = UnityEngine.Camera.main;
var disc = UnityEngine.GameObject.Find("Center Disc").transform;
var discPosition = disc.position;
var discRotation = disc.rotation;
var bones = skins.Select(s => s.bones).ToArray();
var positions = bones.Select(bs => bs.Select(b => b.localPosition).ToArray()).ToArray();
var scales = bones.Select(bs => bs.Select(b => b.localScale).ToArray()).ToArray();
var roots = bones.Select(bs => bs[0].position).ToArray();
var meshes = skins.Select(s => new UnityEngine.Mesh()).ToArray();
var rootIndices = new int[8];
for (int p = 0; p < 8; p++)
{
    skins[p].BakeMesh(meshes[p]);
    rootIndices[p] = Enumerable.Range(0, meshes[p].vertexCount).OrderBy(i => UnityEngine.Vector3.Distance(skins[p].transform.TransformPoint(meshes[p].vertices[i]), roots[p])).First();
}
UnityEngine.Vector2 BodyPoint(int p)
{
    skins[p].BakeMesh(meshes[p]);
    var vertices = meshes[p].vertices;
    var triangles = meshes[p].triangles;
    var desired = motions[p].transform.TransformPoint(new UnityEngine.Vector3(0f, 0.02f, -0.24f));
    float nearest = float.PositiveInfinity;
    var point = UnityEngine.Vector3.zero;
    for (int i = 0; i < triangles.Length; i += 3)
    {
        var center = skins[p].transform.TransformPoint((vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3f);
        float distance = (center - desired).sqrMagnitude;
        if (distance < nearest) { nearest = distance; point = center; }
    }
    return camera.WorldToScreenPoint(point);
}
void ResetPulses() { foreach (var motion in motions) { motion.enabled = false; motion.enabled = true; } }
ResetPulses();
var originalMouse = UnityEngine.InputSystem.Mouse.current;
var originalTouch = UnityEngine.InputSystem.Touchscreen.current;
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("InnerLayerValidationMouse");
var touch = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>("InnerLayerValidationTouch");
mouse.MakeCurrent(); touch.MakeCurrent();
var originalPipeline = UnityEngine.QualitySettings.renderPipeline;
var mobilePipeline = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
UnityEngine.QualitySettings.renderPipeline = mobilePipeline;
var errors = new System.Collections.Generic.List<string>();
UnityEngine.Application.LogCallback logger = (message, trace, kind) =>
{
    if (kind == UnityEngine.LogType.Error || kind == UnityEngine.LogType.Exception || kind == UnityEngine.LogType.Assert) errors.Add(message);
};
UnityEngine.Application.logMessageReceived += logger;
int stage = -1, lastFrame = -1, samples = 0, outOfBounds = 0, ignoredPoints = 0;
bool entered = false, released = false, deformedCheck = false;
double stageStart = UnityEngine.Time.realtimeSinceStartupAsDouble;
var mousePeaks = new float[8];
var touchPeaks = new float[8];
float maxOtherPulse = 0, maxRootDrift = 0, maxBoneTranslation = 0, maxBoneScale = 0, maxDiscDrift = 0, maxDiscRotation = 0, idlePhaseDifference = 0;
float deformedRayError = 0;
bool deformedPicked = false;
var touchPositions = new UnityEngine.Vector2[2];
UnityEditor.SessionState.SetString("InnerLayer.Validation", "running");
UnityEditor.EditorApplication.CallbackFunction update = null;
update = () =>
{
    bool finished = false;
    try
    {
        if (!UnityEditor.EditorApplication.isPlaying || input == null) throw new System.InvalidOperationException("Play Mode stopped during validation.");
        if (lastFrame == UnityEngine.Time.frameCount) return;
        lastFrame = UnityEngine.Time.frameCount;
        samples++;
        double elapsed = UnityEngine.Time.realtimeSinceStartupAsDouble - stageStart;
        if (!entered)
        {
            entered = true;
            released = false;
            if (stage >= 0 && stage < 8)
            {
                ResetPulses();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { buttons = 1, position = BodyPoint(stage) });
            }
            if (stage == 8)
            {
                ResetPulses();
                touchPositions[0] = BodyPoint(1); touchPositions[1] = BodyPoint(5);
                for (int i = 0; i < 2; i++) UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,
                    new UnityEngine.InputSystem.LowLevel.TouchState { touchId = i + 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = touchPositions[i] });
            }
            if (stage == 9)
            {
                ResetPulses();
                var points = new System.Collections.Generic.List<UnityEngine.Vector2> { new UnityEngine.Vector2(-1, -1), camera.WorldToScreenPoint(new UnityEngine.Vector3(0f, 0.018f, 0f)), camera.WorldToScreenPoint(new UnityEngine.Vector3(0.43f, 0f, 0f)) };
                for (int i = 0; i < 8; i++)
                {
                    points.Add(camera.WorldToScreenPoint(UnityEngine.Quaternion.Euler(0f, i * 45f, 0f) * new UnityEngine.Vector3(0f, 0f, -0.1f)));
                    points.Add(camera.WorldToScreenPoint(UnityEngine.Quaternion.Euler(0f, i * 45f + 22.5f, 0f) * new UnityEngine.Vector3(0f, 0f, -0.34f)));
                }
                foreach (var point in points)
                    if (!input.TryPulseAtScreenPoint(point)) ignoredPoints++;
                    else throw new System.InvalidOperationException("Disc/gap/background picked a petal at " + point);
            }
        }
        if (stage >= 0 && stage < 8)
        {
            if (elapsed > 0.06 && !released)
            {
                released = true;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = BodyPoint(stage) });
            }
            mousePeaks[stage] = UnityEngine.Mathf.Max(mousePeaks[stage], motions[stage].PulseWeight);
            for (int p = 0; p < 8; p++) if (p != stage) maxOtherPulse = UnityEngine.Mathf.Max(maxOtherPulse, motions[p].PulseWeight);
            if (stage == 7 && elapsed > 0.2 && !deformedCheck)
            {
                deformedCheck = true;
                var screenPoint = BodyPoint(7);
                var ray = camera.ScreenPointToRay(screenPoint);
                bool hit = targets[7].TryRaycast(ray, out float distance);
                deformedPicked = hit && input.TryPulseAtScreenPoint(screenPoint);
                var hitScreen = camera.WorldToScreenPoint(ray.GetPoint(distance));
                deformedRayError = UnityEngine.Vector2.Distance(screenPoint, (UnityEngine.Vector2)hitScreen);
            }
        }
        if (stage == 8)
        {
            if (elapsed > 0.08 && !released)
            {
                released = true;
                for (int i = 0; i < 2; i++) UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,
                    new UnityEngine.InputSystem.LowLevel.TouchState { touchId = i + 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = touchPositions[i] });
            }
            for (int p = 0; p < 8; p++) touchPeaks[p] = UnityEngine.Mathf.Max(touchPeaks[p], motions[p].PulseWeight);
        }
        for (int p = 0; p < 8; p++)
        {
            skins[p].BakeMesh(meshes[p]);
            var vertices = meshes[p].vertices;
            maxRootDrift = UnityEngine.Mathf.Max(maxRootDrift, UnityEngine.Vector3.Distance(skins[p].transform.TransformPoint(vertices[rootIndices[p]]), roots[p]));
            foreach (var vertex in vertices) if (!skins[p].bounds.Contains(skins[p].transform.TransformPoint(vertex))) outOfBounds++;
            for (int b = 0; b < bones[p].Length; b++)
            {
                maxBoneTranslation = UnityEngine.Mathf.Max(maxBoneTranslation, UnityEngine.Vector3.Distance(positions[p][b], bones[p][b].localPosition));
                maxBoneScale = UnityEngine.Mathf.Max(maxBoneScale, UnityEngine.Vector3.Distance(scales[p][b], bones[p][b].localScale));
            }
            if (stage == -1 && elapsed > 0.5) idlePhaseDifference = UnityEngine.Mathf.Max(idlePhaseDifference, UnityEngine.Quaternion.Angle(bones[0][2].localRotation, bones[p][2].localRotation));
        }
        maxDiscDrift = UnityEngine.Mathf.Max(maxDiscDrift, UnityEngine.Vector3.Distance(discPosition, disc.position));
        maxDiscRotation = UnityEngine.Mathf.Max(maxDiscRotation, UnityEngine.Quaternion.Angle(discRotation, disc.rotation));
        if (elapsed < (stage == -1 ? 1.0 : 0.7)) return;
        stage++;
        stageStart = UnityEngine.Time.realtimeSinceStartupAsDouble;
        entered = false;
        if (stage <= 9) return;
        bool pass = mousePeaks.All(p => p > 0.2f) && maxOtherPulse == 0f && touchPeaks[1] > 0.2f && touchPeaks[5] > 0.2f
            && touchPeaks.Where((p, i) => i != 1 && i != 5).All(p => p == 0f) && ignoredPoints == 19 && deformedPicked && deformedRayError < 0.01f
            && maxRootDrift < 0.000001f && maxBoneTranslation == 0f && maxBoneScale == 0f && maxDiscDrift == 0f && maxDiscRotation < 0.001f
            && outOfBounds == 0 && idlePhaseDifference > 0.5f && errors.Count == 0 && mobilePipeline != null;
        var values = new System.Collections.Generic.Dictionary<string, float> {
            {"samples", samples}, {"maxOtherPetalPulseWeight", maxOtherPulse}, {"ignoredDiscGapBackgroundPoints",ignoredPoints},
            {"deformedRayErrorPixels",deformedRayError}, {"maxRootDriftMetres",maxRootDrift}, {"maxBoneTranslationMetres",maxBoneTranslation},
            {"maxBoneScaleChange",maxBoneScale}, {"maxDiscDriftMetres",maxDiscDrift}, {"maxDiscRotationDegrees",maxDiscRotation},
            {"outOfBoundsVertices",outOfBounds}, {"idlePhaseDifferenceDegrees",idlePhaseDifference}, {"runtimeErrors",errors.Count}
        };
        string Number(float n) => n.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        string report = "{\"passed\":" + (pass ? "true" : "false") + ",\"mobilePipelineChecked\":true,\"deformedPetalPicked\":" + (deformedPicked ? "true" : "false") +
            ",\"mousePulsePeaks\":[" + string.Join(",", mousePeaks.Select(Number)) + "],\"simultaneousTouchPeaks\":[" + string.Join(",", touchPeaks.Select(Number)) + "]," +
            string.Join(",", values.Select(pair => "\"" + pair.Key + "\":" + Number(pair.Value))) + "}";
        UnityEditor.SessionState.SetString("InnerLayer.Validation", report);
        System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName, "output/flower-inner-layer/unity-validation.json"), report);
        finished = true;
    }
    catch (System.Exception exception) { UnityEditor.SessionState.SetString("InnerLayer.Validation", "FAILED: " + exception); finished = true; }
    finally
    {
        if (finished)
        {
            UnityEditor.EditorApplication.update -= update;
            UnityEngine.Application.logMessageReceived -= logger;
            UnityEngine.QualitySettings.renderPipeline = originalPipeline;
            UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
            UnityEngine.InputSystem.InputSystem.RemoveDevice(touch);
            if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
            if (originalTouch != null && originalTouch.added) originalTouch.MakeCurrent();
            foreach (var mesh in meshes) UnityEngine.Object.Destroy(mesh);
        }
    }
};
UnityEditor.EditorApplication.update += update;
return "Validation running: eight separate mouse picks, simultaneous touches, idle offsets, deformed picking, disc/gap misses, pinned roots and mobile pipeline.";
