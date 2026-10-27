using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using Amgen.Deepavali;

public static class ArtworkValidation
{
    public static object Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required.");
        var c = UnityEngine.Object.FindFirstObjectByType<FlowerPaletteController>();
        var controls = c.GetComponentsInChildren<PetalArtworkController>().OrderBy(x => x.PetalIndex).ToArray();
        var serialized = new SerializedObject(c);
        var refs = serialized.FindProperty("petals");
        var skins = Enumerable.Range(0, refs.arraySize).Select(i => (SkinnedMeshRenderer)refs.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var waveField = c.GetType().GetField("artworkWaves", flags);
        var update = (Action)Delegate.CreateDelegate(typeof(Action), c, c.GetType().GetMethod("Update", flags));
        var waveType = waveField.FieldType.GetElementType();
        var started = waveType.GetField("startedAt");
        var duration = waveType.GetField("duration");
        var target = waveType.GetField("target");
        var checks = new Dictionary<string, bool>();
        var detail = new Dictionary<string, object>();
        var oldPipeline = QualitySettings.renderPipeline;
        var oldActive = RenderTexture.active;
        var originalDurations = Enumerable.Range(0, c.PetalCount).Select(i => serialized.FindProperty("petalAppearances").GetArrayElementAtIndex(i).FindPropertyRelative("artworkDuration").floatValue).ToArray();
        GameObject quad = null, view = null;
        Mesh mesh = null;
        Material material = null;
        Texture2D image = null;
        RenderTexture rt = null;
        void Step(int index, float p)
        {
            var waves = (Array)waveField.GetValue(c);
            var wave = waves.GetValue(index);
            started.SetValue(wave, Time.timeAsDouble - (float)duration.GetValue(wave) * p);
            waves.SetValue(wave, index);
            update();
        }
        int Target(int index) => (int)target.GetValue(((Array)waveField.GetValue(c)).GetValue(index));
        void Reset()
        {
            c.SetState(FlowerPaletteState.Base);
            for (int i = 0; i < c.PetalCount; i++) { c.SetSymbol(i, -1); c.SetPetalEmissionScale(i, 1); }
        }
        try
        {
            QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            Reset();
            var ids = skins.Select(s => s.sharedMaterial.GetInstanceID()).ToArray();
            detail["petalCount"] = c.PetalCount;
            checks["oneBoundControllerPerPetal"] = controls.Length == c.PetalCount && controls.Select(x => x.PetalIndex).SequenceEqual(Enumerable.Range(0, c.PetalCount));
            checks["invalidRequestsRejected"] = !c.TransitionSymbol(-1, 0) && !c.TransitionSymbol(c.PetalCount, 0) && !c.TransitionSymbol(0, -2) && !controls[0].TrySetArtwork(99);
            controls[0].SetArtwork(1);
            Step(0, 0.4f);
            c.TransitionTo(FlowerPaletteState.Blue);
            c.GetType().GetField("waveStartedAt", flags).SetValue(c, Time.timeAsDouble - c.TransitionDuration * 0.3);
            update();
            checks["simultaneousIndependentPaletteAndArtwork"] = c.IsTransitioning && c.IsArtworkTransitioning(0) && Math.Abs(c.GetArtworkProgress(0) - 0.4f) < 0.001f && Math.Abs(c.TransitionProgress - 0.3f) < 0.001f;
            controls[1].SetArtwork(2);
            checks["petalsStartIndependently"] = c.GetArtworkProgress(1) == 0 && c.GetArtworkProgress(0) > 0.39f && !c.IsArtworkTransitioning(2);
            c.SetState(FlowerPaletteState.Green);
            c.SetPetalEmissionScale(0, 0.4f);
            checks["paletteResetAndBrightnessPreserveArtwork"] = c.IsArtworkTransitioning(0) && c.GetArtworkProgress(0) > 0.39f && Target(0) == 1;
            controls[0].SetArtwork(3); controls[0].SetArtwork(0);
            checks["activeRevealFinishesBeforeLatestRequest"] = Target(0) == 1 && controls[0].RequestedArtwork == 0 && controls[0].CurrentArtwork == -1 && controls[0].HasPendingArtwork;
            Step(0, 1.01f);
            checks["nextRevealUsesOnlyLatestRequest"] = controls[0].CurrentArtwork == 1 && Target(0) == 0 && controls[0].Progress == 0 && controls[0].IsTransitioning;
            Step(0, 1.01f);
            controls[0].SetArtwork(0);
            checks["settledRequestDoesNotRestart"] = controls[0].CurrentArtwork == 0 && !controls[0].IsTransitioning && controls[0].Progress == 1;
            controls[0].SetArtwork(1); controls[0].SetArtwork(3); controls[0].SetArtwork(1);
            checks["requestingActiveTargetClearsPending"] = !controls[0].HasPendingArtwork;
            c.TransitionTo(FlowerPaletteState.Pink);
            controls[0].SetArtworkImmediate(3);
            checks["immediateArtworkResetIsLocal"] = !controls[0].IsTransitioning && controls[0].CurrentArtwork == 3 && controls[1].IsTransitioning && c.IsTransitioning;
            controls[0].RestoreOriginal();
            checks["restoringOriginalUsesReveal"] = controls[0].IsTransitioning && Target(0) == -1 && skins[0].sharedMaterial.GetFloat("_TargetSymbolEnabled") == 0;
            Step(0, 1.01f);
            checks["restoresOriginalExactly"] = controls[0].CurrentArtwork == -1 && !controls[0].IsTransitioning;
            Reset();
            serialized.Update();
            var p0 = serialized.FindProperty("petalAppearances").GetArrayElementAtIndex(0);
            p0.FindPropertyRelative("symbol").intValue = 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            update(); Step(0, 0.4f);
            serialized.Update();
            serialized.FindProperty("petalAppearances").GetArrayElementAtIndex(0).FindPropertyRelative("emissionScale").floatValue = 1.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo(); update();
            checks["inspectorArtworkAnimatesAndBrightnessPreservesProgress"] = Target(0) == 2 && c.GetArtworkProgress(0) > 0.39f && c.GetArtworkProgress(0) < 0.41f;
            Reset(); serialized.Update();
            serialized.FindProperty("petalAppearances").GetArrayElementAtIndex(0).FindPropertyRelative("symbol").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            c.SetState(FlowerPaletteState.Orange); update();
            checks["sameFramePaletteResetDoesNotDropInspectorArtwork"] = c.IsArtworkTransitioning(0) && Target(0) == 1;
            Reset();
            serialized.Update();
            serialized.FindProperty("petalAppearances").GetArrayElementAtIndex(0).FindPropertyRelative("artworkDuration").floatValue = 0.25f;
            serialized.FindProperty("petalAppearances").GetArrayElementAtIndex(1).FindPropertyRelative("artworkDuration").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo(); update();
            controls[0].SetArtwork(1); controls[1].SetArtwork(2);
            Step(0, 0.8f); Step(1, 0.2f);
            checks["perPetalDurationRespected"] = Math.Abs(c.GetArtworkProgress(0) - 0.8f) < 0.001f && Math.Abs(c.GetArtworkProgress(1) - 0.2f) < 0.001f;
            for (int i = 0; i < c.PetalCount; i++) c.TransitionSymbol(i, 3);
            for (int i = 0; i < 32; i++) update();
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 256; i++) update();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            checks["allPetalArtworkUpdatesAllocateZeroManagedBytes"] = allocated == 0;
            detail["allocatedBytesFor256Updates"] = allocated;
            checks["materialsReusedDuringReveals"] = skins.Select(s => s.sharedMaterial.GetInstanceID()).SequenceEqual(ids);

            // GPU regression on a UV-aligned quad. No post-processing: compare exact old/new
            // pixels on either side of the circular reveal, for all direction combinations.
            Reset();
            quad = new GameObject("Artwork GPU validation quad") { layer = 31, hideFlags = HideFlags.DontSave };
            quad.transform.position = new Vector3(20, 0, 0);
            mesh = new Mesh();
            mesh.vertices = new[] { Vector3.zero, Vector3.up, new Vector3(1,1,0), Vector3.right };
            mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            quad.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = quad.AddComponent<MeshRenderer>();
            material = new Material(skins[0].sharedMaterial);
            renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            view = new GameObject("Artwork GPU validation camera") { hideFlags = HideFlags.DontSave };
            var camera = view.AddComponent<Camera>(); camera.enabled = false;
            view.transform.position = new Vector3(20.5f, 0.5f, -2f);
            camera.orthographic = true; camera.orthographicSize = 0.5f; camera.aspect = 1; camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 5f; camera.allowMSAA = false;
            rt = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = rt;
            image = new Texture2D(256, 256, TextureFormat.RGB24, false);
            Color32[] Capture(float progress)
            {
                material.SetFloat("_ArtworkProgress", progress);
                camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply(); return image.GetPixels32();
            }
            int Difference(Color32 a, Color32 b) => Math.Max(Math.Abs(a.r - b.r), Math.Max(Math.Abs(a.g - b.g), Math.Abs(a.b - b.b)));
            var cases = new[] { new[] { -1, 0 }, new[] { 0, 3 }, new[] { 1, 2 }, new[] { 3, -1 } };
            var gpu = new List<object>();
            foreach (var pair in cases)
            {
                c.SetSymbol(0, pair[0]); c.TransitionSymbol(0, pair[1]);
                material.CopyPropertiesFromMaterial(skins[0].sharedMaterial);
                material.SetFloat("_EmissionScale", 0);
                var a = Capture(0); var b = Capture(1);
                int changed = 0, oldCount = 0, newCount = 0, mismatched = 0;
                var map = material.GetVector("_ArtworkUvToRoot");
                float soft = material.GetFloat("_ArtworkSoftness");
                foreach (float p in new[] { 0.25f, 0.5f, 0.75f })
                {
                    var mid = Capture(p);
                    float front = Mathf.Lerp(-soft, material.GetFloat("_ArtworkRadius") + soft, p);
                    for (int y = 2; y < 254; y++) for (int x = 2; x < 254; x++)
                    {
                        int n = y * 256 + x;
                        if (Difference(a[n], b[n]) < 6) continue;
                        changed++;
                        float radius = new Vector2((x + 0.5f) / 256 * map.x + map.z, (y + 0.5f) / 256 * map.y + map.w).magnitude;
                        if (radius < front - soft - 0.01f) { newCount++; if (Difference(mid[n], b[n]) > 1) mismatched++; }
                        if (radius > front + soft + 0.01f) { oldCount++; if (Difference(mid[n], a[n]) > 1) mismatched++; }
                    }
                }
                bool passed = changed > 100 && oldCount > 50 && newCount > 50 && mismatched == 0;
                checks[$"gpuRadialReveal_{pair[0]}_to_{pair[1]}"] = passed;
                gpu.Add(new { from = pair[0], to = pair[1], changed, oldCount, newCount, mismatched });
            }
            detail["gpuPixelComparisons"] = gpu;
            var shader = material.shader;
            checks["noShaderErrors"] = ShaderUtil.GetShaderMessages(shader).Length == 0;
            var batch = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            checks["srpBatcherCompatible"] = (int)batch.Invoke(null, new object[] { shader, 0 }) == 0;
        }
        catch (Exception e) { checks["completed"] = false; detail["exception"] = e.ToString(); }
        finally
        {
            Reset(); serialized.Update();
            for (int i = 0; i < c.PetalCount; i++) serialized.FindProperty("petalAppearances").GetArrayElementAtIndex(i).FindPropertyRelative("artworkDuration").floatValue = originalDurations[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            RenderTexture.active = oldActive;
            if (rt != null) RenderTexture.ReleaseTemporary(rt);
            if (quad != null) UnityEngine.Object.DestroyImmediate(quad);
            if (view != null) UnityEngine.Object.DestroyImmediate(view);
            if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            if (material != null) UnityEngine.Object.DestroyImmediate(material);
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            QualitySettings.renderPipeline = oldPipeline;
        }
        var report = new { passed = checks.Values.All(x => x), checks, detail };
        File.WriteAllText("output/artwork-transition/validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
