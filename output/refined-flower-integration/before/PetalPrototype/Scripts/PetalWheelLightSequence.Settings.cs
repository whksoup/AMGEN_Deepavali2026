using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class PetalWheelLightSequence
    {
        [Header("Saved preview settings")]
        [Tooltip("Loaded on starting Play Mode. GUI adjustments stay temporary until Save to ScriptableObject is pressed.")]
        public PetalWheelSettingsPreset settingsPreset;

        public bool SaveSettingsToPreset(out string message)
        {
#if UNITY_EDITOR
            if (settingsPreset == null || !UnityEditor.AssetDatabase.Contains(settingsPreset))
            {
                message = "Assign a Petal Wheel Settings asset in the Inspector first.";
                return false;
            }
            try
            {
                UnityEditor.Undo.RecordObject(settingsPreset, "Save petal wheel settings");
                settingsPreset.CaptureFrom(this);
                UnityEditor.EditorUtility.SetDirty(settingsPreset);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(settingsPreset);
                message = "Saved to " + settingsPreset.name + ". Loads next play session.";
                return true;
            }
            catch (System.Exception exception)
            {
                message = "Could not save settings: " + exception.Message;
                Debug.LogException(exception, this);
                return false;
            }
#else
            message = "Saving a project asset requires the Unity Editor.";
            return false;
#endif
        }
    }
}
