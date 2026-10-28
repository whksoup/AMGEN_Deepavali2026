using UnityEngine;

namespace Amgen.Deepavali
{
    public enum DeepavaliSessionPhase
    {
        AwaitingRestoration, DismissingRestorationPrompt, Restoring, FinalReveal,
        AwaitingSpin, DismissingSpinPrompt, EnteringWheel, Spinning, RevealingResult, HoldingResult,
        ScrollDelay, ScrollOpening, ScrollHolding, ScrollClosing, ReturningToBase
    }

    /// <summary>Owns the main wall's repeatable session. All input comes through restoration's XY/presence route.</summary>
    [DefaultExecutionOrder(600), DisallowMultipleComponent]
    public sealed partial class DeepavaliSessionController : MonoBehaviour
    {
        public ScreenRestorationController restoration;
        public RestorationLampResponse lamps;
        public HandHereInteraction handPrompt;
        public PetalWheelLightSequence sequence;
        public FlowerPaletteController flower;
        public Light centerLight;

        [Header("Session")]
        [Min(.01f)] public float openingDwellDuration = .65f;
        [Min(.01f)] public float spinDwellDuration = .15f;
        [Range(.1f, 1)] public float requiredCoverage = .8f;
        [Tooltip("Fallback result hold when no scroll is assigned or playback fails. Normal scroll timing is below.")]
        [Min(0)] public float coloredHoldDuration = 10;
        [Header("Opening center reveal")]
        [Range(.02f, .4f)] public float centerRevealRadius = .16f;
        [Min(0)] public float centerLightIntensity = .6f;
        [Min(.01f)] public float centerLightFadeDuration = .6f;

        public DeepavaliSessionPhase Phase { get; private set; }
        public float DwellProgress { get; private set; }
        public float ColoredHoldElapsed { get; private set; }
        public bool PromptArmed { get; private set; }

        private bool initialized;
        private float dwellTime, centerLightLevel;
        private int pointerVersion;

        private void OnEnable()
        {
            if (handPrompt != null) handPrompt.Despawned += PromptDismissed;
            if (initialized) ResetSession(false);
        }

        private void Start()
        {
            if (settingsPreset != null) settingsPreset.ApplyTo(this);
            if (restoration == null || lamps == null || handPrompt == null || sequence == null || flower == null ||
                lamps.background == null || lamps.background.lamps.Length != 4)
            {
                Debug.LogError("The wall session needs restoration, four candles, the center prompt, wheel and flower.", this);
                enabled = false;
                return;
            }
            initialized = true;
            if (!sequence.timing.bloom)
            { Debug.LogError("The integrated session requires result bloom to be enabled.", this); enabled = false; return; }
            // The first visitor may already be at center. Every subsequent prompt requires a fresh entry.
            ResetSession(true);
        }

        private void Update()
        {
            if (!initialized) return;
            ObserveActivity();
            if (Phase == DeepavaliSessionPhase.ReturningToBase) return;
            if (Phase != DeepavaliSessionPhase.AwaitingRestoration && Phase != DeepavaliSessionPhase.AwaitingSpin) return;
            if (pointerVersion != restoration.PointerContinuityVersion)
            {
                pointerVersion = restoration.PointerContinuityVersion;
                ResetDwell();
            }
            var rect = restoration.interactionCamera.pixelRect;
            var uv = restoration.PointerViewport;
            bool inside = restoration.HasPointer && handPrompt.ContainsScreenPoint(
                new Vector2(rect.x + uv.x * rect.width, rect.y + uv.y * rect.height));
            if (!inside)
            {
                ResetDwell();
                // Tracking loss cancels dwell; only a known point outside the badge rearms a consumed prompt.
                if (restoration.HasPointer) PromptArmed = true;
                return;
            }
            if (handPrompt.IsAppearing) { ResetDwell(); return; }
            if (!PromptArmed) return;
            dwellTime += Time.unscaledDeltaTime;
            float duration = Phase == DeepavaliSessionPhase.AwaitingRestoration ? openingDwellDuration : spinDwellDuration;
            DwellProgress = Mathf.Clamp01(dwellTime / Mathf.Max(.01f, duration));
            handPrompt.SetDwellProgress(DwellProgress);
            if (DwellProgress < 1) return;
            PromptArmed = false;
            Phase = Phase == DeepavaliSessionPhase.AwaitingRestoration
                ? DeepavaliSessionPhase.DismissingRestorationPrompt : DeepavaliSessionPhase.DismissingSpinPrompt;
            handPrompt.BeginDisintegration();
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            BindHostInactivity();
            if (TickInactivity()) return;
            // Observe the lamps after their LateUpdate and the result after the wheel has applied its material state.
            switch (Phase)
            {
                case DeepavaliSessionPhase.Restoring:
                    if (lamps.LitLampCount == 4 && restoration.Coverage >= requiredCoverage)
                    {
                        restoration.paintingEnabled = false;
                        Phase = DeepavaliSessionPhase.FinalReveal;
                        restoration.CompleteRestoration();
                    }
                    break;
                case DeepavaliSessionPhase.FinalReveal:
                    if (restoration.Phase == RestorationPhase.Idle && sequence.Pattern != null && sequence.isActiveAndEnabled)
                    {
                        sequence.HoldResult = true;
                        Phase = DeepavaliSessionPhase.AwaitingSpin;
                        ShowPrompt(false);
                    }
                    break;
                case DeepavaliSessionPhase.EnteringWheel:
                    if (sequence.Pattern != null && sequence.Pattern.Mode == PetalWheelMode.Ready && sequence.Spin())
                        Phase = DeepavaliSessionPhase.Spinning;
                    break;
                case DeepavaliSessionPhase.Spinning:
                    if (sequence.Pattern != null && sequence.Pattern.Mode == PetalWheelMode.Win)
                    {
                        Phase = DeepavaliSessionPhase.RevealingResult;
                        PrepareSelectedScroll();
                    }
                    break;
                case DeepavaliSessionPhase.RevealingResult:
                    if (sequence.IsResultFullyVisible)
                        BeginScrollDelay();
                    else if (Time.realtimeSinceStartupAsDouble - revealStarted >
                        DeepavaliSessionSettings.Safe(sequence.timing.bloomDelay, 1.4f) + DeepavaliSessionSettings.Safe(sequence.timing.bloomSpread, 1.8f) + 10)
                    { Debug.LogError("Result reveal did not complete; resetting the session.", this); endSequence(); }
                    break;
                case DeepavaliSessionPhase.HoldingResult:
                    ColoredHoldElapsed += Time.unscaledDeltaTime;
                    if (ColoredHoldElapsed >= DeepavaliSessionSettings.Safe(coloredHoldDuration, 10)) endSequence();
                    break;
                case DeepavaliSessionPhase.ScrollDelay:
                case DeepavaliSessionPhase.ScrollOpening:
                case DeepavaliSessionPhase.ScrollHolding:
                case DeepavaliSessionPhase.ScrollClosing:
                    TickScroll();
                    break;
            }
            bool illuminateCenter = Phase == DeepavaliSessionPhase.Restoring || Phase == DeepavaliSessionPhase.FinalReveal;
            centerLightLevel = Mathf.MoveTowards(centerLightLevel, illuminateCenter ? 1 : 0,
                Time.unscaledDeltaTime / Mathf.Max(.01f, centerLightFadeDuration));
            ApplyCenterLight();
        }

        private void PromptDismissed()
        {
            if (!isActiveAndEnabled || !initialized) return;
            ResetDwell();
            if (Phase == DeepavaliSessionPhase.DismissingRestorationPrompt)
            {
                restoration.ClearPointer();
                restoration.RevealPoint(new Vector2(.5f, .5f), centerRevealRadius);
                restoration.paintingEnabled = true;
                Phase = DeepavaliSessionPhase.Restoring;
            }
            else if (Phase == DeepavaliSessionPhase.DismissingSpinPrompt && sequence.EnterWheel())
                Phase = DeepavaliSessionPhase.EnteringWheel;
        }

        /// <summary>External reset and normal session end share the same synchronous, cancellable reset path.</summary>
        [ContextMenu("End Sequence (Play Mode)")]
        public void endSequence()
        {
            if (Application.isPlaying && initialized) ResetSession(false);
        }

        public void EndSequence() => endSequence();

        private void ResetSession(bool allowInitialHover, bool showPrompt = true)
        {
            ResetInactivity();
            CancelScroll();
            handPrompt.CancelAndHide();
            handPrompt.externallyDriven = true;
            sequence.autoPreview = false;
            sequence.HoldResult = true;
            sequence.Paused = false;
            restoration.autoComplete = false;
            restoration.paintingEnabled = false;
            restoration.ResetRestoration();
            flower.ResetToInitialState();
            // Shimmer is the image beneath the restoration mask, including during partial restoration.
            sequence.enabled = true;
            sequence.ResetPreview();
            centerLightLevel = 0;
            ApplyCenterLight();
            ColoredHoldElapsed = 0;
            Phase = DeepavaliSessionPhase.AwaitingRestoration;
            if (showPrompt) ShowPrompt(allowInitialHover);
        }

        private void ShowPrompt(bool armed)
        {
            var rect = handPrompt.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            handPrompt.ResetPrompt();
            ResetDwell();
            pointerVersion = restoration.PointerContinuityVersion;
            PromptArmed = armed;
        }

        private void ResetDwell()
        {
            dwellTime = DwellProgress = 0;
            handPrompt.SetDwellProgress(0);
        }

        private void ApplyCenterLight()
        {
            if (centerLight == null) return;
            centerLight.intensity = centerLightIntensity * Mathf.SmoothStep(0, 1, centerLightLevel);
            centerLight.enabled = centerLightLevel > 0;
        }

        private void OnDisable()
        {
            ReleaseInactivity();
            CancelScroll();
            if (handPrompt != null) handPrompt.Despawned -= PromptDismissed;
            if (!initialized) return;
            // Scene teardown can destroy references in any order. Do not respawn UI or reset destroyed owners.
            if (handPrompt != null) handPrompt.CancelAndHide();
            if (restoration != null && restoration.isActiveAndEnabled)
            {
                restoration.paintingEnabled = false;
                restoration.ResetRestoration();
            }
            if (sequence != null && flower != null) { sequence.Paused = false; sequence.ResetPreview(); sequence.HoldResult = false; }
            if (flower != null) flower.ResetToInitialState();
            centerLightLevel = 0; ApplyCenterLight();
            dwellTime = DwellProgress = ColoredHoldElapsed = 0;
            PromptArmed = false;
        }
    }
}
