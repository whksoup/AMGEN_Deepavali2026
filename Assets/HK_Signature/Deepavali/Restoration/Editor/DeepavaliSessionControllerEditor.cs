using UnityEditor;
using UnityEngine;

namespace Amgen.Deepavali.Editor
{
    [CustomEditor(typeof(DeepavaliSessionController))]
    public sealed class DeepavaliSessionControllerEditor : UnityEditor.Editor
    {
        private string message;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var session = (DeepavaliSessionController)target;
            EditorGUILayout.HelpBox("Press M in Game view for session controls. Brush size, inactivity and session timings save together. Runtime changes stay temporary until saved; the settings asset loads on Play. Scroll hold starts fully open.", MessageType.Info);
            if (session.restoration != null)
            {
                EditorGUI.BeginChangeCheck();
                float radius = EditorGUILayout.Slider("Restoration brush radius", session.restoration.brushRadius, .02f, .4f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(session.restoration, "Adjust restoration brush");
                    session.restoration.brushRadius = radius;
                    EditorUtility.SetDirty(session.restoration);
                }
            }
            using (new EditorGUI.DisabledScope(session.settingsPreset == null))
            {
                if (GUILayout.Button("Save to ScriptableObject")) session.SaveSettingsToPreset(out message);
                if (GUILayout.Button("Load saved settings"))
                {
                    Undo.RecordObject(session, "Load session settings");
                    if (session.restoration != null) Undo.RecordObject(session.restoration, "Load restoration brush settings");
                    session.settingsPreset.ApplyTo(session); EditorUtility.SetDirty(session);
                    if (session.restoration != null) EditorUtility.SetDirty(session.restoration);
                }
            }
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Phase", session.Phase.ToString());
                EditorGUILayout.LabelField("Scroll", session.SelectedScrollVariant ?? "None");
                EditorGUILayout.LabelField("Phase seconds", session.ScrollPhaseElapsed.ToString("F2"));
                EditorGUILayout.LabelField("Inactive seconds", session.InactivityElapsed.ToString("F2"));
                if (GUILayout.Button("End sequence / reset")) session.endSequence();
            }
        }
    }
}
