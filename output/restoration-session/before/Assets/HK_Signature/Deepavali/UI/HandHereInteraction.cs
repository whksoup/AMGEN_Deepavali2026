using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Amgen.Deepavali
{
    /// <summary>Temporary click -> loading ring -> shader despawn sequence. Re-enable to reset.</summary>
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
        public bool IsDismissing { get; private set; }

        private void OnEnable()
        {
            graphic = GetComponent<HandHereGraphic>();
            Active.Add(this);
            if (!Application.isPlaying) return;
            originalMaterial = graphic.material;
            instanceMaterial = new Material(originalMaterial) { name = "Hand Guidance (Instance)", hideFlags = HideFlags.DontSave };
            graphic.material = instanceMaterial;
            IsDismissing = false;
            SetState(0, 0, false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Dismiss();
        }

        /// <summary>Stand-in entry point for the eventual experience-driven despawn animation.</summary>
        public void Dismiss()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || IsDismissing) return;
            IsDismissing = true;
            startedAt = Time.unscaledTimeAsDouble;
            SetState(0, 0, true);
        }

        private void Update()
        {
            if (!IsDismissing) return;
            float elapsed = (float)(Time.unscaledTimeAsDouble - startedAt);
            float loadTime = Mathf.Max(0.01f, loadingDuration);
            float dissolveTime = Mathf.Max(0.01f, disintegrateDuration);
            SetState(Mathf.Clamp01(elapsed / loadTime), Mathf.Clamp01((elapsed - loadTime) / dissolveTime), true);
            if (elapsed < loadTime + dissolveTime) return;
            // Disable instead of destroying so the guide can be reused at another position.
            gameObject.SetActive(false);
            onDespawned.Invoke();
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
                if (prompt != null && prompt.isActiveAndEnabled && prompt.graphic != null && prompt.graphic.canvas != null &&
                    prompt.graphic.ContainsScreenPoint(screenPosition, prompt.graphic.canvas.worldCamera)) return true;
            return false;
        }

        private void OnDisable()
        {
            Active.Remove(this);
            IsDismissing = false;
            if (instanceMaterial == null) return;
            graphic.material = originalMaterial;
            Destroy(instanceMaterial);
            instanceMaterial = null;
        }
    }
}
