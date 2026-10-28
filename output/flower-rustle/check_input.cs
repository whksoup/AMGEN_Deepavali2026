// Exercise InputSystem events through PetalLayerPointerInput.Update, not its pick method.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var tops = UnityEngine.Object.FindObjectsByType<Amgen.Deepavali.PetalMotionController>(UnityEngine.FindObjectsSortMode.None).OrderBy(p => p.name).ToArray();
var input = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalLayerPointerInput>();
var ring = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalRingPulse>();
var outer = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.OuterPetalLayerMotion>();
var skins = tops.Select(p => p.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>()).ToArray();
var bones = skins.SelectMany(s => s.bones).ToArray();
var positions = bones.Select(b => b.localPosition).ToArray();
var oldMouse = UnityEngine.InputSystem.Mouse.current;
var oldTouch = UnityEngine.InputSystem.Touchscreen.current;
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("RustleTestMouse");
var touch = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>("RustleTestTouch");
mouse.MakeCurrent(); touch.MakeCurrent();
bool oldBackground = UnityEngine.Application.runInBackground;
UnityEngine.Application.runInBackground = true;
var events = new System.Collections.Generic.List<int>();
System.Action<Amgen.Deepavali.PetalPickTarget> onPick = p => events.Add(System.Array.IndexOf(tops, p.Motion));
input.PetalPressed += onPick;
var mesh = new UnityEngine.Mesh();
UnityEngine.Vector2 Point(int i)
{
    var s = skins[i]; s.BakeMesh(mesh);
    int v = Enumerable.Range(0, s.sharedMesh.vertexCount).OrderBy(k => (s.sharedMesh.vertices[k] - new UnityEngine.Vector3(0, .21f, 0)).sqrMagnitude).First();
    return UnityEngine.Camera.main.WorldToScreenPoint(s.transform.TransformPoint(mesh.vertices[v]));
}
var errors = new System.Collections.Generic.List<string>();
UnityEngine.Application.LogCallback onLog = (m, t, k) => { if (k == UnityEngine.LogType.Error || k == UnityEngine.LogType.Exception) errors.Add(m); };
UnityEngine.Application.logMessageReceived += onLog;
int stage = -1, lastFrame = -1, frames = 0;
double stageStart = 0;
bool entered = false, released = false, retapped = false;
float maxFollowError = 0, maxOuterError = 0, maxBoneDrift = 0, maxUnrelatedPulse = 0, maxEffective = 0;
var peaks = new float[8];
var tipStart = new float[8];
var tipTravel = new float[8];
var touchPeaks = new float[2];
UnityEditor.SessionState.SetString("Rustle.Input", "running");
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    bool done = false;
    try
    {
        if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode stopped.");
        if (lastFrame == UnityEngine.Time.frameCount) return;
        lastFrame = UnityEngine.Time.frameCount; frames++;
        bool resetThisFrame = false;
        if (!entered)
        {
            resetThisFrame = true;
            entered = true; released = retapped = false; stageStart = UnityEngine.Time.timeAsDouble;
            foreach (var p in tops) { p.enabled = false; p.enabled = true; }
            if (stage >= 0 && stage < 8)
            {
                skins[stage].BakeMesh(mesh);
                int tip = Enumerable.Range(0, skins[stage].sharedMesh.vertexCount).OrderByDescending(k => skins[stage].sharedMesh.vertices[k].y).First();
                tipStart[stage] = skins[stage].transform.TransformPoint(mesh.vertices[tip]).y;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { buttons = 1, position = Point(stage) });
            }
            if (stage == 8)
                for (int t = 0; t < 2; t++) UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,
                    new UnityEngine.InputSystem.LowLevel.TouchState { touchId = t + 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = Point(t * 4) });
        }
        double elapsed = UnityEngine.Time.timeAsDouble - stageStart;
        if (stage >= 0 && stage < 8)
        {
            if (elapsed > .06 && !released)
            {
                released = true;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = Point(stage) });
            }
            // Retrigger one petal before its first pulse has settled.
            if (stage == 7 && elapsed > .22 && !retapped)
            {
                retapped = true; released = false;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { buttons = 1, position = Point(stage) });
            }
            peaks[stage] = UnityEngine.Mathf.Max(peaks[stage], tops[stage].PulseWeight);
            for (int i = 0; i < 8; i++)
                if (i != stage && i != (stage + 1) % 8 && i != (stage + 2) % 8 && i != (stage + 7) % 8)
                    maxUnrelatedPulse = UnityEngine.Mathf.Max(maxUnrelatedPulse, tops[i].EffectivePulseWeight);
            skins[stage].BakeMesh(mesh);
            int tip = Enumerable.Range(0, skins[stage].sharedMesh.vertexCount).OrderByDescending(k => skins[stage].sharedMesh.vertices[k].y).First();
            tipTravel[stage] = UnityEngine.Mathf.Max(tipTravel[stage], 1000 * (skins[stage].transform.TransformPoint(mesh.vertices[tip]).y - tipStart[stage]));
        }
        if (stage == 8)
        {
            if (elapsed > .08 && !released)
            {
                released = true;
                for (int t = 0; t < 2; t++) UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,
                    new UnityEngine.InputSystem.LowLevel.TouchState { touchId = t + 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = Point(t * 4) });
            }
            for (int t = 0; t < 2; t++) touchPeaks[t] = UnityEngine.Mathf.Max(touchPeaks[t], tops[t * 4].PulseWeight);
        }
        for (int i = 0; i < 8; i++)
        {
            float expected = UnityEngine.Mathf.Max(tops[i].PulseWeight,
                UnityEngine.Mathf.Max(tops[(i + 7) % 8].PulseWeight * .55f,
                UnityEngine.Mathf.Max(tops[(i + 6) % 8].PulseWeight * .22f, tops[(i + 1) % 8].PulseWeight * .12f)));
            maxFollowError = UnityEngine.Mathf.Max(maxFollowError, UnityEngine.Mathf.Abs(expected - tops[i].EffectivePulseWeight));
            maxEffective = UnityEngine.Mathf.Max(maxEffective, tops[i].EffectivePulseWeight);
        }
        var minLift = tops.Select(p => p.CurrentLiftDegrees).Aggregate(UnityEngine.Vector3.Min);
        // A test-only reset happens outside Unity's frame loop; observe following only
        // on frames where LateUpdate has had a chance to consume the new upper pose.
        if (!resetThisFrame) maxOuterError = UnityEngine.Mathf.Max(maxOuterError, (outer.CurrentLiftDegrees - minLift * .72f).magnitude);
        for (int i = 0; i < bones.Length; i++) maxBoneDrift = UnityEngine.Mathf.Max(maxBoneDrift, (bones[i].localPosition - positions[i]).magnitude);
        if (elapsed < 1.3) return;
        if (++stage <= 8) { entered = false; return; }
        bool passed = peaks.All(v => v > .6f) && touchPeaks.All(v => v > .6f) && tipTravel.All(v => v > 35f)
            && maxFollowError < .0001f && maxOuterError < .0001f && maxBoneDrift < 1e-6f
            && maxUnrelatedPulse < .0001f && maxEffective <= 1.0001f && events.Count == 11 && errors.Count == 0;
        var report = new { passed, frames, events, mousePeaks = peaks, touchPeaks, tipTravelMm = tipTravel, maxFollowError, maxOuterError, maxBoneDrift, maxUnrelatedPulse, maxEffective, errors };
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
        System.IO.File.WriteAllText(System.IO.Path.GetFullPath("output/flower-rustle/input-validation.json"), json);
        UnityEditor.SessionState.SetString("Rustle.Input", json);
        done = true;
    }
    catch (System.Exception ex) { UnityEditor.SessionState.SetString("Rustle.Input", "FAILED: " + ex); done = true; }
    finally
    {
        if (done)
        {
            UnityEditor.EditorApplication.update -= tick;
            input.PetalPressed -= onPick;
            UnityEngine.Application.logMessageReceived -= onLog;
            UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
            UnityEngine.InputSystem.InputSystem.RemoveDevice(touch);
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            if (oldTouch != null && oldTouch.added) oldTouch.MakeCurrent();
            UnityEngine.Application.runInBackground = oldBackground;
            UnityEngine.Object.Destroy(mesh);
        }
    }
};
UnityEditor.EditorApplication.update += tick;
return "Started mouse, retrigger and simultaneous-touch check through the actual input Update.";
