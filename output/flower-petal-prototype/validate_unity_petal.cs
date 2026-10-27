// Temporary Play Mode integration check, executed through Unity MCP eval_file.
// Samples real skinned geometry; synthetic input devices and callbacks are removed afterwards.
if (!UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Run the petal test scene in Play Mode first.");
var motion = UnityEngine.Object.FindFirstObjectByType<Amgen.Deepavali.PetalMotionController>();
var skin = motion.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();
var bones = skin.bones;
var positions = bones.Select(b => b.localPosition).ToArray();
var scales = bones.Select(b => b.localScale).ToArray();
var basePosition = bones[0].position;
var disc = motion.GetComponentsInChildren<UnityEngine.Transform>().Single(t => t.name == "CenterDisc");
var discPosition = disc.position;
var discRotation = disc.rotation;
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/HK_Signature/Deepavali/PetalPrototype/Models/Deepavali_SinglePetal.fbx");
var sourceTransforms = source.GetComponentsInChildren<UnityEngine.Transform>();
var restRotations = bones.Select(b => sourceTransforms.Single(t => t.name == b.name).localRotation).ToArray();
var mesh = new UnityEngine.Mesh();
skin.BakeMesh(mesh);
int rootIndex = 0, tipIndex = 0;
float nearest = float.MaxValue, farthest = 0f;
var initialVertices = mesh.vertices;
for (int i = 0; i < initialVertices.Length; i++)
{
    float distance = UnityEngine.Vector3.Distance(skin.transform.TransformPoint(initialVertices[i]), basePosition);
    if (distance < nearest) { nearest = distance; rootIndex = i; }
    if (distance > farthest) { farthest = distance; tipIndex = i; }
}
var originalKeyboard = UnityEngine.InputSystem.Keyboard.current;
var originalMouse = UnityEngine.InputSystem.Mouse.current;
var originalTouch = UnityEngine.InputSystem.Touchscreen.current;
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("PetalValidationKeyboard");
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("PetalValidationMouse");
var touch = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>("PetalValidationTouch");
keyboard.MakeCurrent(); mouse.MakeCurrent(); touch.MakeCurrent();
var originalPipeline = UnityEngine.QualitySettings.renderPipeline;
var mobilePipeline = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
var errors = new System.Collections.Generic.List<string>();
UnityEngine.Application.LogCallback logger = (message, trace, kind) =>
{
    if (kind == UnityEngine.LogType.Error || kind == UnityEngine.LogType.Exception || kind == UnityEngine.LogType.Assert)
        errors.Add(message);
};
UnityEngine.Application.logMessageReceived += logger;
double start = UnityEngine.Time.realtimeSinceStartupAsDouble;
int lastFrame = -1, samples = 0, outOfBounds = 0;
float maxRootDrift = 0, maxBoneTranslation = 0, maxBoneScale = 0, maxDiscDrift = 0, maxDiscRotation = 0;
float peak = 0, keyboardPeak = 0, mousePeak = 0, touchPeak = 0, snapOnRetrigger = 0, disableError = 0;
float minIdleTip = float.MaxValue, maxIdleTip = float.MinValue, maxTipHeight = float.MinValue;
float settledWeight = -1;
bool keyDown = false, keyUp = false, mouseDown = false, mouseUp = false, touchDown = false, touchUp = false;
bool switched = false, resetChecked = false;
UnityEditor.SessionState.SetString("PetalPrototype.Validation", "running");
UnityEditor.EditorApplication.CallbackFunction update = null;
update = () =>
{
    bool finished = false;
    try
    {
        if (!UnityEditor.EditorApplication.isPlaying || motion == null)
            throw new System.InvalidOperationException("Play Mode stopped during validation.");
        if (lastFrame == UnityEngine.Time.frameCount) return;
        lastFrame = UnityEngine.Time.frameCount;
        double elapsed = UnityEngine.Time.realtimeSinceStartupAsDouble - start;
        samples++;
        if (elapsed >= 0.8 && !keyDown) { keyDown = true; UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Space)); }
        if (elapsed >= 1.0 && !keyUp) { keyUp = true; UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState()); }
        if (elapsed >= 2.8 && !mouseDown) { mouseDown = true; UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { buttons = 1, position = new UnityEngine.Vector2(320,320) }); }
        if (elapsed >= 3.0 && !mouseUp) { mouseUp = true; UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = new UnityEngine.Vector2(320,320) }); }
        if (elapsed >= 4.8 && !touchDown) { touchDown = true; UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch, new UnityEngine.InputSystem.LowLevel.TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = new UnityEngine.Vector2(320,320) }); }
        if (elapsed >= 5.0 && !touchUp) { touchUp = true; UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch, new UnityEngine.InputSystem.LowLevel.TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = new UnityEngine.Vector2(320,320) }); }
        if (elapsed >= 6.6 && elapsed < 7.1)
        {
            var rotation = bones[2].localRotation;
            motion.TriggerPulse();
            snapOnRetrigger = UnityEngine.Mathf.Max(snapOnRetrigger, UnityEngine.Quaternion.Angle(rotation, bones[2].localRotation));
        }
        if (elapsed >= 7.4 && !switched)
        {
            switched = true;
            if (mobilePipeline == null) throw new System.InvalidOperationException("Mobile pipeline asset missing.");
            UnityEngine.QualitySettings.renderPipeline = mobilePipeline;
        }
        float weight = motion.PulseWeight;
        peak = UnityEngine.Mathf.Max(peak, weight);
        if (elapsed >= 0.8 && elapsed < 2.0) keyboardPeak = UnityEngine.Mathf.Max(keyboardPeak, weight);
        if (elapsed >= 2.8 && elapsed < 4.0) mousePeak = UnityEngine.Mathf.Max(mousePeak, weight);
        if (elapsed >= 4.8 && elapsed < 6.0) touchPeak = UnityEngine.Mathf.Max(touchPeak, weight);
        skin.BakeMesh(mesh);
        var vertices = mesh.vertices;
        maxRootDrift = UnityEngine.Mathf.Max(maxRootDrift, UnityEngine.Vector3.Distance(skin.transform.TransformPoint(vertices[rootIndex]), basePosition));
        float tipHeight = skin.transform.TransformPoint(vertices[tipIndex]).y;
        maxTipHeight = UnityEngine.Mathf.Max(maxTipHeight, tipHeight);
        if (elapsed < 0.8) { minIdleTip = UnityEngine.Mathf.Min(minIdleTip, tipHeight); maxIdleTip = UnityEngine.Mathf.Max(maxIdleTip, tipHeight); }
        var bounds = skin.bounds;
        foreach (var vertex in vertices)
            if (!bounds.Contains(skin.transform.TransformPoint(vertex))) outOfBounds++;
        for (int i = 0; i < bones.Length; i++)
        {
            maxBoneTranslation = UnityEngine.Mathf.Max(maxBoneTranslation, UnityEngine.Vector3.Distance(positions[i], bones[i].localPosition));
            maxBoneScale = UnityEngine.Mathf.Max(maxBoneScale, UnityEngine.Vector3.Distance(scales[i], bones[i].localScale));
        }
        maxDiscDrift = UnityEngine.Mathf.Max(maxDiscDrift, UnityEngine.Vector3.Distance(discPosition, disc.position));
        maxDiscRotation = UnityEngine.Mathf.Max(maxDiscRotation, UnityEngine.Quaternion.Angle(discRotation, disc.rotation));
        if (elapsed >= 10 && !resetChecked)
        {
            settledWeight = motion.PulseWeight;
            motion.enabled = false;
            for (int i = 0; i < bones.Length; i++)
                disableError = UnityEngine.Mathf.Max(disableError, UnityEngine.Quaternion.Angle(restRotations[i], bones[i].localRotation));
            motion.enabled = true;
            resetChecked = true;
        }
        if (elapsed < 11) return;
        bool pass = samples > 30 && maxRootDrift < 0.000001f && maxBoneTranslation == 0 && maxBoneScale == 0
            && maxDiscDrift == 0 && maxDiscRotation < 0.001f && outOfBounds == 0 && peak <= 1.0001f
            && keyboardPeak > 0.2f && mousePeak > 0.2f && touchPeak > 0.2f
            && snapOnRetrigger < 0.001f && disableError < 0.001f && settledWeight < 0.005f
            && maxIdleTip - minIdleTip > 0.0001f && errors.Count == 0 && resetChecked && motion.enabled;
        var values = new System.Collections.Generic.Dictionary<string, float> {
            {"samples",samples},{"maxRootDriftMetres",maxRootDrift},{"maxBoneTranslationMetres",maxBoneTranslation},
            {"maxBoneScaleChange",maxBoneScale},{"maxDiscDriftMetres",maxDiscDrift},{"maxDiscRotationDegrees",maxDiscRotation},
            {"outOfBoundsVertices",outOfBounds},{"maxPulseWeight",peak},{"keyboardPulsePeak",keyboardPeak},
            {"mousePulsePeak",mousePeak},{"touchPulsePeak",touchPeak},{"retriggerSnapDegrees",snapOnRetrigger},
            {"disableRestPoseErrorDegrees",disableError},{"settledPulseWeight",settledWeight},
            {"idleTipTravelMetres",maxIdleTip-minIdleTip},{"maxTipHeightMetres",maxTipHeight},{"runtimeErrors",errors.Count}
        };
        string report = "{\"passed\":" + (pass ? "true" : "false") + ",\"mobilePipelineChecked\":true," +
            string.Join(",", values.Select(pair => "\"" + pair.Key + "\":" + pair.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture))) + "}";
        UnityEditor.SessionState.SetString("PetalPrototype.Validation", report);
        System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName,
            "output/flower-petal-prototype/unity-validation.json"), report);
        finished = true;
    }
    catch (System.Exception exception)
    {
        UnityEditor.SessionState.SetString("PetalPrototype.Validation", "FAILED: " + exception);
        finished = true;
    }
    finally
    {
        if (finished)
        {
            UnityEditor.EditorApplication.update -= update;
            UnityEngine.Application.logMessageReceived -= logger;
            UnityEngine.QualitySettings.renderPipeline = originalPipeline;
            UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
            UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
            UnityEngine.InputSystem.InputSystem.RemoveDevice(touch);
            if (originalKeyboard != null && originalKeyboard.added) originalKeyboard.MakeCurrent();
            if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
            if (originalTouch != null && originalTouch.added) originalTouch.MakeCurrent();
            UnityEngine.Object.Destroy(mesh);
        }
    }
};
UnityEditor.EditorApplication.update += update;
return "Validation running for 11 seconds: keyboard, mouse, touch, repeated pulse, mobile renderer, anchoring, bounds, reset.";
