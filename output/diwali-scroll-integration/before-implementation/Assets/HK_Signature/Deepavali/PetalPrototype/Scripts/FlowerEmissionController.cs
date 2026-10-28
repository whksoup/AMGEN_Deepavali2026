using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>One runtime material controls every petal's masked edge lights.</summary>
    [DisallowMultipleComponent]
    public sealed class FlowerEmissionController : MonoBehaviour
    {
        [SerializeField] private Material petalMaterial;
        [SerializeField] private Renderer[] petals;
        [Tooltip("Shared light color, in sRGB. The center artwork is unaffected.")]
        [SerializeField] private Color edgeColor = new Color(1f, 0.72f, 0.28f, 1f);
        [SerializeField, Min(0f)] private float edgeIntensity = 3f;

        [Header("Tap feedback — shared across the flower")]
        [SerializeField] private Color flashColor = new Color(1f, 0.18f, 0.48f, 1f);
        [SerializeField, Min(0.01f)] private float flashAttack = 0.05f;
        [SerializeField, Min(0f)] private float flashHold = 0.12f;
        [SerializeField, Min(0.01f)] private float flashFade = 0.55f;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private Material runtimeMaterial;
        private Material[] originalMaterials;
        private bool flashing;
        private double flashStartedAt;
        private float flashStartWeight;
        private float flashWeight;

        public Color EdgeColor => edgeColor;
        public float EdgeIntensity => edgeIntensity;
        public float FlashWeight => flashWeight;

        private void OnEnable()
        {
            ResetFlash();
            if (petalMaterial == null || petals == null || !petalMaterial.HasProperty(EmissionColor))
            {
                Debug.LogError("Assign a lit petal material with emission and the flower's petal renderers.", this);
                enabled = false;
                return;
            }

            runtimeMaterial = new Material(petalMaterial)
            {
                name = petalMaterial.name + " (Flower Runtime)",
                hideFlags = HideFlags.DontSave
            };
            runtimeMaterial.EnableKeyword("_EMISSION");
            originalMaterials = new Material[petals.Length];
            for (int i = 0; i < petals.Length; i++)
            {
                if (petals[i] == null) continue;
                originalMaterials[i] = petals[i].sharedMaterial;
                petals[i].sharedMaterial = runtimeMaterial;
            }
            ApplyEmission();
        }

        private void Update()
        {
            if (!flashing) return;
            // Timestamp the input frame so a slow previous frame cannot consume a fresh flash.
            float flashElapsed = (float)(Time.timeAsDouble - flashStartedAt);
            if (flashElapsed < flashAttack)
                flashWeight = Mathf.Lerp(flashStartWeight, 1f, Smooth01(flashElapsed / flashAttack));
            else if (flashElapsed < flashAttack + flashHold)
                flashWeight = 1f;
            else if (flashElapsed < flashAttack + flashHold + flashFade)
                flashWeight = 1f - Smooth01((flashElapsed - flashAttack - flashHold) / flashFade);
            else
                ResetFlash();
            ApplyEmission();
        }

        /// <summary>Briefly recolor all edge lights, then return to the current baseline color.</summary>
        [ContextMenu("Trigger Color Flash (Play Mode)")]
        public void TriggerFlash()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || runtimeMaterial == null) return;
            // Repeated or simultaneous taps restart from the current blend without snapping
            // or stacking intensity, timers, coroutines, or material instances.
            flashStartWeight = flashWeight;
            flashStartedAt = Time.timeAsDouble;
            flashing = true;
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private void ResetFlash()
        {
            flashing = false;
            flashStartedAt = 0;
            flashStartWeight = flashWeight = 0f;
        }

        /// <summary>Call from an interaction/session controller, using an sRGB color.</summary>
        public void SetEdgeColor(Color color)
        {
            edgeColor = color;
            ApplyEmission();
        }

        /// <summary>Zero turns the edge lights off; values above one allow HDR emission.</summary>
        public void SetEdgeIntensity(float intensity)
        {
            edgeIntensity = Mathf.Max(0f, intensity);
            ApplyEmission();
        }

        private void ApplyEmission()
        {
            if (runtimeMaterial != null)
                runtimeMaterial.SetColor(EmissionColor,
                    Color.Lerp(edgeColor.linear, flashColor.linear, flashWeight) * edgeIntensity);
        }

        private void OnValidate()
        {
            edgeIntensity = Mathf.Max(0f, edgeIntensity);
            flashAttack = Mathf.Max(0.01f, flashAttack);
            flashHold = Mathf.Max(0f, flashHold);
            flashFade = Mathf.Max(0.01f, flashFade);
            ApplyEmission();
        }

        private void OnDisable()
        {
            ResetFlash();
            if (runtimeMaterial == null) return;
            for (int i = 0; petals != null && originalMaterials != null && i < petals.Length && i < originalMaterials.Length; i++)
                if (petals[i] != null && petals[i].sharedMaterial == runtimeMaterial)
                    petals[i].sharedMaterial = originalMaterials[i];
            if (Application.isPlaying) Destroy(runtimeMaterial);
            else DestroyImmediate(runtimeMaterial);
            runtimeMaterial = null;
            originalMaterials = null;
        }
    }
}
