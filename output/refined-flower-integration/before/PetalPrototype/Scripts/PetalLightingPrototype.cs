using System.Collections.Generic;
using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>Two-petal visual lab. Only the assigned indices receive spatial emission.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class PetalLightingPrototype : MonoBehaviour
    {
        public FlowerPaletteController flower;
        public PetalLightingPreset preset;
        public Texture2D reference;
        public Camera previewCamera;
        public int innerIndex = 0, outerIndex = 8;
        public bool effectEnabled = true, animateShimmer = true, loopGrowth;
        [Range(1, 12)] public float cycleSeconds = 5;
        public bool focusPair;
        public bool showPanel = true;
        public PetalLightingSettings Inner { get; private set; }
        public PetalLightingSettings Outer { get; private set; }
        public float Clock { get; set; }
        private float cycle;
        private int selected;
        private Rect originalRect;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private bool originalOrthographic;
        private float originalSize, originalFov;
        private readonly Dictionary<Renderer, bool> visibility = new Dictionary<Renderer, bool>();
        private bool initialized, lastFocus;
        private FlowerPaletteLighting spillRig;
        private bool originalSpillEnabled;
        private GUIStyle label, title, small, button;
        private string savedMessage = "";

        private static PetalLightingSettings Copy(PetalLightingSettings value) =>
            JsonUtility.FromJson<PetalLightingSettings>(JsonUtility.ToJson(value ?? new PetalLightingSettings()));

        private void Start()
        {
            if (flower == null || preset == null || previewCamera == null || innerIndex == outerIndex ||
                flower.GetPetalRenderer(innerIndex) == null || flower.GetPetalRenderer(outerIndex) == null)
            { Debug.LogError("Assign a flower, lighting preset, camera and two distinct valid petal indices.", this); enabled = false; return; }
            LoadLook();
            originalRect = previewCamera.rect; originalPosition = previewCamera.transform.position;
            originalRotation = previewCamera.transform.rotation; originalOrthographic = previewCamera.orthographic;
            originalSize = previewCamera.orthographicSize; originalFov = previewCamera.fieldOfView;
            foreach (var r in flower.GetComponentsInChildren<Renderer>(true)) visibility[r] = r.enabled;
            spillRig = FindFirstObjectByType<FlowerPaletteLighting>();
            originalSpillEnabled = spillRig != null && spillRig.enabled;
            initialized = true;
            lastFocus = !focusPair;
        }

        public void LoadLook()
        {
            Inner = Copy(preset.inner); Outer = Copy(preset.outer);
            loopGrowth = false; cycle = 0; Clock = 0;
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            if (animateShimmer) Clock += Time.deltaTime;
            if (loopGrowth)
            {
                cycle += Time.deltaTime / Mathf.Max(1, cycleSeconds);
                // Smooth growth, a readable tip hold, then fade back to the beginning.
                float t = Mathf.Repeat(cycle, 1);
                float p = t < .72f ? Mathf.SmoothStep(0, 1, t / .72f) : t < .9f ? 1 : 1 - Mathf.SmoothStep(0, 1, (t - .9f) / .1f);
                Inner.progress = Outer.progress = p;
            }
            ApplyNow();
            float panelFraction = showPanel ? Mathf.Clamp01(330f * UiScale / Mathf.Max(1, Screen.width)) : 0;
            var viewport = new Rect(0, 0, 1 - panelFraction, 1);
            bool resized = previewCamera.rect != viewport;
            previewCamera.rect = viewport;
            if (lastFocus != focusPair || resized)
            {
                // The broad full-flower light proxies would illuminate empty space in a close-up.
                if (spillRig != null) spillRig.enabled = originalSpillEnabled && !focusPair;
                foreach (var entry in visibility)
                    if (entry.Key != null) entry.Key.enabled = entry.Value && (!focusPair || entry.Key == flower.GetPetalRenderer(innerIndex) || entry.Key == flower.GetPetalRenderer(outerIndex));
                if (focusPair) FramePair(); else RestoreCameraPose();
                lastFocus = focusPair;
            }
        }

        public void ApplyNow()
        {
            if (Inner == null || Outer == null || flower == null || preset == null) return;
            flower.SetSpatialLighting(innerIndex, Inner, preset.innerMap, Clock, effectEnabled && isActiveAndEnabled);
            flower.SetSpatialLighting(outerIndex, Outer, preset.outerMap, Clock, effectEnabled && isActiveAndEnabled);
        }

        private void FramePair()
        {
            var points = new List<Vector3>();
            foreach (int index in new[] { innerIndex, outerIndex })
            {
                var renderer = flower.GetPetalRenderer(index);
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    var mesh = new Mesh();
                    skinned.BakeMesh(mesh, true);
                    foreach (var v in mesh.vertices) points.Add(renderer.transform.TransformPoint(v));
                    Destroy(mesh);
                }
                else
                    foreach (var v in renderer.GetComponent<MeshFilter>().sharedMesh.vertices) points.Add(renderer.transform.TransformPoint(v));
            }
            var right = previewCamera.transform.right; var up = previewCamera.transform.up; var forward = previewCamera.transform.forward;
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity, maxX = float.NegativeInfinity, maxY = float.NegativeInfinity, z = 0;
            foreach (var p in points)
            {
                float x = Vector3.Dot(p, right), y = Vector3.Dot(p, up);
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); z += Vector3.Dot(p, forward);
            }
            Vector3 center = right * ((minX + maxX) * .5f) + up * ((minY + maxY) * .5f) + forward * (z / points.Count);
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = Mathf.Max((maxY - minY) * .5f, (maxX - minX) * .5f / previewCamera.aspect) * 1.3f;
            previewCamera.transform.position = center - forward * 2;
        }

        private void RestoreCameraPose()
        {
            previewCamera.transform.SetPositionAndRotation(originalPosition, originalRotation);
            previewCamera.orthographic = originalOrthographic; previewCamera.orthographicSize = originalSize; previewCamera.fieldOfView = originalFov;
        }

        private float UiScale => Mathf.Clamp(Screen.height / 960f, .65f, 1.8f);
        private void OnGUI()
        {
            if (!initialized || !showPanel) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(.91f, .88f, .8f) } };
                title = new GUIStyle(label) { fontSize = 22, fontStyle = FontStyle.Bold };
                small = new GUIStyle(label) { fontSize = 12, wordWrap = true };
                button = new GUIStyle(GUI.skin.button) { fontSize = 14, fixedHeight = 29 };
            }
            Matrix4x4 oldMatrix = GUI.matrix; Color oldColor = GUI.color;
            float scale = UiScale; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float w = Screen.width / scale, h = Screen.height / scale, x = w - 330;
            GUI.color = new Color(.055f, .045f, .07f, 1); GUI.DrawTexture(new Rect(x, 0, 330, h), Texture2D.whiteTexture); GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(x + 16, 14, 298, h - 28));
            GUILayout.Label("Petal light study", title);
            GUILayout.Label("Two petals · warm light / deep gaps", small);
            if (reference != null)
            {
                var rect = GUILayoutUtility.GetRect(298, 169); GUI.DrawTexture(rect, reference, ScaleMode.ScaleToFit);
            }
            selected = GUILayout.Toolbar(selected, new[] { "Inner · 01", "Outer · 01" }, button);
            var settings = selected == 0 ? Inner : Outer;
            GUILayout.Space(5);
            if (GUILayout.Button(effectEnabled ? "B · Spatial lighting ON  —  show original" : "A · Original lighting  —  show prototype", button)) effectEnabled = !effectEnabled;
            focusPair = GUILayout.Toggle(focusPair, "Focus the two test petals");
            animateShimmer = GUILayout.Toggle(animateShimmer, "Animate shimmer");
            Slider("Shimmer amount", ref settings.shimmer, 0, 1);
            Slider("Dark gap coverage", ref settings.gapCoverage, 0, 1);
            Slider("Dark gap depth", ref settings.gapDepth, 0, 1);
            Slider("Shimmer speed", ref settings.speed, 0, 2);
            GUILayout.Space(4);
            settings.growth = GUILayout.Toggle(settings.growth, "Side-to-tip growth");
            float previous = settings.progress;
            Slider("Growth progress", ref settings.progress, 0, 1);
            if (previous != settings.progress) loopGrowth = false;
            Slider("Tip brightness", ref settings.tipBrightness, 0, 20);
            Slider("Edge brightness", ref settings.edgeBrightness, 0, 20);
            Slider("Soft wash width", ref settings.washWidth, .02f, .8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(loopGrowth ? "Stop loop" : "Loop both", button)) { loopGrowth = !loopGrowth; if (loopGrowth) { cycle = 0; Inner.growth = Outer.growth = true; } }
            if (GUILayout.Button("Full glow", button)) { loopGrowth = false; Inner.progress = Outer.progress = 1; }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy to other", button)) { if (selected == 0) { float seed = Outer.seed; Outer = Copy(Inner); Outer.seed = seed; } else { float seed = Inner.seed; Inner = Copy(Outer); Inner.seed = seed; } }
            if (GUILayout.Button("Reload saved", button)) LoadLook();
            GUILayout.EndHorizontal();
#if UNITY_EDITOR
            if (GUILayout.Button("Save look as scene defaults", button))
            {
                UnityEditor.Undo.RecordObject(preset, "Save petal lighting look");
                preset.inner = Copy(Inner); preset.outer = Copy(Outer);
                UnityEditor.EditorUtility.SetDirty(preset); UnityEditor.AssetDatabase.SaveAssetIfDirty(preset);
                savedMessage = "Saved. This look will survive leaving Play Mode.";
            }
#endif
            GUILayout.Label(savedMessage.Length > 0 ? savedMessage : "Controls affect the selected test petal.\nOther petals retain their current appearance.", small);
            GUILayout.EndArea();
            GUI.matrix = oldMatrix; GUI.color = oldColor;
        }

        private void Slider(string name, ref float value, float min, float max)
        {
            GUILayout.Label(name + "   " + value.ToString("0.00"), label);
            value = GUILayout.HorizontalSlider(value, min, max);
        }

        private void OnDisable()
        {
            if (!initialized) return;
            if (flower != null)
            {
                flower.SetSpatialLighting(innerIndex, Inner, null, 0, false);
                flower.SetSpatialLighting(outerIndex, Outer, null, 0, false);
            }
            foreach (var entry in visibility) if (entry.Key != null) entry.Key.enabled = entry.Value;
            if (spillRig != null) spillRig.enabled = originalSpillEnabled;
            if (previewCamera != null) { RestoreCameraPose(); previewCamera.rect = originalRect; }
            lastFocus = !focusPair;
        }
    }
}
