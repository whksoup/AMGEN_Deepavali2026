using UnityEditor;
using UnityEngine;

namespace Amgen.Deepavali.Editor
{
    [CustomEditor(typeof(PetalArtworkController))]
    public sealed class PetalArtworkControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var control = (PetalArtworkController)target;
            int index = control.PetalIndex;
            var flower = serializedObject.FindProperty("flower").objectReferenceValue as FlowerPaletteController;
            if (flower == null || index < 0)
            {
                EditorGUILayout.HelpBox("Assign the flower and this petal's renderer to control its artwork.", MessageType.Info);
                return;
            }

            // Edit the owner's existing entry: one source of truth, no duplicated artwork state.
            var owner = new SerializedObject(flower);
            var entries = owner.FindProperty("petalAppearances");
            if (index >= entries.arraySize) return;
            var entry = entries.GetArrayElementAtIndex(index);
            var symbol = entry.FindPropertyRelative("symbol");
            int count = flower.SymbolSet == null ? 0 : flower.SymbolSet.Count;
            var names = new string[count + 1];
            names[0] = "Original ornament";
            for (int i = 0; i < count; i++)
                names[i + 1] = flower.SymbolSet.TryGet(i, out var artwork) ? artwork.label : "Invalid artwork " + i;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Petal " + (index + 1).ToString("00") + " artwork", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup("Artwork", Mathf.Clamp(symbol.intValue + 1, 0, count), names);
            if (EditorGUI.EndChangeCheck()) symbol.intValue = selected - 1;
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("artworkDuration"), new GUIContent("Reveal duration"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("artworkSoftness"), new GUIContent("Reveal softness"));
            owner.ApplyModifiedProperties();
            EditorGUILayout.HelpBox(Application.isPlaying
                ? "Changes reveal from root to tip. The active reveal finishes before the latest choice starts."
                : "Artwork is the starting appearance. Duration and softness control later root-to-tip reveals.", MessageType.None);

            if (!Application.isPlaying) return;
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.ProgressBar(rect, control.Progress, control.IsTransitioning
                ? (control.HasPendingArtwork ? "Revealing — latest choice queued" : "Revealing artwork") : "Artwork ready");
            using (new EditorGUI.DisabledScope(!control.isActiveAndEnabled))
                if (GUILayout.Button("Apply selected artwork immediately")) control.SetArtworkImmediate(symbol.intValue);
            if (control.IsTransitioning) Repaint();
        }
    }
}
