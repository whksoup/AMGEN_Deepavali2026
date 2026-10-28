using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Gives the Diwali scroll clips the import settings they need for transparency
/// (Transcode on, VP8, Keep Alpha) and adds menu items that drop a ready-made overlay into the scene.
/// </summary>
public class DiwaliScrollVideoImport : AssetPostprocessor
{
    const string Marker = "diwali-scroll-configured";
    static readonly string[] Variants = { "orange", "blue", "green", "red", "yellow", "purple", "pink", "cyan" };
    static readonly Regex ClipName = new Regex(@"^diwali_[a-z]+_(spawn|idle|hide)$");

    static bool IsScrollClip(string path) =>
        path.EndsWith(".webm") && ClipName.IsMatch(Path.GetFileNameWithoutExtension(path));

    void OnPreprocessAsset()
    {
        if (!IsScrollClip(assetPath)) return;
        var importer = assetImporter as VideoClipImporter;
        // Only configure once, so later changes made in the Inspector are kept.
        if (importer == null || importer.userData == Marker) return;
        Configure(importer);
    }

    static void Configure(VideoClipImporter importer)
    {
        var s = importer.defaultTargetSettings;
        s.enableTranscoding = true;
        s.codec = VideoCodec.VP8;
        s.resizeMode = VideoResizeMode.OriginalSize;
        s.bitrateMode = VideoBitrateMode.High;
        s.spatialQuality = VideoSpatialQuality.HighSpatialQuality;
        importer.defaultTargetSettings = s;
        importer.keepAlpha = true;
        importer.importAudio = false;
        importer.userData = Marker;
    }

    [MenuItem("Tools/Diwali Scroll/Reapply Video Import Settings")]
    static void ReapplyImportSettings()
    {
        foreach (var guid in AssetDatabase.FindAssets("diwali_ t:VideoClip"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!IsScrollClip(path)) continue;
            var importer = (VideoClipImporter)AssetImporter.GetAtPath(path);
            Configure(importer);
            importer.SaveAndReimport();
        }
    }

    [MenuItem("GameObject/UI/Diwali Scroll Overlay", false, 2100)]
    [MenuItem("Tools/Diwali Scroll/Add Overlay To Scene")]
    static void CreateOverlay(MenuCommand command)
    {
        var canvasGO = new GameObject("Diwali Scroll Canvas", typeof(Canvas));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var go = new GameObject("Diwali Scroll", typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(canvasGO.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<RawImage>().raycastTarget = false;

        var overlay = go.AddComponent<DiwaliScrollOverlay>();
        var list = new List<DiwaliScrollOverlay.Variant>();
        foreach (var name in Variants)
        {
            var v = new DiwaliScrollOverlay.Variant
            {
                name = char.ToUpper(name[0]) + name.Substring(1),
                spawnClip = LoadClip($"diwali_{name}_spawn"),
                idleClip = LoadClip($"diwali_{name}_idle"),
                hideClip = LoadClip($"diwali_{name}_hide"),
            };
            if (v.spawnClip && v.idleClip && v.hideClip) list.Add(v);
        }
        overlay.variants = list.ToArray();

        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Diwali Scroll Overlay");
        Selection.activeGameObject = go;
    }

    static VideoClip LoadClip(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:VideoClip"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<VideoClip>(path);
        }
        Debug.LogWarning($"Diwali Scroll: could not find {name}.webm");
        return null;
    }
}
