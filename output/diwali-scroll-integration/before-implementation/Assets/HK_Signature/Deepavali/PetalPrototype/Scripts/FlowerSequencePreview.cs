using UnityEngine;

namespace Amgen.Deepavali
{
    [DefaultExecutionOrder(250)]
    public sealed class FlowerSequencePreview : MonoBehaviour
    {
        public FlowerLightSequence sequence;
        public Camera previewCamera;
        public Texture2D reference;
        private Rect originalRect;
        private GUIStyle heading, text, button, caption;
        private string saved = "";
        private Vector2 scroll;
        private float UiScale => Mathf.Clamp(Screen.height / 960f, .65f, 1.8f);
        private void Start() { if (previewCamera != null) originalRect = previewCamera.rect; }
        private void LateUpdate()
        {
            if (previewCamera != null) previewCamera.rect = new Rect(0, 0, 1 - Mathf.Clamp01(350 * UiScale / Mathf.Max(1, Screen.width)), 1);
        }
        private void OnDisable() { if (previewCamera != null) previewCamera.rect = originalRect; }
        private void OnGUI()
        {
            if (sequence == null || sequence.Config == null) return;
            if (heading == null)
            {
                text = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, normal = { textColor = new Color(.92f, .87f, .78f) } };
                heading = new GUIStyle(text) { fontSize = 23, fontStyle = FontStyle.Bold };
                caption = new GUIStyle(text) { fontSize = 12 };
                button = new GUIStyle(GUI.skin.button) { fontSize = 14, fixedHeight = 30 };
            }
            var config = sequence.Config;
            var matrix = GUI.matrix; var color = GUI.color;
            float scale = UiScale; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float x = Screen.width / scale - 350, height = Screen.height / scale;
            GUI.color = new Color(.055f, .045f, .07f); GUI.DrawTexture(new Rect(x, 0, 350, height), Texture2D.whiteTexture); GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(x + 16, 16, 318, height - 32));
            scroll = GUILayout.BeginScrollView(scroll, false, false);
            GUILayout.Label("Flower light sequence", heading);
            GUILayout.Label("Sleep → Ambient → Roulette → Color", caption);
            if (reference != null) GUI.DrawTexture(GUILayoutUtility.GetRect(290, 130), reference, ScaleMode.ScaleToFit);
            string status = sequence.Stage.ToString();
            if (sequence.Stage == FlowerLightingStage.Roulette) status += sequence.WinnerLanded ? " · landed on petal " + (sequence.WinnerSlot + 1) : " · clockwise";
            if (sequence.Stage == FlowerLightingStage.Result) status += " · " + sequence.WinnerPalette;
            GUILayout.Label(status, heading);
            GUILayout.Label(sequence.Stage == FlowerLightingStage.Result ? "Center rim on · full palette look" : "Center rim off", caption);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sleep", button)) { sequence.autoPreview = false; sequence.Sleep(); }
            if (GUILayout.Button("Ambient", button)) { sequence.autoPreview = false; sequence.WakeAmbient(); }
            if (GUILayout.Button("Spin", button)) { sequence.autoPreview = false; sequence.StartRoulette(); }
            GUILayout.EndHorizontal();
            if (GUILayout.Button(sequence.autoPreview ? "Stop automatic sequence" : "Play full sequence", button))
            { if (sequence.autoPreview) sequence.autoPreview = false; else sequence.PlaySequence(); }
            GUILayout.Space(10);
            Slider("Ambient brightness", ref config.ambientBrightness, 0, 8);
            Slider("Ambient drift speed", ref config.ambientSpeed, .05f, 1);
            Slider("Ambient dark gaps", ref config.ambientGapCoverage, 0, 1);
            Slider("Roulette tip brightness", ref config.rouletteBrightness, 1, 20);
            float reachPercent = config.rouletteTipReach * 100;
            Slider("Tip reach down edge (%)", ref reachPercent, 5, 100); config.rouletteTipReach = reachPercent / 100;
            Slider("Roulette icon brightness", ref config.rouletteIconBrightness, 0, 20);
            Slider("Spin duration (seconds)", ref config.rouletteSeconds, 3, 15);
            float turns = config.rouletteTurns; Slider("Full turns", ref turns, 2, 10); config.rouletteTurns = Mathf.RoundToInt(turns);
            GUILayout.Label("Spin timing is locked when each spin starts.", caption);
            GUILayout.Space(10);
            GUILayout.Label("Preview a color result", text);
            for (int row = 0; row < 4; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 2; col++)
                {
                    int slot = row * 2 + col;
                    if (GUILayout.Button(config.outcomes[slot].ToString(), button)) sequence.PreviewResult(slot);
                }
                GUILayout.EndHorizontal();
            }
#if UNITY_EDITOR
            if (GUILayout.Button("Save timing and lighting defaults", button))
            {
                var destination = sequence.preset;
                UnityEditor.Undo.RecordObject(destination, "Save flower sequence defaults");
                destination.sleepingSeconds = config.sleepingSeconds; destination.ambientSeconds = config.ambientSeconds;
                destination.rouletteSeconds = config.rouletteSeconds; destination.rouletteTurns = config.rouletteTurns;
                destination.winnerHoldSeconds = config.winnerHoldSeconds; destination.resultSeconds = config.resultSeconds;
                destination.ambientBrightness = config.ambientBrightness; destination.ambientSpeed = config.ambientSpeed;
                destination.ambientGapCoverage = config.ambientGapCoverage; destination.rouletteBrightness = config.rouletteBrightness;
                destination.rouletteTipReach = config.rouletteTipReach; destination.rouletteIconBrightness = config.rouletteIconBrightness;
                destination.warmColor = config.warmColor;
                UnityEditor.EditorUtility.SetDirty(destination); UnityEditor.AssetDatabase.SaveAssetIfDirty(destination);
                saved = "Saved for the next play session.";
            }
#endif
            GUILayout.Label(saved.Length > 0 ? saved : "Eight equal-probability stops. The selected petal determines the result color.", caption);
            GUILayout.EndScrollView(); GUILayout.EndArea(); GUI.matrix = matrix; GUI.color = color;
        }
        private void Slider(string name, ref float value, float min, float max)
        { GUILayout.Label(name + "   " + value.ToString("0.00"), text); value = GUILayout.HorizontalSlider(value, min, max); }
    }
}
