using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Amgen.Deepavali.Editor
{
    [Serializable]
    public sealed class SymbolMaskSource
    {
        public string label;
        [Tooltip("Linear RGBA PNG: R symbol coverage, G main/accent role, B detail (222 = neutral), A erase coverage.")]
        public Texture2D mask;
    }

    [CreateAssetMenu(menuName = "Deepavali/Symbol Atlas Source")]
    public sealed class FlowerSymbolAtlasSource : ScriptableObject
    {
        public string outputName = "Flower Symbols";
        [Min(4)] public int padding = 16;
        public SymbolMaskSource[] tiles = Array.Empty<SymbolMaskSource>();
        public FlowerSymbolSet generatedSet;
    }

    [CustomEditor(typeof(FlowerSymbolAtlasSource))]
    public sealed class FlowerSymbolAtlasSourceInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Tiles use the full petal ArtworkUV. Alpha erases the OLD pattern, including blank spaces; it is not surface transparency. The shader protects the existing edge. Keep array order stable: it defines symbol IDs.", MessageType.Info);
            if (GUILayout.Button("Build Shared Symbol Atlas"))
            {
                try { FlowerSymbolAtlasBuilder.Build((FlowerSymbolAtlasSource)target); }
                catch (Exception e) { Debug.LogException(e, target); }
            }
        }
    }

    public static class FlowerSymbolAtlasBuilder
    {
        public static FlowerSymbolSet Build(FlowerSymbolAtlasSource source)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Build symbol atlases in Edit Mode.");
            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(sourcePath) || source.tiles == null || source.tiles.Length == 0)
                throw new InvalidOperationException("Save the source asset and assign at least one mask tile.");
            if (string.IsNullOrWhiteSpace(source.outputName) || source.outputName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidOperationException("Output Name must be a filename without a path.");
            int padding = Mathf.Clamp(source.padding, 4, 64);
            var inputs = new Texture2D[source.tiles.Length];
            Texture2D atlas = null;
            try
            {
                for (int i = 0; i < inputs.Length; i++)
                {
                    var tile = source.tiles[i];
                    string path = tile == null ? null : AssetDatabase.GetAssetPath(tile.mask);
                    if (string.IsNullOrEmpty(path) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Tile " + i + " needs a packed RGBA PNG mask.");
                    inputs[i] = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                    if (!inputs[i].LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException("Cannot read " + path);
                    if (i > 0 && (inputs[i].width != inputs[0].width || inputs[i].height != inputs[0].height))
                        throw new InvalidOperationException("All tiles must have the same pixel dimensions.");
                }
                int tileWidth = inputs[0].width, tileHeight = inputs[0].height;
                int strideX = tileWidth + 2 * padding, strideY = tileHeight + 2 * padding;
                int columns = Mathf.CeilToInt(Mathf.Sqrt(inputs.Length));
                int rows = (inputs.Length + columns - 1) / columns;
                int width = Mathf.NextPowerOfTwo(columns * strideX), height = Mathf.NextPowerOfTwo(rows * strideY);
                if (width > 2048 || height > 2048) throw new InvalidOperationException("Atlas exceeds 2048 pixels. Reduce tile size or symbol count for the mobile atlas.");
                atlas = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                var pixels = new Color32[width * height];
                var rects = new Rect[inputs.Length];
                for (int i = 0; i < inputs.Length; i++)
                {
                    var data = inputs[i].GetPixels32();
                    int ox = (i % columns) * strideX, oy = (i / columns) * strideY;
                    for (int y = 0; y < strideY; y++) for (int x = 0; x < strideX; x++)
                    {
                        int sx = Mathf.Clamp(x - padding, 0, tileWidth - 1), sy = Mathf.Clamp(y - padding, 0, tileHeight - 1);
                        pixels[(oy + y) * width + ox + x] = data[sy * tileWidth + sx];
                    }
                    // Sample at texel centers, not at the gutter boundary.
                    rects[i] = new Rect((ox + padding + 0.5f) / width, (oy + padding + 0.5f) / height,
                        (tileWidth - 1f) / width, (tileHeight - 1f) / height);
                }
                atlas.SetPixels32(pixels); atlas.Apply();
                string folder = Path.GetDirectoryName(sourcePath).Replace('\\', '/');
                string pngPath = folder + "/" + source.outputName + ".png";
                string setPath = folder + "/" + source.outputName + ".asset";
                File.WriteAllBytes(pngPath, atlas.EncodeToPNG());
                AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);
                ConfigureTexture(pngPath);
                var set = AssetDatabase.LoadAssetAtPath<FlowerSymbolSet>(setPath);
                if (set == null)
                {
                    if (File.Exists(setPath)) throw new InvalidOperationException("Output asset exists with a different type.");
                    set = ScriptableObject.CreateInstance<FlowerSymbolSet>();
                    AssetDatabase.CreateAsset(set, setPath);
                }
                var serialized = new SerializedObject(set);
                serialized.FindProperty("atlas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
                serialized.FindProperty("maxMipLevel").floatValue = Mathf.Max(0, Mathf.FloorToInt(Mathf.Log(padding, 2)) - 1);
                var symbols = serialized.FindProperty("symbols"); symbols.arraySize = inputs.Length;
                for (int i = 0; i < inputs.Length; i++)
                {
                    var entry = symbols.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("label").stringValue = source.tiles[i].label;
                    entry.FindPropertyRelative("uvRect").rectValue = rects[i];
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                source.generatedSet = set;
                EditorUtility.SetDirty(source); EditorUtility.SetDirty(set);
                AssetDatabase.SaveAssets();
                return set;
            }
            finally
            {
                foreach (var input in inputs) if (input != null) UnityEngine.Object.DestroyImmediate(input);
                if (atlas != null) UnityEngine.Object.DestroyImmediate(atlas);
            }
        }

        public static void ConfigureTexture(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false; importer.alphaIsTransparency = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = true; importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Standalone", overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.BC7, compressionQuality = 100 });
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.ETC2_RGBA8, compressionQuality = 100 });
            importer.SaveAndReimport();
        }
    }
}
