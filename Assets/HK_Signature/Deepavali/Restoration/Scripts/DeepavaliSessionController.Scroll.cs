using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class DeepavaliSessionController
    {
        [Header("Saved session settings")]
        public DeepavaliSessionSettings settingsPreset;
        [Header("Result scroll")]
        public DiwaliScrollOverlay scrollOverlay;
        [Min(0)] public float scrollDelayAfterResultSeconds = 2;
        [Tooltip("Time fully open, excluding opening and closing clips.")]
        [Min(0)] public float scrollHoldSeconds = 15;
        public float ScrollPhaseElapsed { get; private set; }
        public string SelectedScrollVariant { get; private set; }
        private double scrollPhaseStarted, revealStarted;
        private float scrollPhaseDuration;
        private bool scrollFailed;

        private void PrepareSelectedScroll()
        {
            revealStarted = Time.realtimeSinceStartupAsDouble;
            scrollFailed = false;
            SelectedScrollVariant = sequence.SelectedOutcome?.scrollVariant;
            if (scrollOverlay == null) return;
            scrollOverlay.showOnStart = scrollOverlay.toggleOnMouseClick = scrollOverlay.waitForIdleLoopEnd = false;
            scrollOverlay.CancelImmediate();
            scrollFailed = string.IsNullOrEmpty(SelectedScrollVariant) || !scrollOverlay.PrepareVariant(SelectedScrollVariant);
        }
        private void BeginScrollDelay()
        {
            ColoredHoldElapsed = 0;
            if (scrollOverlay == null || scrollFailed || !string.IsNullOrEmpty(scrollOverlay.LastError))
            { Phase = DeepavaliSessionPhase.HoldingResult; return; }
            SetScrollPhase(DeepavaliSessionPhase.ScrollDelay,
                DeepavaliSessionSettings.Safe(scrollDelayAfterResultSeconds, 2));
        }
        private void SetScrollPhase(DeepavaliSessionPhase phase, float duration = 0)
        {
            Phase = phase; scrollPhaseStarted = Time.realtimeSinceStartupAsDouble;
            ScrollPhaseElapsed = 0; scrollPhaseDuration = duration;
        }
        private void TickScroll()
        {
            ScrollPhaseElapsed = (float)(Time.realtimeSinceStartupAsDouble - scrollPhaseStarted);
            bool timedOut = scrollOverlay != null && Phase == DeepavaliSessionPhase.ScrollDelay &&
                ScrollPhaseElapsed > scrollPhaseDuration + scrollOverlay.prepareTimeoutSeconds + 1;
            if (scrollOverlay == null || !scrollOverlay.isActiveAndEnabled || !string.IsNullOrEmpty(scrollOverlay.LastError) || timedOut ||
                ((Phase == DeepavaliSessionPhase.ScrollOpening || Phase == DeepavaliSessionPhase.ScrollHolding) &&
                scrollOverlay.CurrentState == DiwaliScrollOverlay.State.Hidden))
            {
                if (scrollOverlay != null) scrollOverlay.CancelImmediate();
                ColoredHoldElapsed = 0; Phase = DeepavaliSessionPhase.HoldingResult;
                return;
            }
            switch (Phase)
            {
                case DeepavaliSessionPhase.ScrollDelay:
                    if (ScrollPhaseElapsed >= scrollPhaseDuration && scrollOverlay.IsPrepared)
                    { SetScrollPhase(DeepavaliSessionPhase.ScrollOpening); scrollOverlay.Show(SelectedScrollVariant); }
                    break;
                case DeepavaliSessionPhase.ScrollOpening:
                    if (scrollOverlay.CurrentState == DiwaliScrollOverlay.State.Shown)
                        SetScrollPhase(DeepavaliSessionPhase.ScrollHolding, DeepavaliSessionSettings.Safe(scrollHoldSeconds, 15));
                    break;
                case DeepavaliSessionPhase.ScrollHolding:
                    if (ScrollPhaseElapsed >= scrollPhaseDuration)
                    { SetScrollPhase(DeepavaliSessionPhase.ScrollClosing); scrollOverlay.Hide(); }
                    break;
                case DeepavaliSessionPhase.ScrollClosing:
                    if (scrollOverlay.CurrentState == DiwaliScrollOverlay.State.Hidden) BeginGreeting();
                    break;
            }
        }
        private void CancelScroll()
        {
            HideResultLogo();
            if (scrollOverlay != null) scrollOverlay.CancelImmediate();
            SelectedScrollVariant = null; ScrollPhaseElapsed = 0; scrollFailed = false;
        }
        public bool SaveSettingsToPreset(out string message)
        {
#if UNITY_EDITOR
            if (settingsPreset == null || !UnityEditor.AssetDatabase.Contains(settingsPreset))
            { message = "Assign a saved Deepavali Session Settings asset first."; return false; }
            UnityEditor.Undo.RecordObject(settingsPreset, "Save Deepavali session settings");
            settingsPreset.CaptureFrom(this);
            UnityEditor.EditorUtility.SetDirty(settingsPreset);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(settingsPreset);
            message = "Saved to " + settingsPreset.name + ". Loads next Play session.";
            return true;
#else
            message = "Saving project assets requires the Unity Editor."; return false;
#endif
        }
    }
}
