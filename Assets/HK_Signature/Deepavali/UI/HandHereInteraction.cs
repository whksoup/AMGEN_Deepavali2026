using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Amgen.Deepavali
{
    /// <summary>Reusable appearance/loading/disintegration prompt, with optional experience-driven hover dwell.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(HandHereGraphic))]
    public sealed class HandHereInteraction : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField, Min(0.01f)] private float loadingDuration = 0.65f;
        [SerializeField, Min(0.01f)] private float disintegrateDuration = 0.85f;
        [SerializeField] private UnityEvent onDespawned = new UnityEvent();
        private static readonly HashSet<HandHereInteraction> Active = new HashSet<HandHereInteraction>();
        private HandHereGraphic graphic;
        private Material originalMaterial;
        private Material instanceMaterial;
        private double startedAt;
        private bool skipLoading;
        public bool externallyDriven;
        /// <summary>True while the reversed disintegration assembles the prompt. Delay hover dwell until false.</summary>
        public bool IsAppearing { get; private set; }
        /// <summary>True after a dismissal request, including while waiting for appearance to finish.</summary>
        public bool IsDismissing { get; private set; }
        public float DwellProgress { get; private set; }
        public event System.Action Despawned;

        private void OnEnable()
        {
            graphic = GetComponent<HandHereGraphic>();
            Active.Add(this);
            if (!Application.isPlaying) return;
            originalMaterial = graphic.material;
            instanceMaterial = new Material(originalMaterial) { name = "Hand Guidance (Instance)", hideFlags = HideFlags.DontSave };
            graphic.material = instanceMaterial;
            BeginAppearance();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!externallyDriven && eventData.button == PointerEventData.InputButton.Left) Dismiss();
        }

        /// <summary>Stand-in entry point for the eventual experience-driven despawn animation.</summary>
        public void Dismiss()
        {
            BeginDismissal(false);
        }

        public void BeginDisintegration() => BeginDismissal(true);

        private void BeginDismissal(bool loaded)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || IsDismissing) return;
            IsDismissing = true;
            skipLoading = loaded;
            // Accept early clicks/external requests without snapping a partially assembled badge to solid.
            // IsDismissing also prevents repeated requests from replacing this pending dismissal.
            if (IsAppearing) return;
            StartDismissal();
        }

        private void StartDismissal()
        {
            startedAt = Time.unscaledTimeAsDouble;
            SetState(skipLoading ? 1 : 0, 0, true);
        }

        public void SetDwellProgress(float progress)
        {
            if (IsAppearing || IsDismissing) return;
            DwellProgress = Mathf.Clamp01(progress);
            SetState(DwellProgress, 0, DwellProgress > 0);
        }

        public void ResetPrompt()
        {
            gameObject.SetActive(true);
            BeginAppearance();
        }

        public void CancelAndHide()
        {
            ClearAnimationState();
            gameObject.SetActive(false);
        }

        private void BeginAppearance()
        {
            ClearAnimationState();
            IsAppearing = Application.isPlaying && isActiveAndEnabled;
            startedAt = IsAppearing ? Time.unscaledTimeAsDouble : 0;
            SetState(0, IsAppearing ? 1 : 0, false);
        }

        private void ClearAnimationState()
        {
            IsAppearing = false;
            IsDismissing = false;
            startedAt = 0;
            skipLoading = false;
            DwellProgress = 0;
        }

        public bool ContainsScreenPoint(Vector2 point)
        {
            if (graphic == null || graphic.canvas == null || !isActiveAndEnabled) return false;
            var canvas = graphic.canvas;
            return graphic.ContainsScreenPoint(point, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera);
        }

        private void Update()
        {
            if (IsAppearing)
            {
                float progress = Mathf.Clamp01((float)(Time.unscaledTimeAsDouble - startedAt) /
                    Mathf.Max(0.01f, disintegrateDuration));
                // The existing shader's fragments and dissolve are deterministic in _Dissolve.
                // Run that value backwards with the loading ring idle; no separate effect is needed.
                SetState(0, 1 - progress, false);
                if (progress < 1) return;
                IsAppearing = false;
                if (IsDismissing) StartDismissal();
                return;
            }
            if (!IsDismissing) return;
            float elapsed = (float)(Time.unscaledTimeAsDouble - startedAt);
            float loadTime = skipLoading ? 0 : Mathf.Max(0.01f, loadingDuration);
            float dissolveTime = Mathf.Max(0.01f, disintegrateDuration);
            SetState(skipLoading ? 1 : Mathf.Clamp01(elapsed / loadTime), Mathf.Clamp01((elapsed - loadTime) / dissolveTime), true);
            if (elapsed < loadTime + dissolveTime) return;
            // Disable instead of destroying so the guide can be reused at another position.
            gameObject.SetActive(false);
            onDespawned.Invoke();
            Despawned?.Invoke();
        }

        private void SetState(float loading, float dissolve, bool active)
        {
            if (instanceMaterial == null) return;
            ApplyState(instanceMaterial, loading, dissolve, active);
            // MaskableGraphic can create a separate stencil material.
            var rendered = graphic.materialForRendering;
            if (rendered != instanceMaterial) ApplyState(rendered, loading, dissolve, active);
        }

        private void ApplyState(Material target, float loading, float dissolve, bool active)
        {
            target.SetFloat("_LoadProgress", loading);
            target.SetFloat("_Loading", active ? 1 : 0);
            target.SetFloat("_Dissolve", dissolve);
            target.SetFloat("_GraphicScale", Mathf.Min(graphic.rectTransform.rect.width, graphic.rectTransform.rect.height) / 220f);
        }

        public static bool BlocksScreenPoint(Vector2 screenPosition)
        {
            foreach (var prompt in Active)
                if (prompt != null && prompt.ContainsScreenPoint(screenPosition)) return true;
            return false;
        }

        private void OnDisable()
        {
            Active.Remove(this);
            ClearAnimationState();
            if (instanceMaterial == null) return;
            graphic.material = originalMaterial;
            Destroy(instanceMaterial);
            instanceMaterial = null;
        }
    }
}
