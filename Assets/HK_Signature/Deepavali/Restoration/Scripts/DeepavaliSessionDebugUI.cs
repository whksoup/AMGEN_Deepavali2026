using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Amgen.Deepavali
{
    /// <summary>Optional session controls. M toggles the panel without changing the camera viewport.</summary>
    [DefaultExecutionOrder(-600), DisallowMultipleComponent]
    public sealed class DeepavaliSessionDebugUI : MonoBehaviour
    {
        public DeepavaliSessionController session;
        public bool showControls;

        private ScreenRestorationController blockedRestoration;
        private DeepavaliTouchInput touchInput;
        private Func<Vector2, bool> previousBlocker;
        private Vector2 scroll, previousMouse;
        private bool hadMouse;
        private string message;
        private GUIStyle label, heading, button;
        private float Scale => Mathf.Clamp(Screen.height / 900f, .65f, 1.8f);
        private Rect Panel => new Rect(Mathf.Max(12, Screen.width / Scale - 432), 16,
            Mathf.Min(416, Screen.width / Scale - 24), Mathf.Max(100, Screen.height / Scale - 32));

        private void OnEnable()
        {
            if (session == null) session = GetComponent<DeepavaliSessionController>();
            if (session == null || session.restoration == null) return;
            touchInput = session.restoration.GetComponent<DeepavaliTouchInput>();
            blockedRestoration = session.restoration;
            previousBlocker = blockedRestoration.BlocksMouseScreenPoint;
            blockedRestoration.BlocksMouseScreenPoint = BlocksScreenPoint;
            hadMouse = false;
        }

        private void OnDisable()
        {
            if (blockedRestoration != null && blockedRestoration.BlocksMouseScreenPoint == BlocksScreenPoint)
                blockedRestoration.BlocksMouseScreenPoint = previousBlocker;
            blockedRestoration = null;
            previousBlocker = null;
            hadMouse = false;
        }

        public bool ContainsScreenPoint(Vector2 point) => showControls && isActiveAndEnabled &&
            Panel.Contains(new Vector2(point.x / Scale, (Screen.height - point.y) / Scale));

        private bool BlocksScreenPoint(Vector2 point) => ContainsScreenPoint(point) || (previousBlocker?.Invoke(point) ?? false);

        private void Update()
        {
            if (session == null || !Application.isFocused) { hadMouse = false; return; }
            bool toggle;
            bool mousePresent;
            Vector2 mousePosition;
#if ENABLE_INPUT_SYSTEM
            toggle = Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
            mousePresent = Mouse.current != null;
            mousePosition = mousePresent ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            toggle = Input.GetKeyDown(KeyCode.M);
            mousePresent = Input.mousePresent;
            mousePosition = Input.mousePosition;
#endif
            if (toggle)
            {
                showControls = !showControls;
                session.RegisterActivity();
            }
            // Hover movement over the panel counts as activity, but an open, untouched panel does not.
            if (mousePresent && hadMouse && ContainsScreenPoint(mousePosition) &&
                (mousePosition - previousMouse).sqrMagnitude >= 1)
                session.RegisterActivity();
            previousMouse = mousePosition;
            hadMouse = mousePresent;
        }

        private void OnGUI()
        {
            if (!showControls || session == null) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                heading = new GUIStyle(label) { fontSize = 20, fontStyle = FontStyle.Bold };
                button = new GUIStyle(GUI.skin.button) { fontSize = 14, fixedHeight = 30 };
            }
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            bool oldEnabled = GUI.enabled;
            try
            {
                float scale = Scale;
                // Read the event in its original top-left screen coordinates before scaling the GUI.
                var evt = Event.current;
                bool overPanel = Panel.Contains(evt.mousePosition / scale);
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
                if (overPanel && (evt.type == EventType.MouseDown ||
                    evt.type == EventType.MouseDrag || evt.type == EventType.ScrollWheel || evt.type == EventType.KeyDown))
                    session.RegisterActivity();
                GUI.color = new Color(.06f, .05f, .08f, .97f);
                GUI.DrawTexture(Panel, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUILayout.BeginArea(new Rect(Panel.x + 14, Panel.y + 12, Panel.width - 28, Panel.height - 24));
                GUILayout.Label("Deepavali session   ·   M to close", heading);
                GUILayout.Label(session.Phase.ToString(), label);
                if (touchInput != null && touchInput.isActiveAndEnabled)
                {
                    GUILayout.Label(touchInput.mousePreview ? "Input: mouse preview" :
                        touchInput.HasSensorContact ? "Input: sensor contact " + touchInput.ContactId : "Input: sensor · waiting for touch", label);
                    bool preview = GUILayout.Toggle(touchInput.mousePreview, "Mouse preview (temporary)");
                    if (preview != touchInput.mousePreview)
                    { touchInput.mousePreview = preview; session.RegisterActivity(); }
                }
                if (session.restoration != null)
                    GUILayout.Label("Coverage " + (session.restoration.Coverage * 100).ToString("0") + "%   ·   Candles " +
                        (session.lamps != null ? session.lamps.LitLampCount.ToString() : "—") + "/4", label);
                GUILayout.Label("Inactive " + session.InactivityElapsed.ToString("0.0") + " / " +
                    session.inactivityTimeout.ToString("0.0") + " seconds", label);
                GUILayout.BeginHorizontal();
#if UNITY_EDITOR
                GUI.enabled = session.settingsPreset != null;
                if (GUILayout.Button("Save to ScriptableObject", button))
                { session.SaveSettingsToPreset(out message); session.RegisterActivity(); }
#endif
                GUI.enabled = session.settingsPreset != null;
                if (GUILayout.Button("Load saved", button))
                {
                    session.settingsPreset.ApplyTo(session);
                    message = "Loaded " + session.settingsPreset.name + ".";
                    session.RegisterActivity();
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(message)) GUILayout.Label(message, label);
#if UNITY_EDITOR
                GUILayout.Label("Changes remain temporary until saved. Saved values load on Play.", label);
#else
                GUILayout.Label("Runtime adjustments are temporary. Project settings are saved in the Unity Editor.", label);
#endif
                scroll = GUILayout.BeginScrollView(scroll);
                GUILayout.Space(8);
                if (session.restoration != null)
                    Slider("Restoration brush radius (viewport height)", ref session.restoration.brushRadius, .02f, .4f);
                Slider("Idle time before return (seconds)", ref session.inactivityTimeout, 1, 180);
                Slider("Return fade duration (seconds)", ref session.inactivityFadeDuration, .1f, 8);
                Slider("Opening dwell (seconds)", ref session.openingDwellDuration, .1f, 4);
                Slider("Spin dwell (seconds)", ref session.spinDwellDuration, .05f, 3);
                Slider("Required restored coverage", ref session.requiredCoverage, .1f, 1);
                Slider("Fallback colored hold (seconds)", ref session.coloredHoldDuration, 0, 60);
                Slider("Center reveal radius", ref session.centerRevealRadius, .02f, .4f);
                Slider("Center light intensity", ref session.centerLightIntensity, 0, 2);
                Slider("Center light fade (seconds)", ref session.centerLightFadeDuration, .05f, 4);
                Slider("Scroll delay after result (seconds)", ref session.scrollDelayAfterResultSeconds, 0, 15);
                Slider("Fully open scroll hold (seconds)", ref session.scrollHoldSeconds, 0, 60);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
                GUI.enabled = oldEnabled;
            }
        }

        private void Slider(string text, ref float value, float min, float max)
        {
            GUILayout.Label(text + "   " + value.ToString("0.00"), label);
            // Showing the panel must not clamp a saved value or manufacture user activity.
            if (float.IsFinite(value)) { min = Mathf.Min(min, value); max = Mathf.Max(max, value); }
            float next = GUILayout.HorizontalSlider(value, min, max);
            if (next == value) return;
            value = next;
            session.RegisterActivity();
        }
    }
}
