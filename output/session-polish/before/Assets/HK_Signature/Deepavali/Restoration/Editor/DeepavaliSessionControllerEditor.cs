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
            EditorGUILayout.HelpBox("Scroll hold starts fully open. Runtime adjustments stay temporary until saved. The settings asset loads on Play.", MessageType.Info);
            using (new EditorGUI.DisabledScope(session.settingsPreset == null))
            {
                if (GUILayout.Button("Save to ScriptableObject")) session.SaveSettingsToPreset(out message);
                if (GUILayout.Button("Load saved settings"))
                {
                    Undo.RecordObject(session, "Load session settings");
                    session.settingsPreset.ApplyTo(session); EditorUtility.SetDirty(session);
                }
            }
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Phase", session.Phase.ToString());
                EditorGUILayout.LabelField("Scroll", session.SelectedScrollVariant ?? "None");
                EditorGUILayout.LabelField("Phase seconds", session.ScrollPhaseElapsed.ToString("F2"));
                if (GUILayout.Button("End sequence / reset")) session.endSequence();
            }
        }
    }
}
