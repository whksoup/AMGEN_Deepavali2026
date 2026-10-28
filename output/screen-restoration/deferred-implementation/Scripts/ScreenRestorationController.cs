using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Amgen.Deepavali
{
    public enum RestorationPhase { Restoring, Completing, Idle }

    [DefaultExecutionOrder(-500), DisallowMultipleComponent]
    public sealed class ScreenRestorationController : MonoBehaviour
    {
        public Camera interactionCamera;
        public Shader compositeShader;
        [Header("Opening look")]
        [Range(0, 1)] public float darkBrightness = .24f;
        [Range(0, 1)] public float darkSaturation;
        [Header("Hover brush")]
        public bool useMouseInput = true;
        [Tooltip("Radius as a fraction of viewport height; remains circular on a wide wall.")]
        [Range(.02f, .4f)] public float brushRadius = .16f;
        [Range(.01f, 1)] public float brushSoftness = .65f;
        [Min(.01f)] public float secondsToRestore = .38f;
        [Range(64, 1024)] public int maskWidth = 384;
        [Tooltip("External input must be refreshed while a hand is present. Stale samples stop painting.")]
        [Min(.01f)] public float inputTimeout = .25f;
        [Tooltip("Break a stroke across large tracking jumps, measured in viewport heights.")]
        [Min(.01f)] public float maxStrokeJump = .6f;
        [Header("Completion")]
        public Rect completionArea = new Rect(.025f, .04f, .95f, .92f);
        [Range(.1f, 1)] public float completionCoverage = .72f;
        [Min(.01f)] public float completionDuration = 1.8f;
        [Tooltip("Idle motion, wheel sequence and petal input to pause until restoration finishes.")]
        public Behaviour[] idleBehaviours = Array.Empty<Behaviour>();
        public UnityEvent onRestorationStarted = new UnityEvent();
        public UnityEvent onRestored = new UnityEvent();
        public RestorationPhase Phase { get; private set; }
        public float Coverage => mask?.Coverage ?? 0;
        public float CompletionReveal { get; private set; }
        public Texture2D MaskTexture => maskTexture;
        public bool HasPointer { get; private set; }
        public Vector2 PointerViewport { get; private set; }
        public event Action Completed;

        private RestorationMask mask;
        private Texture2D maskTexture;
        private Material composite;
        private RestorationRenderPass renderPass;
        private bool[] originalEnabled;
        private bool previousPresent, externalPresent;
        private Vector2 previousPoint, externalPoint;
        private double sampleTime;
        private float completionTime;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (interactionCamera == null) interactionCamera = Camera.main;
            if (interactionCamera == null || compositeShader == null || !compositeShader.isSupported)
            { Debug.LogError("Restoration needs a camera and the Screen Restoration shader.", this); enabled = false; return; }
            float aspect = Mathf.Max(.1f, interactionCamera.pixelRect.width / Mathf.Max(1, interactionCamera.pixelRect.height));
            int width = Mathf.Clamp(maskWidth, 64, 1024), height = Mathf.Clamp(Mathf.RoundToInt(width / aspect), 32, 1024);
            mask = new RestorationMask(width, height, completionArea);
            maskTexture = new Texture2D(width, height, TextureFormat.R8, false, true)
                { name = "Restoration Coverage (Runtime)", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            composite = new Material(compositeShader) { name = "Restoration (Runtime)", hideFlags = HideFlags.DontSave };
            composite.SetTexture("_RestorationMask", maskTexture);
            renderPass = new RestorationRenderPass(composite);
            originalEnabled = new bool[idleBehaviours.Length];
            for (int i = 0; i < idleBehaviours.Length; i++)
                originalEnabled[i] = idleBehaviours[i] != null && idleBehaviours[i].enabled;
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
            ResetRestoration();
        }

        private void BeforeCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera != interactionCamera || Phase == RestorationPhase.Idle || composite == null) return;
            composite.SetFloat("_DarkBrightness", darkBrightness);
            composite.SetFloat("_DarkSaturation", darkSaturation);
            composite.SetFloat("_CompleteReveal", CompletionReveal);
            camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(renderPass);
        }

        /// <summary>Sensor adapter: normalized bottom-left viewport coordinates, plus tracking presence.</summary>
        public void SetPointerViewport(Vector2 viewport, bool present = true)
        {
            externalPoint = viewport; externalPresent = present && RestorationMask.IsValidPoint(viewport);
            sampleTime = Time.unscaledTimeAsDouble;
            // Presence gaps between frames must also break the interpolated stroke.
            if (!externalPresent) previousPresent = false;
        }

        /// <summary>Screen pixels, bottom-left origin. Uses the camera pixel rect, including its offset.</summary>
        public void SetPointerScreen(Vector2 screenPoint, bool present = true)
        {
            if (interactionCamera == null) return;
            var rect = interactionCamera.pixelRect;
            SetPointerViewport(new Vector2((screenPoint.x - rect.x) / Mathf.Max(1, rect.width),
                (screenPoint.y - rect.y) / Mathf.Max(1, rect.height)), present && rect.Contains(screenPoint));
        }

        private void Update()
        {
            if (mask == null) return;
            bool present = false; Vector2 point = externalPoint;
            if (useMouseInput)
            {
                Vector2 screen = default;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null) { screen = Mouse.current.position.ReadValue(); present = Application.isFocused; }
#else
                screen = Input.mousePosition; present = Application.isFocused;
#endif
                var rect = interactionCamera.pixelRect;
                present &= rect.Contains(screen);
                point = new Vector2((screen.x - rect.x) / Mathf.Max(1, rect.width), (screen.y - rect.y) / Mathf.Max(1, rect.height));
            }
            else present = externalPresent && Time.unscaledTimeAsDouble - sampleTime <= inputTimeout;
            Advance(Time.unscaledDeltaTime, point, present);
        }

        /// <summary>One simulation step; also usable by deterministic validation or a custom input driver.</summary>
        public void Advance(float deltaTime, Vector2 viewport, bool present)
        {
            if (mask == null || !isActiveAndEnabled || !float.IsFinite(deltaTime) || deltaTime < 0) return;
            HasPointer = present && RestorationMask.IsValidPoint(viewport);
            PointerViewport = viewport;
            if (Phase == RestorationPhase.Idle) return;
            if (Phase == RestorationPhase.Completing)
            {
                completionTime += deltaTime;
                CompletionReveal = Mathf.SmoothStep(0, 1, Mathf.Clamp01(completionTime / Mathf.Max(.01f, completionDuration)));
                if (CompletionReveal >= 1)
                {
                    Phase = RestorationPhase.Idle; SetIdleEnabled(true);
                    onRestored.Invoke(); Completed?.Invoke();
                }
                return;
            }
            if (HasPointer && deltaTime > 0)
            {
                float aspect = interactionCamera.pixelRect.width / Mathf.Max(1, interactionCamera.pixelRect.height);
                Vector2 diff = viewport - previousPoint; diff.x *= aspect;
                Vector2 from = previousPresent && diff.magnitude <= maxStrokeJump ? previousPoint : viewport;
                mask.Paint(from, viewport, brushRadius, brushSoftness,
                    Mathf.Min(deltaTime, .1f) / Mathf.Max(.01f, secondsToRestore), aspect);
                UploadMask();
                if (Coverage >= completionCoverage) CompleteRestoration();
            }
            previousPresent = HasPointer; previousPoint = viewport;
        }

        public float SampleRestoration(Vector2 viewport) => Mathf.Lerp(mask?.Sample(viewport) ?? 0, 1, CompletionReveal);

        [ContextMenu("Restart Restoration (Play Mode)")]
        public void ResetRestoration()
        {
            if (mask == null) return;
            Phase = RestorationPhase.Restoring; CompletionReveal = completionTime = 0;
            previousPresent = externalPresent = HasPointer = false;
            mask.Clear(); UploadMask(); SetIdleEnabled(false); onRestorationStarted.Invoke();
        }

        [ContextMenu("Complete Restoration (Play Mode)")]
        public void CompleteRestoration()
        {
            if (mask == null || Phase != RestorationPhase.Restoring) return;
            Phase = RestorationPhase.Completing; completionTime = 0; previousPresent = false;
        }

        private void UploadMask() { maskTexture.SetPixelData(mask.Pixels, 0); maskTexture.Apply(false, false); }
        private void SetIdleEnabled(bool ready)
        {
            if (originalEnabled == null) return;
            for (int i = 0; i < idleBehaviours.Length && i < originalEnabled.Length; i++)
                if (idleBehaviours[i] != null && idleBehaviours[i] != this)
                    idleBehaviours[i].enabled = ready && originalEnabled[i];
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            SetIdleEnabled(true); originalEnabled = null;
            renderPass?.Dispose(); renderPass = null;
            if (composite != null) Destroy(composite);
            if (maskTexture != null) Destroy(maskTexture);
            mask = null; composite = null; maskTexture = null;
        }
    }
}
