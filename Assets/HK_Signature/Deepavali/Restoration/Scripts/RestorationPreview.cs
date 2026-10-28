using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Amgen.Deepavali
{
    /// <summary>Optional test-scene controls. Hide with H for a clean wall preview.</summary>
    public sealed class RestorationPreview : MonoBehaviour
    {
        public ScreenRestorationController restoration;
        public PetalWheelLightSequence sequence;
        public bool showControls = true;
        private float Scale => Mathf.Clamp(Screen.height / 900f, .75f, 2);
        private Rect Panel => new Rect((Screen.width / Scale - 360) * .5f, 16, 360, 120);
        private void OnEnable() { if (restoration != null) restoration.BlocksMouseScreenPoint = ContainsScreenPoint; }
        private void OnDisable()
        {
            if (restoration != null && restoration.BlocksMouseScreenPoint == ContainsScreenPoint)
                restoration.BlocksMouseScreenPoint = null;
        }
        private bool ContainsScreenPoint(Vector2 screen) => showControls &&
            Panel.Contains(new Vector2(screen.x / Scale, (Screen.height - screen.y) / Scale));
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return;
            if (Keyboard.current.hKey.wasPressedThisFrame) showControls = !showControls;
            if (Keyboard.current.rKey.wasPressedThisFrame) restoration.ResetRestoration();
            if (Keyboard.current.fKey.wasPressedThisFrame) restoration.CompleteRestoration();
#else
            if (Input.GetKeyDown(KeyCode.H)) showControls = !showControls;
            if (Input.GetKeyDown(KeyCode.R)) restoration.ResetRestoration();
            if (Input.GetKeyDown(KeyCode.F)) restoration.CompleteRestoration();
#endif
        }
        private void OnGUI()
        {
            if (!showControls || restoration == null) return;
            var oldMatrix = GUI.matrix;
            float scale = Scale;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUILayout.BeginArea(Panel, GUI.skin.box);
            GUILayout.Label(restoration.Phase == RestorationPhase.Idle ? "Restored — idle is active" :
                restoration.Phase == RestorationPhase.Completing ? "Bringing the scene to life…" : "Move the pointer to restore light and color");
            float progress = Mathf.Lerp(restoration.Coverage, 1, restoration.CompletionReveal);
            GUILayout.Label("Restored: " + Mathf.RoundToInt(progress * 100) + "%   •   H: hide controls");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restart (R)")) restoration.ResetRestoration();
            if (restoration.Phase != RestorationPhase.Idle)
            {
                if (GUILayout.Button("Finish reveal (F)")) restoration.CompleteRestoration();
            }
            else if (sequence != null && sequence.Pattern != null)
            {
                if (sequence.Pattern.Mode == PetalWheelMode.Shimmer)
                { if (GUILayout.Button("Enter wheel")) sequence.EnterWheel(); }
                else if (GUILayout.Button("Spin")) sequence.Spin();
            }
            GUILayout.EndHorizontal(); GUILayout.EndArea(); GUI.matrix = oldMatrix;
        }
    }
}
