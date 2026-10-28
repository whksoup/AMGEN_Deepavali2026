using UnityEngine;

namespace Amgen.Deepavali
{
    [DefaultExecutionOrder(250)]
    public sealed class PetalWheelSequencePreview : MonoBehaviour
    {
        public PetalWheelLightSequence sequence;
        public Camera previewCamera;
        private Rect originalRect;
        private Vector2 scroll;
        private GUIStyle text, title, button;
        private float Scale => Mathf.Clamp(Screen.height / 960f, .65f, 1.8f);
        private void Start() { if (previewCamera != null) originalRect = previewCamera.rect; }
        private void LateUpdate() { if (previewCamera != null) previewCamera.rect = new Rect(0, 0, 1 - Mathf.Clamp01(350 * Scale / Mathf.Max(1, Screen.width)), 1); }
        private void OnDisable() { if (previewCamera != null) previewCamera.rect = originalRect; }
        private void OnGUI()
        {
            if (sequence == null || sequence.Pattern == null) return;
            if (text == null)
            {
                text = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, normal = { textColor = new Color(.92f, .87f, .78f) } };
                title = new GUIStyle(text) { fontSize = 23, fontStyle = FontStyle.Bold };
                button = new GUIStyle(GUI.skin.button) { fontSize = 14, fixedHeight = 30 };
            }
            var p = sequence.Pattern; var t = sequence.timing;
            var matrix = GUI.matrix; var color = GUI.color; float scale = Scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float x = Screen.width / scale - 350, height = Screen.height / scale;
            GUI.color = new Color(.055f, .045f, .07f); GUI.DrawTexture(new Rect(x, 0, 350, height), Texture2D.whiteTexture); GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(x + 16, 16, 318, height - 32)); scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Petal wheel lighting", title);
            GUILayout.Label("Shimmer → Pulse → Wheel → Bloom", text);
            GUILayout.Label(p.Phase, title);
            GUILayout.Label(p.Mode == PetalWheelMode.Win ? "Result: " + sequence.style.outcomes[p.Winner] : "Inner wheel: " + Mathf.Abs(p.Velocity).ToString("0.0") + " steps/s", text);
            GUI.enabled = !p.Busy;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Shimmer", button)) { sequence.autoPreview = false; sequence.Shimmer(); }
            if (GUILayout.Button("Enter wheel", button)) { sequence.autoPreview = false; sequence.EnterWheel(); }
            if (GUILayout.Button("Spin", button)) { sequence.autoPreview = false; sequence.Spin(); }
            GUILayout.EndHorizontal(); GUI.enabled = true;
            if (GUILayout.Button(sequence.autoPreview ? "Stop automatic preview" : "Play full sequence", button))
            { sequence.autoPreview = !sequence.autoPreview; if (sequence.autoPreview) sequence.ResetPreview(); }
            GUILayout.Space(8);
            Slider("Inner / center shimmer intensity", ref sequence.shimmerIntensity, 1, 8);
            Slider("Outer spin intensity", ref sequence.outerLightIntensity, 0, 8);
            Slider("Outer shimmer intensity", ref sequence.outerShimmerIntensity, 0, 8);
            Slider("Outer full-petal glow", ref sequence.outerSurfaceGlow, 0, 8);
            Slider("Shimmer period", ref t.shimmerPeriod, 2, 12); Slider("Shimmer level", ref t.shimmerLevel, .2f, 1);
            Slider("Entry pulse", ref t.pulseLength, .3f, 2); Slider("Settle time", ref t.settleLength, .4f, 4);
            Slider("Top speed (steps/s)", ref t.speed, 6, 32); Slider("Cruise time", ref t.cruise, 0, 4); Slider("Slowdown", ref t.slowdown, 1.5f, 8);
            Slider("Fade in", ref t.fadeIn, 0, .4f); Slider("Inner afterglow", ref t.innerAfterglow, 0, 1); Slider("Outer afterglow", ref t.outerAfterglow, 0, 1);
            Slider("Breath period", ref t.breathPeriod, 2, 10); Slider("Breath variation", ref t.breathVariation, 0, 1);
            Slider("Light up on spin", ref t.lightUp, .1f, 1.5f); Slider("Idle steps/s", ref t.idleSpeed, 0, 4);
            t.windup = GUILayout.Toggle(t.windup, "Wind-up before launch"); t.overshoot = GUILayout.Toggle(t.overshoot, "Overshoot and settle");
            t.outerMotion = (PetalWheelOuterMotion)GUILayout.SelectionGrid((int)t.outerMotion, new[] { "Breathe", "Chase" }, 2);
            t.winPattern = (PetalWheelWinPattern)GUILayout.SelectionGrid((int)t.winPattern, new[] { "Breathe", "Swell", "Burst", "Alternate", "Burst/flash", "Sparkle" }, 2);
            t.bloom = GUILayout.Toggle(t.bloom, "Bloom into result palette");
            Slider("Bloom delay", ref t.bloomDelay, .3f, 3); Slider("Bloom spread", ref t.bloomSpread, .6f, 4); Slider("Hold after bloom", ref t.hold, 1, 8);
            t.afterHold = (PetalWheelAfterHold)GUILayout.SelectionGrid((int)t.afterHold, new[] { "Ready", "Shimmer", "Stay" }, 3);
            GUILayout.Label("Preview a result", text); GUI.enabled = !p.Busy;
            for (int row = 0; row < 4; row++)
            { GUILayout.BeginHorizontal(); for (int col = 0; col < 2; col++) { int slot = row * 2 + col; if (GUILayout.Button(sequence.style.outcomes[slot].ToString(), button)) { sequence.autoPreview = false; sequence.PreviewResult(slot); } } GUILayout.EndHorizontal(); }
            GUI.enabled = true;
            GUILayout.Label("Timing changes are for this play session. Inspector defaults are saved with this scene.", text);
            GUILayout.EndScrollView(); GUILayout.EndArea(); GUI.matrix = matrix; GUI.color = color;
        }
        private void Slider(string label, ref float value, float min, float max)
        { GUILayout.Label(label + "   " + value.ToString("0.00"), text); value = GUILayout.HorizontalSlider(value, min, max); }
    }
}
