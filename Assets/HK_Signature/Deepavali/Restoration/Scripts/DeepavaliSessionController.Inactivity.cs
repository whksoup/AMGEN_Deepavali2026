using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Amgen.Deepavali
{
    public sealed partial class DeepavaliSessionController
    {
        [Header("Inactivity")]
        [Tooltip("Seconds without meaningful pointer movement, tracking acquisition or an input action before returning to the dark opening.")]
        [Min(.1f)] public float inactivityTimeout = 30;
        [Tooltip("Total fade-out and fade-in duration for an inactivity return.")]
        [Min(.1f)] public float inactivityFadeDuration = 2;
        public float InactivityElapsed { get; private set; }
        public float InactivityFadeAlpha => inactivityFade != null ? inactivityFade.alpha : 0;

        private Vector2 activityAnchor;
        private bool activityPresent, returningToBase, resetAtBlack;
        private float inactivityFadeElapsed, activeFadeDuration;
        private CanvasGroup inactivityFade;
        private LaunchSceneController inactivityHost;

        /// <summary>Real activity only. Animation, rendering and phase transitions do not refresh the timer.</summary>
        public void RegisterActivity()
        {
            if (!returningToBase) InactivityElapsed = 0;
        }

        private void ObserveActivity()
        {
            if (returningToBase) return;
            if (restoration.HasPointer)
            {
                Vector2 delta = restoration.PointerViewport - activityAnchor;
                delta.x *= restoration.interactionCamera.aspect;
                // Accumulate movement against the last meaningful position, ignoring sub-pixel sensor jitter.
                if (!activityPresent || delta.sqrMagnitude >= .002f * .002f)
                {
                    activityAnchor = restoration.PointerViewport;
                    RegisterActivity();
                }
            }
            activityPresent = restoration.HasPointer;
#if ENABLE_INPUT_SYSTEM
            if (Application.isFocused && ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame ||
                Mouse.current.scroll.ReadValue().sqrMagnitude > 0)))) RegisterActivity();
#else
            if (Application.isFocused && Input.anyKeyDown) RegisterActivity();
#endif
        }

        private bool TickInactivity()
        {
            if (returningToBase)
            {
                inactivityFadeElapsed += Time.unscaledDeltaTime;
                float half = activeFadeDuration * .5f;
                if (!resetAtBlack)
                {
                    inactivityFade.alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(inactivityFadeElapsed / half));
                    if (inactivityFadeElapsed >= half)
                    {
                        float duration = activeFadeDuration;
                        // Change palettes, lamps, scroll playback and motion only while the whole frame is black.
                        ResetSession(false, false);
                        returningToBase = resetAtBlack = true;
                        activeFadeDuration = duration;
                        inactivityFadeElapsed = half;
                        inactivityFade.alpha = 1;
                        inactivityFade.blocksRaycasts = true;
                        Phase = DeepavaliSessionPhase.ReturningToBase;
                    }
                }
                else
                {
                    inactivityFade.alpha = 1 - Mathf.SmoothStep(0, 1,
                        Mathf.Clamp01((inactivityFadeElapsed - half) / half));
                    if (inactivityFadeElapsed >= activeFadeDuration)
                    {
                        ResetInactivity();
                        Phase = DeepavaliSessionPhase.AwaitingRestoration;
                        ShowPrompt(false);
                    }
                }
                return true;
            }

            float timeout = DeepavaliSessionSettings.Safe(inactivityTimeout, 30, .1f);
            InactivityElapsed = Mathf.Min(InactivityElapsed + Time.unscaledDeltaTime, timeout);
            // Already at base: leave the invitation visible instead of fading it every timeout interval.
            if (Phase == DeepavaliSessionPhase.AwaitingRestoration || IsResultPresentation || InactivityElapsed < timeout) return false;
            EnsureInactivityFade();
            returningToBase = true;
            resetAtBlack = false;
            inactivityFadeElapsed = 0;
            activeFadeDuration = DeepavaliSessionSettings.Safe(inactivityFadeDuration, 2, .1f);
            inactivityFade.blocksRaycasts = true;
            restoration.paintingEnabled = false;
            sequence.Paused = true;
            Phase = DeepavaliSessionPhase.ReturningToBase;
            return true;
        }

        private void EnsureInactivityFade()
        {
            if (inactivityFade != null) return;
            var root = new GameObject("Inactivity light fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            inactivityFade = root.GetComponent<CanvasGroup>();
            inactivityFade.alpha = 0;
            inactivityFade.interactable = false;
            var cover = new GameObject("Black fade", typeof(RectTransform), typeof(Image));
            cover.transform.SetParent(root.transform, false);
            var rect = cover.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            cover.GetComponent<Image>().color = Color.black;
        }

        private void ResetInactivity()
        {
            InactivityElapsed = inactivityFadeElapsed = 0;
            returningToBase = resetAtBlack = activityPresent = false;
            activityAnchor = default;
            if (inactivityFade != null) { inactivityFade.alpha = 0; inactivityFade.blocksRaycasts = false; }
        }

        private void BindHostInactivity()
        {
            var host = LaunchSceneController.Instance;
            if (host == inactivityHost) return;
            if (inactivityHost != null && inactivityHost.InactivityOwner == this) inactivityHost.InactivityOwner = null;
            inactivityHost = host;
            if (inactivityHost != null) inactivityHost.InactivityOwner = this;
        }

        private void ReleaseInactivity()
        {
            ResetInactivity();
            if (inactivityHost != null && inactivityHost.InactivityOwner == this) inactivityHost.InactivityOwner = null;
            inactivityHost = null;
            if (inactivityFade != null) Destroy(inactivityFade.gameObject);
            inactivityFade = null;
        }
    }
}
