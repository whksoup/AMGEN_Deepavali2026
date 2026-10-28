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
        private float[] levels, originalIntensity;
        private Renderer[] flames;
        private MaterialPropertyBlock[] originalBlocks;
        private MaterialPropertyBlock originalSoil, working;
        private static readonly int FlameLevel = Shader.PropertyToID("_Restoration");
        private static readonly int SoilLevels = Shader.PropertyToID("_LampRestoration");

        private void OnEnable()
        {
            if (!Application.isPlaying || background == null || restoration == null) return;
            int count = background.lamps.Length;
            ignited = new bool[count]; levels = new float[count];
            originalLightEnabled = new bool[count]; originalIntensity = new float[count];
            flames = new Renderer[count]; originalBlocks = new MaterialPropertyBlock[count];
            working = new MaterialPropertyBlock();
            for (int i = 0; i < count; i++)
            {
                if (i < background.flames.Length && background.flames[i] != null)
                    flames[i] = background.flames[i].GetComponent<Renderer>();
                originalBlocks[i] = new MaterialPropertyBlock();
                if (flames[i] != null) flames[i].GetPropertyBlock(originalBlocks[i]);
                if (i < background.lampLights.Length && background.lampLights[i] != null)
                {
                    originalLightEnabled[i] = background.lampLights[i].enabled;
                    originalIntensity[i] = background.lampLights[i].intensity;
                }
            }
            originalSoil = new MaterialPropertyBlock();
            if (background.soil != null) background.soil.GetPropertyBlock(originalSoil);
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
                    flames[i].GetPropertyBlock(working); working.SetFloat(FlameLevel, level);
                    flames[i].SetPropertyBlock(working);
                }
                if (i < background.lampLights.Length && background.lampLights[i] != null)
                {
                    var light = background.lampLights[i];
                    light.enabled = originalLightEnabled[i] && level > 0;
                    // The background's LateUpdate has already supplied this frame's authored flicker.
                    light.intensity *= level;
                }
            }
            if (background.soil != null)
            {
                background.soil.GetPropertyBlock(working); working.SetVector(SoilLevels, soil);
                background.soil.SetPropertyBlock(working);
            }
        }

        private void OnDisable()
        {
            if (restoration != null) restoration.onRestorationStarted.RemoveListener(ResetLamps);
            if (levels == null || background == null) return;
            for (int i = 0; i < levels.Length; i++)
            {
                if (flames[i] != null) flames[i].SetPropertyBlock(originalBlocks[i]);
                if (i < background.lampLights.Length && background.lampLights[i] != null)
                {
                    background.lampLights[i].enabled = originalLightEnabled[i];
                    background.lampLights[i].intensity = originalIntensity[i];
                }
            }
            if (background.soil != null) background.soil.SetPropertyBlock(originalSoil);
            levels = null;
        }
    }
}
