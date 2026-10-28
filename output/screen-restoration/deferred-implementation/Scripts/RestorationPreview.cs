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
        public bool showControls = true;
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) showControls = !showControls;
#else
            if (Input.GetKeyDown(KeyCode.H)) showControls = !showControls;
#endif
        }
        private void OnGUI()
        {
            if (!showControls || restoration == null) return;
            var oldMatrix = GUI.matrix;
            float scale = Mathf.Clamp(Screen.height / 900f, .75f, 2);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUILayout.BeginArea(new Rect(16, 16, 310, 124), GUI.skin.box);
            GUILayout.Label(restoration.Phase == RestorationPhase.Idle ? "Restored — idle is active" :
                restoration.Phase == RestorationPhase.Completing ? "Bringing the scene to life…" : "Move the pointer to restore light and color");
            GUILayout.Label("Restored: " + Mathf.RoundToInt(restoration.Coverage * 100) + "%   •   H: hide controls");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restart")) restoration.ResetRestoration();
            if (GUILayout.Button("Finish reveal")) restoration.CompleteRestoration();
            GUILayout.EndHorizontal(); GUILayout.EndArea(); GUI.matrix = oldMatrix;
        }
    }
}
