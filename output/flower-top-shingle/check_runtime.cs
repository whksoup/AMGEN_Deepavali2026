// A short live check of visible picking, independent tip motion and fixed attachments.
if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var tops = UnityEngine.Object.FindObjectsByType<Amgen.Deepavali.PetalMotionController>(UnityEngine.FindObjectsSortMode.None).OrderBy(p => p.name).ToArray();
var input = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalLayerPointerInput>();
var skins = tops.Select(p => p.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>()).ToArray();
var bases = skins.Select(s => s.bones.Single(b => b.name.EndsWith("_Base"))).ToArray();
var roots = bases.Select(b => b.position).ToArray();
var tipIndices = skins.Select(s => System.Array.IndexOf(s.sharedMesh.vertices, s.sharedMesh.vertices.OrderByDescending(v => v.y).First())).ToArray();
var mesh = new UnityEngine.Mesh();
var minimumTip = Enumerable.Repeat(float.PositiveInfinity, tops.Length).ToArray();
var maximumTip = Enumerable.Repeat(float.NegativeInfinity, tops.Length).ToArray();
var peaks = new float[tops.Length];
var picked = new bool[tops.Length];
var errors = new System.Collections.Generic.List<string>();
int selection = -1;
System.Action<Amgen.Deepavali.PetalPickTarget> onPick = p => { selection = System.Array.IndexOf(tops, p.Motion); };
UnityEngine.Application.LogCallback onLog = (m, trace, kind) => { if (kind == UnityEngine.LogType.Error || kind == UnityEngine.LogType.Exception) errors.Add(m); };
input.PetalPressed += onPick;
UnityEngine.Application.logMessageReceived += onLog;
bool savedBackground = UnityEngine.Application.runInBackground;
UnityEngine.Application.runInBackground = true;
double start = UnityEngine.Time.timeAsDouble;
int next = 0, frames = 0, lastFrame = -1;
float maxRootDrift = 0, maxBaseLift = 0, maxTipLift = 0, independentTipSpread = 0;
UnityEditor.SessionState.SetString("TopShingle.Runtime", "running");
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    bool done = false;
    try
    {
        if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play Mode ended during check.");
        if (lastFrame == UnityEngine.Time.frameCount) return;
        lastFrame = UnityEngine.Time.frameCount; frames++;
        double elapsed = UnityEngine.Time.timeAsDouble - start;
        if (next < tops.Length && elapsed >= 0.5 + next * 1.1)
        {
            // Sample a visible point in the petal's upper body, away from the center disc.
            var s = skins[next]; s.BakeMesh(mesh);
            int index = System.Array.IndexOf(s.sharedMesh.vertices, s.sharedMesh.vertices.OrderBy(v => UnityEngine.Vector3.SqrMagnitude(v - new UnityEngine.Vector3(0, .21f, 0))).First());
            var screen = UnityEngine.Camera.main.WorldToScreenPoint(s.transform.TransformPoint(mesh.vertices[index]));
            selection = -1;
            picked[next] = input.TryPulseAtScreenPoint(new UnityEngine.Vector2(screen.x, screen.y)) && selection == next;
            next++;
        }
        for (int i = 0; i < tops.Length; i++)
        {
            maxRootDrift = UnityEngine.Mathf.Max(maxRootDrift, UnityEngine.Vector3.Distance(roots[i], bases[i].position));
            peaks[i] = UnityEngine.Mathf.Max(peaks[i], tops[i].PulseWeight);
            maxBaseLift = UnityEngine.Mathf.Max(maxBaseLift, tops[i].CurrentLiftDegrees.x);
            maxTipLift = UnityEngine.Mathf.Max(maxTipLift, tops[i].CurrentLiftDegrees.z);
            skins[i].BakeMesh(mesh);
            float height = skins[i].transform.TransformPoint(mesh.vertices[tipIndices[i]]).y;
            minimumTip[i] = UnityEngine.Mathf.Min(minimumTip[i], height);
            maximumTip[i] = UnityEngine.Mathf.Max(maximumTip[i], height);
        }
        independentTipSpread = UnityEngine.Mathf.Max(independentTipSpread, tops.Max(p => p.CurrentLiftDegrees.z) - tops.Min(p => p.CurrentLiftDegrees.z));
        if (elapsed < 11) return;
        var travel = maximumTip.Zip(minimumTip, (a, b) => 1000 * (a - b)).ToArray();
        bool passed = frames > 30 && picked.All(p => p) && peaks.All(p => p > .2f) && travel.All(d => d > 1f)
            && maxRootDrift < 1e-6f && maxBaseLift < .701f && maxTipLift > 5f && independentTipSpread > 2f && errors.Count == 0;
        var report = new { passed, frames, picked, pulsePeaks = peaks, tipTravelMm = travel, maxRootDrift, maxBaseLift, maxTipLift, independentTipSpread, errors };
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
        System.IO.File.WriteAllText(System.IO.Path.GetFullPath("output/flower-top-shingle/runtime-validation.json"), json);
        UnityEditor.SessionState.SetString("TopShingle.Runtime", json);
        done = true;
    }
    catch (System.Exception ex) { UnityEditor.SessionState.SetString("TopShingle.Runtime", "FAILED: " + ex); done = true; }
    finally
    {
        if (done)
        {
            UnityEditor.EditorApplication.update -= tick;
            input.PetalPressed -= onPick;
            UnityEngine.Application.logMessageReceived -= onLog;
            UnityEngine.Application.runInBackground = savedBackground;
            UnityEngine.Object.Destroy(mesh);
        }
    }
};
UnityEditor.EditorApplication.update += tick;
return "Started 11-second picking and motion check.";
