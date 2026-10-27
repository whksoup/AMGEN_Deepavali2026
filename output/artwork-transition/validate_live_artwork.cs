using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Amgen.Deepavali;

public static class LiveArtworkValidation
{
    public static async Task<object> Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required.");
        var c = UnityEngine.Object.FindFirstObjectByType<FlowerPaletteController>();
        var preview = UnityEngine.Object.FindFirstObjectByType<FlowerAppearancePreview>();
        var controls = c.GetComponentsInChildren<PetalArtworkController>().OrderBy(x => x.PetalIndex).ToArray();
        var motions = c.GetComponentsInChildren<PetalMotionController>();
        var data = new SerializedObject(c);
        var entries = data.FindProperty("petalAppearances");
        var durations = Enumerable.Range(0, entries.arraySize).Select(i => entries.GetArrayElementAtIndex(i).FindPropertyRelative("artworkDuration").floatValue).ToArray();
        var renderer = (SkinnedMeshRenderer)data.FindProperty("petals").GetArrayElementAtIndex(0).objectReferenceValue;
        var root = renderer.bones[0]; var rootPosition = root.position;
        var checks = new Dictionary<string, bool>();
        var errors = new List<string>();
        Application.LogCallback logger = (m,t,k) => { if (k == LogType.Error || k == LogType.Exception || k == LogType.Assert) errors.Add(m); };
        Application.logMessageReceived += logger;
        try
        {
            c.SetState(FlowerPaletteState.Base);
            for (int i = 0; i < c.PetalCount; i++) { c.SetSymbol(i, -1); entries.GetArrayElementAtIndex(i).FindPropertyRelative("artworkDuration").floatValue = 0.4f; }
            data.ApplyModifiedPropertiesWithoutUndo();
            await Task.Delay(50);
            preview.SelectAll(); preview.CycleSymbol();
            var previewData = new SerializedObject(preview);
            var bindings = previewData.FindProperty("artworkControls");
            checks["reviewUiBindsEveryPetalComponent"] = bindings.arraySize == c.PetalCount && Enumerable.Range(0, bindings.arraySize).All(i => bindings.GetArrayElementAtIndex(i).objectReferenceValue == controls[i]);
            checks["reviewUiStartsAllPetalReveals"] = controls.All(a => a.IsTransitioning && a.RequestedArtwork == 0);
            controls[0].NextArtwork(); controls[0].NextArtwork(); controls[1].SetArtwork(3);
            c.TransitionTo(FlowerPaletteState.Blue);
            motions[0].TriggerPulse();
            var map = renderer.sharedMaterial.GetVector("_ArtworkUvToRoot");
            await Task.Delay(100);
            checks["liveArtworkAndPaletteAdvanceTogether"] = controls[0].Progress > 0 && controls[0].Progress < 1 && c.TransitionProgress > 0 && c.IsTransitioning;
            checks["motionPreservesArtworkRootCoordinates"] = motions[0].PulseWeight > 0 && renderer.sharedMaterial.GetVector("_ArtworkUvToRoot") == map && Vector3.Distance(root.position, rootPosition) < 0.000001f;
            double deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (controls.Any(a => a.IsTransitioning))
            {
                if (Time.realtimeSinceStartupAsDouble > deadline) throw new TimeoutException("Artwork reveals did not finish.");
                await Task.Delay(30);
            }
            checks["liveLatestChoiceWinsPerPetal"] = controls[0].CurrentArtwork == 2 && controls[1].CurrentArtwork == 3 && controls.Skip(2).All(a => a.CurrentArtwork == 0);
            controls[0].SetArtwork(1); controls[0].SetArtwork(3);
            c.enabled = false;
            await Task.Delay(50);
            c.enabled = true;
            await Task.Delay(50);
            checks["reenableAppliesLatestQueuedArtwork"] = controls[0].CurrentArtwork == 3 && !controls[0].IsTransitioning && !controls[0].HasPendingArtwork;
            checks["runtimeMaterialsMatchPetalsPlusCenter"] = Resources.FindObjectsOfTypeAll<Material>().Count(m => m.name.StartsWith("Flower Petal ") || m.name == "Flower Center (Runtime)") == c.PetalCount + 1;
        }
        catch (Exception e) { checks["completed"] = false; errors.Add(e.ToString()); }
        finally
        {
            if (!c.enabled) c.enabled = true;
            c.SetState(FlowerPaletteState.Base);
            for (int i = 0; i < c.PetalCount; i++) c.SetSymbol(i, -1);
            data.Update(); entries = data.FindProperty("petalAppearances");
            for (int i = 0; i < entries.arraySize; i++) entries.GetArrayElementAtIndex(i).FindPropertyRelative("artworkDuration").floatValue = durations[i];
            data.ApplyModifiedPropertiesWithoutUndo(); preview.SelectAll();
            Application.logMessageReceived -= logger;
        }
        checks["noRuntimeErrors"] = errors.Count == 0;
        var report = new { passed = checks.Values.All(x => x), petalCount = c.PetalCount, checks, errors };
        File.WriteAllText("output/artwork-transition/live-validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
