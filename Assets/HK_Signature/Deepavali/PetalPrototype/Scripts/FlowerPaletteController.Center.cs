using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class FlowerPaletteController
    {
        [Header("Center ring and palette icons")]
        [Tooltip("Six icons in Gold, Pink, Purple, Green, Blue, Orange order. Base has no icon.")]
        [SerializeField] private FlowerSymbolSet centerIconSet;
        [SerializeField, Range(0.1f, 1f)] private float centerIconScale = 0.65f;
        [Tooltip("Ring radius as a fraction of the disc radius.")]
        [SerializeField, Range(0.5f, 0.98f)] private float centerRingRadius = 0.92f;
        [Tooltip("Ring half-width as a fraction of the disc radius.")]
        [SerializeField, Range(0.002f, 0.04f)] private float centerRingWidth = 0.012f;
        [SerializeField, Min(0f)] private float centerRingGlow = 1.3f;
        [SerializeField, Min(0f)] private float centerIconGlow = 1.3f;
        [Tooltip("Glow of the small dotted ring inside the rim. Zero hides the dots.")]
        [SerializeField, Min(0f)] private float centerDotGlow = 0.5f;

        private static readonly int CenterIconScale = Shader.PropertyToID("_CenterIconScale");
        private static readonly int CenterRingRadius = Shader.PropertyToID("_CenterRingRadius");
        private static readonly int CenterRingWidth = Shader.PropertyToID("_CenterRingWidth");
        private static readonly int CenterRingGlow = Shader.PropertyToID("_CenterRingGlow");
        private static readonly int CenterIconGlow = Shader.PropertyToID("_CenterIconGlow");
        private static readonly int CenterDotGlow = Shader.PropertyToID("_CenterDotGlow");
        private static readonly int CenterRingColor = Shader.PropertyToID("_CenterRingColor");
        private static readonly int TargetCenterRingColor = Shader.PropertyToID("_TargetCenterRingColor");

        public FlowerSymbolSet CenterIconSet => centerIconSet;
        public FlowerPaletteState CenterState => settled == null ? selectedState : settled[settled.Length - 1];
        public int GetCenterIcon(FlowerPaletteState state)
        {
            int index = (int)state - 1;
            return index >= 0 && index < 6 && centerIconSet != null && centerIconSet.TryGet(index, out _) ? index : -1;
        }

        /// <summary>Change only the center palette/icon through the shared outward wave and latest-request queue.</summary>
        public bool TransitionCenterTo(FlowerPaletteState state)
        {
            if (!Ready || !paletteSet.TryGet(state, out _)) return false;
            selectedState = state;
            requested[runtimePetals.Length] = state;
            if (!transitioning) StartNextWave();
            return true;
        }

        /// <summary>Immediate center palette/icon change; other surfaces' transitions continue.</summary>
        public bool SetCenterState(FlowerPaletteState state)
        {
            if (!Ready || !paletteSet.TryGet(state, out _)) return false;
            selectedState = state;
            settled[runtimePetals.Length] = targets[runtimePetals.Length] = requested[runtimePetals.Length] = state;
            ApplyPair(runtimeCenter, state, state, true, paletteSet);
            return true;
        }

        private void ApplyCenterArtworkPair(Material mat, FlowerPaletteState from, FlowerPaletteState to)
        {
            if (centerIconSet != null)
            {
                mat.SetTexture(SymbolAtlas, centerIconSet.Atlas);
                mat.SetFloat(SymbolMaxMip, centerIconSet.MaxMipLevel);
            }
            ApplyCenterIcon(mat, GetCenterIcon(from), SymbolEnabled, SymbolRect);
            ApplyCenterIcon(mat, GetCenterIcon(to), TargetSymbolEnabled, TargetSymbolRect);
            mat.SetFloat(CenterIconScale, centerIconScale);
            mat.SetFloat(CenterRingRadius, centerRingRadius);
            mat.SetFloat(CenterRingWidth, centerRingWidth);
            mat.SetFloat(CenterRingGlow, centerRingGlow);
            mat.SetFloat(CenterIconGlow, centerIconGlow);
            mat.SetFloat(CenterDotGlow, centerDotGlow);
        }

        private void ApplyCenterIcon(Material mat, int icon, int enabledId, int rectId)
        {
            bool valid = icon >= 0 && centerIconSet != null && centerIconSet.TryGet(icon, out _);
            mat.SetFloat(enabledId, valid ? 1f : 0f);
            if (!valid) return;
            centerIconSet.TryGet(icon, out var entry);
            var r = entry.uvRect;
            mat.SetVector(rectId, new Vector4(r.width, r.height, r.x, r.y));
        }

        private void ValidateCenterAppearance()
        {
            centerIconScale = Finite(centerIconScale) ? Mathf.Clamp(centerIconScale, 0.1f, 1f) : 0.65f;
            centerRingRadius = Finite(centerRingRadius) ? Mathf.Clamp(centerRingRadius, 0.5f, 0.98f) : 0.92f;
            centerRingWidth = Finite(centerRingWidth) ? Mathf.Clamp(centerRingWidth, 0.002f, 0.04f) : 0.012f;
            centerRingGlow = Finite(centerRingGlow) ? Mathf.Max(0f, centerRingGlow) : 1.3f;
            centerIconGlow = Finite(centerIconGlow) ? Mathf.Max(0f, centerIconGlow) : 1.3f;
            centerDotGlow = Finite(centerDotGlow) ? Mathf.Max(0f, centerDotGlow) : 0.5f;
        }
    }
}
