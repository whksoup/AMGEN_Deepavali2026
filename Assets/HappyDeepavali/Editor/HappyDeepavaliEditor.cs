using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Gives the Happy Deepavali clips the import settings they need for transparency (Transcode on, VP8, Keep Alpha),
/// gives images in HappyDeepavali/Logo clean transparent edges, and adds a menu item that drops the title into the scene.
/// </summary>
public class HappyDeepavaliImport : AssetPostprocessor
{
    const string Marker = "happy-deepavali-configured";
    static readonly string[] ClipNames = { "happy_deepavali_spawn", "happy_deepavali_idle", "happy_deepavali_hide" };

    static bool IsTitleClip(string path) =>
        path.EndsWith(".webm") && System.Array.IndexOf(ClipNames, Path.GetFileNameWithoutExtension(path)) >= 0;

    void OnPreprocessAsset()
    {
        if (!IsTitleClip(assetPath)) return;
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

    // Logo images in HappyDeepavali/Logo (the placeholder, or a logo.png dropped in later).
    void OnPreprocessTexture()
    {
        var importer = assetImporter as TextureImporter;
        if (!assetPath.Contains("/HappyDeepavali/Logo/") || importer == null || importer.userData == Marker) return;
        importer.userData = Marker;
        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }

    [MenuItem("Tools/Happy Deepavali/Reapply Video Import Settings")]
    static void ReapplyImportSettings()
    {
        foreach (var name in ClipNames)
        {
            var path = FindPath(name, "t:VideoClip");
            if (path == null) continue;
            var importer = (VideoClipImporter)AssetImporter.GetAtPath(path);
            Configure(importer);
            importer.SaveAndReimport();
        }
    }

    // Centre of the logo in the 2560 x 1118 frame: 863.5 px from the top.
    const float LogoAnchorY = 1f - 863.5f / 1118f;

    [MenuItem("GameObject/UI/Happy Deepavali Title", false, 2101)]
    [MenuItem("Tools/Happy Deepavali/Add Title To Scene")]
    static void CreateTitle(MenuCommand command)
    {
        var canvasGO = new GameObject("Happy Deepavali Canvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1001;
        // Lay the UI out in the clips' own 2560 x 1118 pixels, scaled to whatever the screen is.
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560, 1118);
        scaler.matchWidthOrHeight = 0.5f;

        var go = new GameObject("Happy Deepavali", typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(canvasGO.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<RawImage>().raycastTarget = false;

        var overlay = go.AddComponent<HappyDeepavaliOverlay>();
        overlay.spawnClip = Load<VideoClip>("happy_deepavali_spawn", "t:VideoClip");
        overlay.idleClip = Load<VideoClip>("happy_deepavali_idle", "t:VideoClip");
        overlay.hideClip = Load<VideoClip>("happy_deepavali_hide", "t:VideoClip");

        // The logo is its own image under the title: swap its Texture for logo.png and set Width / Height to resize.
        var logoGO = new GameObject("Logo", typeof(RectTransform), typeof(RawImage));
        logoGO.transform.SetParent(go.transform, false);
        var tex = Load<Texture2D>("logo_placeholder", "t:Texture2D");
        var logo = logoGO.GetComponent<RawImage>();
        logo.texture = tex;
        logo.raycastTarget = false;
        var lrt = (RectTransform)logoGO.transform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, LogoAnchorY);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.anchoredPosition = Vector2.zero;
        lrt.sizeDelta = tex ? new Vector2(tex.width, tex.height) : new Vector2(537, 105);
        overlay.logo = logo;

        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Happy Deepavali Title");
        Selection.activeGameObject = go;
    }

    static string FindPath(string name, string filter)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " " + filter))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name) return path;
        }
        Debug.LogWarning($"Happy Deepavali: could not find {name}");
        return null;
    }

    static T Load<T>(string name, string filter) where T : Object
    {
        var path = FindPath(name, filter);
        return path == null ? null : AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
