using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>Ignites the existing lamps when their screen positions have been restored.</summary>
    [DefaultExecutionOrder(500), DisallowMultipleComponent]
    public sealed class RestorationLampResponse : MonoBehaviour
    {
        public ScreenRestorationController restoration;
        public DeepavaliBackground background;
        [Range(.01f, 1)] public float ignitionThreshold = .55f;
        [Min(.01f)] public float ignitionDuration = .55f;
        public int LitLampCount { get; private set; }
        private bool[] ignited, originalLightEnabled;
        private float[] levels, originalIntensity, originalFlameLevels;
        private Renderer[] flames;
        private MaterialPropertyBlock working;
        private Vector4 originalSoilLevels;
        private static readonly int FlameLevel = Shader.PropertyToID("_Restoration");
        private static readonly int SoilLevels = Shader.PropertyToID("_LampRestoration");

        private void OnEnable()
        {
            if (!Application.isPlaying || background == null || restoration == null) return;
            int count = background.lamps.Length;
            ignited = new bool[count]; levels = new float[count];
            originalLightEnabled = new bool[count]; originalIntensity = new float[count];
            flames = new Renderer[count]; originalFlameLevels = new float[count];
            working = new MaterialPropertyBlock();
            for (int i = 0; i < count; i++)
            {
                if (i < background.flames.Length && background.flames[i] != null)
                    flames[i] = background.flames[i].GetComponent<Renderer>();
                originalFlameLevels[i] = 1;
                if (flames[i] != null)
                {
                    flames[i].GetPropertyBlock(working);
                    originalFlameLevels[i] = working.HasFloat(FlameLevel) ? working.GetFloat(FlameLevel)
                        : flames[i].sharedMaterial.GetFloat(FlameLevel);
                }
                if (i < background.lampLights.Length && background.lampLights[i] != null)
                {
                    originalLightEnabled[i] = background.lampLights[i].enabled;
                    originalIntensity[i] = background.lampLights[i].intensity;
                }
            }
            originalSoilLevels = Vector4.one;
            if (background.soil != null)
            {
                background.soil.GetPropertyBlock(working);
                originalSoilLevels = working.HasVector(SoilLevels) ? working.GetVector(SoilLevels)
                    : background.soil.sharedMaterial.GetVector(SoilLevels);
            }
            restoration.onRestorationStarted.AddListener(ResetLamps);
            ResetLamps();
        }

        private void ResetLamps()
        {
            if (ignited == null) return;
            System.Array.Clear(ignited, 0, ignited.Length); System.Array.Clear(levels, 0, levels.Length);
            LitLampCount = 0; ApplyLevels();
        }

        private void LateUpdate()
        {
            if (levels == null || restoration == null) return;
            for (int i = 0; i < levels.Length; i++)
            {
                var anchor = background.lamps[i];
                if (anchor == null) continue;
                Vector3 viewport = restoration.interactionCamera.WorldToViewportPoint(anchor.position);
                if (!ignited[i] && (restoration.Phase != RestorationPhase.Restoring ||
                    (viewport.z > 0 && restoration.SampleRestoration(viewport) >= ignitionThreshold)))
                { ignited[i] = true; LitLampCount++; }
                levels[i] = Mathf.MoveTowards(levels[i], ignited[i] ? 1 : 0,
                    Time.unscaledDeltaTime / Mathf.Max(.01f, ignitionDuration));
            }
            ApplyLevels();
        }

        private void ApplyLevels()
        {
            Vector4 soil = Vector4.one;
            for (int i = 0; i < levels.Length; i++)
            {
                float level = Mathf.SmoothStep(0, 1, levels[i]);
                if (i < 4) soil[i] = level;
                if (flames[i] != null)
                {
                    flames[i].GetPropertyBlock(working); working.SetFloat(FlameLevel, level * originalFlameLevels[i]);
                    flames[i].SetPropertyBlock(working);
                }
                if (i < background.lampLights.Length && background.lampLights[i] != null)
                {
                    var light = background.lampLights[i];
                    light.enabled = originalLightEnabled[i] && level > 0;
                    // The background's LateUpdate has already supplied this frame's authored flicker.
                    light.intensity = (background.isActiveAndEnabled ? light.intensity : originalIntensity[i]) * level;
                }
            }
            if (background.soil != null)
            {
                background.soil.GetPropertyBlock(working); working.SetVector(SoilLevels, Vector4.Scale(soil, originalSoilLevels));
                background.soil.SetPropertyBlock(working);
            }
        }

        private void OnDisable()
        {
            if (restoration != null) restoration.onRestorationStarted.RemoveListener(ResetLamps);
            if (levels == null || background == null) return;
            for (int i = 0; i < levels.Length; i++)
            {
                if (flames[i] != null)
                {
                    flames[i].GetPropertyBlock(working); working.SetFloat(FlameLevel, originalFlameLevels[i]);
                    flames[i].SetPropertyBlock(working);
                }
                if (i < background.lampLights.Length && background.lampLights[i] != null)
                {
                    background.lampLights[i].enabled = originalLightEnabled[i];
                    background.lampLights[i].intensity = originalIntensity[i];
                }
            }
            if (background.soil != null)
            {
                background.soil.GetPropertyBlock(working); working.SetVector(SoilLevels, originalSoilLevels);
                background.soil.SetPropertyBlock(working);
            }
            levels = null;
        }
    }
}
