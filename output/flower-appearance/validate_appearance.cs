using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEditor;
using Amgen.Deepavali;

public static class AppearanceValidation
{
    public static async Task<object> Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required.");
        var c = UnityEngine.Object.FindFirstObjectByType<FlowerPaletteController>();
        var input = UnityEngine.Object.FindFirstObjectByType<PetalLayerPointerInput>();
        var preview = UnityEngine.Object.FindFirstObjectByType<FlowerAppearancePreview>();
        var serialized = new SerializedObject(c);
        var refs = serialized.FindProperty("petals");
        var skins = Enumerable.Range(0, refs.arraySize).Select(i => (SkinnedMeshRenderer)refs.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var motions = skins.Select(s => s.GetComponentInParent<PetalMotionController>()).ToArray();
        var originalMotionEnabled = motions.Select(m => m.enabled).ToArray();
        var disc = (Renderer)serialized.FindProperty("centerDisc").objectReferenceValue;
        var sourcePetal = (Material)serialized.FindProperty("petalMaterial").objectReferenceValue;
        var sourceCenter = AssetDatabase.LoadAssetAtPath<Material>("Assets/HK_Signature/Deepavali/PetalPrototype/Materials/Center Palette.mat");
        var camera = Camera.main;
        var cameraPosition = camera.transform.position;
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        var oldPipeline = QualitySettings.renderPipeline;
        var originalMouse = Mouse.current;
        var originalTouch = Touchscreen.current;
        var oldBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        var oldEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        Mouse mouse = null;
        Touchscreen touch = null;
        var checks = new Dictionary<string, bool>();
        var details = new Dictionary<string, object>();
        var errors = new List<string>();
        Application.LogCallback logger = (m, t, k) => { if (k == LogType.Error || k == LogType.Exception || k == LogType.Assert) errors.Add(m); };
        Application.logMessageReceived += logger;
        var rt = RenderTexture.GetTemporary(800, 800, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var image = new Texture2D(800, 800, TextureFormat.RGB24, false);
        Texture2D cleanMask = null, changedMask = null;
        var bake = new Mesh();
        var originalMeshes = skins.Select(s => s.sharedMesh).ToArray();
        var originalMask = sourcePetal.GetTexture("_BaseMap");
        try
        {
            QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            foreach (var m in motions) m.enabled = false;
            camera.transform.position = new Vector3(0, 1.4f, 0);
            camera.targetTexture = rt;
            Color32[] Capture(string path = null)
            {
                camera.Render();
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 800, 800), 0, 0);
                image.Apply();
                if (path != null) File.WriteAllBytes("output/flower-appearance/" + path + ".png", image.EncodeToPNG());
                return image.GetPixels32();
            }
            double Difference(Color32[] a, Color32[] b)
            {
                long total = 0;
                for (int i = 0; i < a.Length; i++) total += Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b);
                return total / (a.Length * 3.0 * 255.0);
            }

            c.SetState(FlowerPaletteState.Base);
            for (int i = 0; i < c.PetalCount; i++) { c.SetSymbol(i, -1); c.SetPetalState(i, (FlowerPaletteState)(i % 7)); c.SetPetalEmissionScale(i, i * 0.25f); }
            bool independent = true, emission = true;
            var ids = skins.Select(s => s.sharedMaterial.GetInstanceID()).ToArray();
            for (int i = 0; i < skins.Length; i++)
            {
                c.PaletteSet.TryGet((FlowerPaletteState)(i % 7), out var p);
                var mat = skins[i].sharedMaterial;
                independent &= c.GetPetalState(i) == (FlowerPaletteState)(i % 7) && mat.GetColor("_BaseColor") == p.fill.linear && mat.GetColor("_TargetBaseColor") == p.fill.linear;
                emission &= mat.GetFloat("_EmissionScale") == i * 0.25f && Mathf.Abs(mat.GetFloat("_EdgeIntensity") - p.edgeIntensity) < 0.0001f;
            }
            checks["independentPalettes"] = independent && ids.Distinct().Count() == 8;
            checks["emissionUsesFillAndIndependentScale"] = emission;
            checks["invalidRequestsRejected"] = !c.SetPetalState(-1, FlowerPaletteState.Base) && !c.SetPetalState(0, (FlowerPaletteState)9) &&
                !c.SetPetalEmissionScale(0, float.NaN) && !c.SetPetalEmissionScale(0, -1) && !c.SetSymbol(0, 99) && !c.SetSymbol(0, -2) &&
                !c.TransitionToLayout(new[] { FlowerPaletteState.Blue }, FlowerPaletteState.Base);
            for (int i = 0; i < c.PetalCount; i++) c.SetPetalEmissionScale(i, 1);

            // Every sample symbol must use all palettes without swapping textures or meshes.
            var hashes = new HashSet<string>();
            bool paletteConsistent = true;
            for (int s = 0; s < c.SymbolSet.Count; s++)
                for (int p = 0; p < 7; p++)
                {
                    c.SetState((FlowerPaletteState)p);
                    c.PaletteSet.TryGet((FlowerPaletteState)p, out var colors);
                    for (int i = 0; i < c.PetalCount; i++)
                    {
                        c.SetSymbol(i, s);
                        var mat = skins[i].sharedMaterial;
                        paletteConsistent &= mat.GetColor("_TargetBaseColor") == colors.fill.linear &&
                            mat.GetColor("_TargetPatternColor") == colors.ornament.linear && mat.GetColor("_TargetAccentColor") == colors.accent.linear &&
                            mat.GetTexture("_BaseMap") == originalMask && mat.GetTexture("_SymbolAtlas") == c.SymbolSet.Atlas;
                    }
                    Capture();
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                        hashes.Add(Convert.ToBase64String(sha.ComputeHash(image.EncodeToPNG())));
                }
            checks["all28SymbolPaletteCombinations"] = paletteConsistent && hashes.Count == 28;
            details["distinctSymbolPaletteRenders"] = hashes.Count;
            checks["stableRuntimeMaterials"] = skins.Select((s, i) => s.sharedMaterial.GetInstanceID() == ids[i]).All(x => x);
            checks["geometryUnchanged"] = skins.Select((s, i) => s.sharedMesh == originalMeshes[i]).All(x => x) && skins.Sum(s => s.bones.Length) == 24;

            // Decode the shipped compressed mask so its alpha compression noise is tested.
            var maskRT = RenderTexture.GetTemporary(originalMask.width, originalMask.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            try
            {
                Graphics.Blit(originalMask, maskRT);
                RenderTexture.active = maskRT;
                cleanMask = new Texture2D(originalMask.width, originalMask.height, TextureFormat.RGBA32, true, true);
                cleanMask.ReadPixels(new Rect(0, 0, originalMask.width, originalMask.height), 0, 0);
                cleanMask.Apply(true);
            }
            finally { RenderTexture.ReleaseTemporary(maskRT); }
            cleanMask.wrapMode = TextureWrapMode.Clamp; cleanMask.filterMode = FilterMode.Trilinear;
            var dirtyPixels = cleanMask.GetPixels32();
            int changedPixels = 0;
            for (int y = (int)(cleanMask.height * 0.45f); y < cleanMask.height * 0.72f; y++)
                for (int x = (int)(cleanMask.width * 0.33f); x < cleanMask.width * 0.67f; x++)
                {
                    int at = y * cleanMask.width + x;
                    var p = dirtyPixels[at];
                    dirtyPixels[at] = new Color32((byte)(255 - p.r), (byte)(255 - p.g), 128, p.a);
                    changedPixels++;
                }
            changedMask = new Texture2D(cleanMask.width, cleanMask.height, TextureFormat.RGBA32, true, true);
            changedMask.SetPixels32(dirtyPixels); changedMask.Apply(true); changedMask.wrapMode = TextureWrapMode.Clamp; changedMask.filterMode = FilterMode.Trilinear;
            c.SetState(FlowerPaletteState.Base);
            foreach (var s in skins) s.sharedMaterial.SetTexture("_BaseMap", cleanMask);
            for (int i = 0; i < c.PetalCount; i++) c.SetSymbol(i, -1);
            var oldVisible = Capture();
            foreach (var s in skins) s.sharedMaterial.SetTexture("_BaseMap", changedMask);
            var oldChanged = Capture();
            double controlDifference = Difference(oldVisible, oldChanged);
            for (int i = 0; i < c.PetalCount; i++) c.SetSymbol(i, 0);
            var blankChanged = Capture();
            foreach (var s in skins) s.sharedMaterial.SetTexture("_BaseMap", cleanMask);
            var blankClean = Capture("05-blank-erasure");
            double erasedDifference = Difference(blankChanged, blankClean);
            checks["oldArtworkAndPigmentFullyErased"] = changedPixels > 0 && controlDifference > 0.001 && erasedDifference < 0.00001;
            details["oldPatternControlDifference"] = controlDifference;
            details["replacementDifferenceAfterChangingOldPattern"] = erasedDifference;
            foreach (var s in skins) s.sharedMaterial.SetTexture("_BaseMap", originalMask);

            // Emission remains independent of artwork selection and produces bloom.
            var volume = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None)
                .First(v => v.profile != null && v.profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out _));
            bool wasVolumeEnabled = volume.enabled;
            for (int i = 0; i < c.PetalCount; i++) { c.SetSymbol(i, 1); c.SetPetalEmissionScale(i, 2); }
            volume.enabled = true;
            var bloomOn = Capture();
            volume.enabled = false;
            var bloomOff = Capture();
            volume.enabled = wasVolumeEnabled;
            double bloomDifference = Difference(bloomOn, bloomOff);
            checks["bloomStillVisible"] = bloomDifference > 0.00001;
            checks["shadowsRetained"] = skins.All(s => s.receiveShadows && s.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off);
            details["bloomImageDifference"] = bloomDifference;

            var shader = sourcePetal.shader;
            var shaderFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var srp = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", shaderFlags);
            checks["srpBatcherCompatible"] = (int)srp.Invoke(null, new object[] { shader, 0 }) == 0;
            checks["shaderCompiledWithoutErrors"] = !ShaderUtil.ShaderHasError(shader) && ShaderUtil.GetShaderMessages(shader).Length == 0;

            // Runtime wave: changing the request does not alter the current target.
            camera.targetTexture = oldTarget; camera.transform.position = cameraPosition;
            c.SetState(FlowerPaletteState.Base);
            for (int i = 0; i < c.PetalCount; i++) { c.SetSymbol(i, -1); c.SetPetalEmissionScale(i, 1); }
            c.TransitionTo(FlowerPaletteState.Blue);
            await Task.Delay(100);
            var firstTarget = skins[0].sharedMaterial.GetColor("_TargetBaseColor");
            float progressBeforeInspector = c.TransitionProgress;
            var inspector = new SerializedObject(c);
            inspector.FindProperty("petalAppearances").GetArrayElementAtIndex(0).FindPropertyRelative("emissionScale").floatValue = 0.75f;
            inspector.ApplyModifiedPropertiesWithoutUndo();
            await Task.Delay(50);
            checks["inspectorBrightnessDoesNotInterruptWave"] = c.IsTransitioning && c.TransitionProgress >= progressBeforeInspector &&
                skins[0].sharedMaterial.GetColor("_TargetBaseColor") == firstTarget && skins[0].sharedMaterial.GetFloat("_EmissionScale") == 0.75f;
            c.SetPetalEmissionScale(0, 1f);
            c.TransitionTo(FlowerPaletteState.Pink); c.TransitionTo(FlowerPaletteState.Green);
            checks["activeWaveNotInterrupted"] = c.IsTransitioning && c.HasPendingTransition && skins[0].sharedMaterial.GetColor("_TargetBaseColor") == firstTarget;
            await WaitUntil(() => !c.IsTransitioning, 6000);
            checks["latestRequestWinsInRealFrames"] = Enumerable.Range(0, c.PetalCount).All(i => c.GetPetalState(i) == FlowerPaletteState.Green);
            var update = (Action)Delegate.CreateDelegate(typeof(Action), c, typeof(FlowerPaletteController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic));
            c.TransitionTo(FlowerPaletteState.Orange);
            for (int i = 0; i < 16; i++) update();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 256; i++) update();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            checks["transitionUpdatesAllocateZeroManagedBytes"] = allocated == 0;
            details["allocatedBytesFor256Updates"] = allocated;
            c.SetState(FlowerPaletteState.Base);

            // Real Input System events through the scene UI.
            foreach (var m in motions) { m.enabled = false; m.enabled = true; }
            preview.SelectAll();
            // The validation Editor is headless/unfocused. Let synthetic devices feed the player.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>("AppearanceValidationMouse");
            touch = InputSystem.AddDevice<Touchscreen>("AppearanceValidationTouch");
            mouse.MakeCurrent(); touch.MakeCurrent();
            Canvas.ForceUpdateCanvases();
            var bar = GameObject.Find("Palette Choices").transform;
            Vector2 ButtonPoint(int i)
            {
                var r = (RectTransform)bar.GetChild(i);
                return RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center));
            }
            checks["uiDoesNotPickPetals"] = !input.TryPulseAtScreenPoint(ButtonPoint(2));
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1, position = ButtonPoint(2) });
            await Task.Delay(100);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = ButtonPoint(2) });
            await Task.Delay(100);
            checks["paletteUiRequestsWave"] = c.GetRequestedPetalState(0) == FlowerPaletteState.Pink && c.IsTransitioning && motions.All(m => m.PulseWeight == 0);
            c.SetState(FlowerPaletteState.Base);

            var roots = skins.Select(s => s.bones[0].position).ToArray();
            var mapX = skins.Select(s => s.sharedMaterial.GetVector("_RestUvToFlowerX")).ToArray();
            var mapZ = skins.Select(s => s.sharedMaterial.GetVector("_RestUvToFlowerZ")).ToArray();
            skins[0].BakeMesh(bake);
            var beforeVertices = bake.vertices;
            Vector2 PetalPoint(int index) => camera.WorldToScreenPoint(motions[index].transform.TransformPoint(new Vector3(0, 0.02f, -0.24f)));
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = PetalPoint(0) });
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Began, position = PetalPoint(2) });
            await Task.Delay(100);
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = PetalPoint(0) });
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = PetalPoint(2) });
            float peak0 = 0, peak2 = 0, otherPeak = 0, rootDrift = 0;
            bool mapsStable = true;
            for (int sample = 0; sample < 16; sample++)
            {
                peak0 = Mathf.Max(peak0, motions[0].PulseWeight); peak2 = Mathf.Max(peak2, motions[2].PulseWeight);
                for (int i = 0; i < motions.Length; i++)
                {
                    if (i != 0 && i != 2) otherPeak = Mathf.Max(otherPeak, motions[i].PulseWeight);
                    rootDrift = Mathf.Max(rootDrift, Vector3.Distance(roots[i], skins[i].bones[0].position));
                    mapsStable &= skins[i].sharedMaterial.GetVector("_RestUvToFlowerX") == mapX[i] && skins[i].sharedMaterial.GetVector("_RestUvToFlowerZ") == mapZ[i];
                }
                await Task.Delay(30);
            }
            skins[0].BakeMesh(bake);
            var afterVertices = bake.vertices;
            float vertexMotion = beforeVertices.Select((p, i) => Vector3.Distance(p, afterVertices[i])).Max();
            checks["simultaneousTouchPulsesAreIndependent"] = peak0 > 0.2f && peak2 > 0.2f && otherPeak == 0;
            checks["rootsRemainPinned"] = rootDrift < 0.000001f;
            checks["restCoordinatesStableUnderMotion"] = mapsStable && vertexMotion > 0.00001f;
            checks["touchSelectsPetalForAppearance"] = preview.SelectedPetal == 2;
            details["touchPulsePeaks"] = new[] { peak0, peak2, otherPeak };
            details["maxRootDriftMetres"] = rootDrift;
            details["vertexMotionMetres"] = vertexMotion;
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1, position = ButtonPoint(5) });
            await Task.Delay(100);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = ButtonPoint(5) });
            await Task.Delay(100);
            checks["selectedPetalUiDoesNotRecolorOthers"] = c.GetRequestedPetalState(2) == FlowerPaletteState.Blue && c.GetRequestedPetalState(0) == FlowerPaletteState.Base;
            preview.CycleSymbol();
            checks["symbolUiAffectsOnlySelection"] = c.GetSymbol(2) == 0 && c.GetSymbol(0) == -1;
            preview.SetEmission(0.4f);
            checks["brightnessUiAffectsOnlySelection"] = Mathf.Abs(c.GetPetalEmissionScale(2) - 0.4f) < 0.0001f && c.GetPetalEmissionScale(0) == 1;

            // Same rest radius at the same UV on every rotated petal, with the disc starting first.
            float At(Material m, Vector2 uv, float p)
            {
                var mx = m.GetVector("_RestUvToFlowerX"); var mz = m.GetVector("_RestUvToFlowerZ");
                float radius = new Vector2(mx.x * uv.x + mx.y * uv.y + mx.z, mz.x * uv.x + mz.y * uv.y + mz.z).magnitude;
                float soft = m.GetFloat("_WaveSoftness");
                float front = Mathf.Lerp(-soft, m.GetFloat("_WaveRadius") + soft, p);
                float t = Mathf.Clamp01((radius - front + soft) / (2 * soft));
                return 1 - t * t * (3 - 2 * t);
            }
            var first = skins[0].sharedMaterial;
            checks["radialDirectionAndCenter"] = At(first, new Vector2(0.5f, 0.1f), 0.5f) > 0.95f &&
                At(first, new Vector2(0.5f, 0.9f), 0.5f) < 0.05f &&
                At(disc.sharedMaterial, new Vector2(0.5f, 0.5f), 0.1f) > At(first, new Vector2(0.5f, 0), 0.1f);
            checks["ringSharesRadialTiming"] = skins.All(s => Mathf.Abs(At(s.sharedMaterial, new Vector2(0.5f, 0.4f), 0.5f) - At(first, new Vector2(0.5f, 0.4f), 0.5f)) < 0.00001f);

            c.SetState(FlowerPaletteState.Base);
            c.SetPetalState(2, FlowerPaletteState.Blue); c.SetSymbol(2, 3); c.SetPetalEmissionScale(2, 0.4f);
            c.TransitionPetalTo(2, FlowerPaletteState.Orange);
            c.TransitionPetalTo(2, FlowerPaletteState.Blue);
            bool restored = true, retained = true;
            for (int cycle = 0; cycle < 3; cycle++)
            {
                c.enabled = false;
                restored &= skins.All(s => s.sharedMaterial == sourcePetal) && disc.sharedMaterial == sourceCenter;
                await Task.Delay(50);
                c.enabled = true;
                retained &= c.GetPetalState(2) == FlowerPaletteState.Blue && c.GetSymbol(2) == 3 && Mathf.Abs(c.GetPetalEmissionScale(2) - 0.4f) < 0.00001f;
                await Task.Delay(50);
            }
            int owned = Resources.FindObjectsOfTypeAll<Material>().Count(m => m.name.StartsWith("Flower Petal ") || m.name == "Flower Center (Runtime)");
            checks["materialsRestoredOnDisable"] = restored;
            checks["appearanceRetainedOnReenable"] = retained;
            checks["noMaterialLeaksAcrossReenable"] = owned == 9;
            details["liveOwnedMaterials"] = owned;
        }
        catch (Exception exception)
        {
            checks["completed"] = false;
            details["exception"] = exception.ToString();
        }
        finally
        {
            if (!c.enabled) c.enabled = true;
            c.SetState(FlowerPaletteState.Base);
            for (int i = 0; i < c.PetalCount; i++) { c.SetSymbol(i, -1); c.SetPetalEmissionScale(i, 1); skins[i].sharedMaterial.SetTexture("_BaseMap", originalMask); }
            preview.SelectAll();
            for (int i = 0; i < motions.Length; i++) motions[i].enabled = originalMotionEnabled[i];
            camera.targetTexture = oldTarget; camera.transform.position = cameraPosition;
            RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.Destroy(image); UnityEngine.Object.Destroy(bake);
            if (cleanMask != null) UnityEngine.Object.Destroy(cleanMask);
            if (changedMask != null) UnityEngine.Object.Destroy(changedMask);
            QualitySettings.renderPipeline = oldPipeline;
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
            if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
            if (originalTouch != null && originalTouch.added) originalTouch.MakeCurrent();
            InputSystem.settings.backgroundBehavior = oldBackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInputBehavior;
            Application.logMessageReceived -= logger;
        }
        checks["noRuntimeErrors"] = errors.Count == 0;
        details["runtimeErrors"] = errors;
        var report = new { passed = checks.Values.All(x => x), checks, details };
        File.WriteAllText("output/flower-appearance/integration-validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return report;
    }

    private static async Task WaitUntil(Func<bool> predicate, int milliseconds)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + milliseconds / 1000.0;
        while (!predicate())
        {
            if (Time.realtimeSinceStartupAsDouble > deadline) throw new TimeoutException("Validation timed out waiting for the player.");
            await Task.Delay(30);
        }
    }
}
