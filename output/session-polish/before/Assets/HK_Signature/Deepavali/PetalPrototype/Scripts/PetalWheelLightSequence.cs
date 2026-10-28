using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    [DefaultExecutionOrder(150), DisallowMultipleComponent]
    public sealed partial class PetalWheelLightSequence : MonoBehaviour
    {
        public FlowerPaletteController flower;
        [Tooltip("Lighting styles and masks. Uses the flower's Outcome Set when assigned; style outcomes are for legacy previews.")]
        public FlowerSequencePreset style;
        public int[] innerClockwise = { 0, 1, 2, 3, 4, 5, 6, 7 };
        public int[] outerClockwise;
        public PetalWheelTiming timing = new PetalWheelTiming();
        [Header("Light visibility")]
        [Range(1, 8)] public float shimmerIntensity = 3.5f;
        [Range(0, 8)] public float outerLightIntensity = 1.9f;
        [Range(0, 8)] public float outerShimmerIntensity = 1.4f;
        [Tooltip("Emissive illumination of the outer petal's existing colored artwork.")]
        [Range(0, 8)] public float outerSurfaceGlow = 3;
        public int variationSeed = 137;
        public bool autoPreview;
        [Min(.1f)] public float previewShimmerSeconds = 6, previewReadySeconds = 3;
        public PetalWheelPattern Pattern { get; private set; }
        public DeepavaliOutcome SelectedOutcome { get; private set; }
        public FlowerPaletteState OutcomePalette(int slot) => flower != null && flower.outcomeSet != null
            ? flower.outcomeSet.At(slot).palette : style.outcomes[slot];
        // Runtime ownership only; shared preview presets and their return timings remain intact.
        public bool HoldResult { get; set; }
        public bool IsResultFullyVisible => Pattern != null && Pattern.Mode == PetalWheelMode.Win &&
            Pattern.Bloom >= 1 && Pattern.BloomAlpha >= 1 && paletteStarted && flower.TransitionProgress >= 1;
        public event Action<int, FlowerPaletteState> ResultSelected;
        private PetalLightingSettings[] ambient;
        private PetalLightingSettings roulette;
        private FlowerPaletteLighting spillRig;
        private bool originalSpill, paletteStarted;
        private PetalWheelMode previousMode;

        private void Start()
        {
            if (settingsPreset != null) settingsPreset.ApplyTo(this);
            if (flower == null || style == null || style.innerMap == null || style.outerMap == null ||
                innerClockwise == null || innerClockwise.Length != 8 || outerClockwise == null || outerClockwise.Length == 0 ||
                innerClockwise.Length + outerClockwise.Length != flower.PetalCount || style.outcomes == null || style.outcomes.Length != 8)
            { Fail("Assign the flower, existing lighting style, eight inner petals and all outer petals."); return; }
            if (flower.outcomeSet != null && !flower.outcomeSet.Validate(out var outcomeError))
            { Fail(outcomeError); return; }
            var used = new bool[flower.PetalCount];
            foreach (var ring in new[] { innerClockwise, outerClockwise }) foreach (int i in ring)
            {
                if (i < 0 || i >= used.Length || used[i] || flower.GetPetalRenderer(i) == null)
                { Fail("Petal mappings must be complete, distinct renderer indices."); return; }
                used[i] = true;
            }
            for (int i = 0; i < 8; i++)
            {
                if (OutcomePalette(i) == FlowerPaletteState.Base || !flower.SupportsPalette(OutcomePalette(i)))
                { Fail("Missing result palette at slot " + i); return; }
                for (int j = 0; j < i; j++) if (OutcomePalette(i) == OutcomePalette(j)) { Fail("Result palettes must be distinct."); return; }
                if (flower.outcomeSet != null && flower.GetStartupSymbol(innerClockwise[i]) != flower.outcomeSet.At(i).openingSymbol)
                { Fail("Opening symbol does not match outcome at wheel slot " + i); return; }
            }
            ambient = new PetalLightingSettings[flower.PetalCount];
            for (int i = 0; i < ambient.Length; i++)
                ambient[i] = new PetalLightingSettings { growth = false, tipBrightness = 0, gapDepth = 1,
                    washWidth = .2f, washStrength = .12f, shimmer = .8f, seed = 3 + i * 7.13f };
            roulette = new PetalLightingSettings { growth = false, edgeBrightness = 0, tipBrightness = 0, shimmer = 0,
                gapCoverage = 0, gapDepth = 0, washStrength = 0, washWidth = .15f };
            spillRig = FindFirstObjectByType<FlowerPaletteLighting>();
            originalSpill = spillRig != null && spillRig.enabled;
            ResetPreview();
        }
        private void Fail(string message) { Debug.LogError(message, this); enabled = false; }
        private void LateUpdate() => Tick(Time.deltaTime);
        public void ResetPreview()
        {
            if (ambient == null || flower == null) return;
            flower.ResetToInitialState(); paletteStarted = false;
            SelectedOutcome = null;
            flower.SetPatternPaletteProgress(1); flower.SetPatternLightHandoff(0);
            Pattern = new PetalWheelPattern(timing, outerClockwise.Length, variationSeed);
            previousMode = Pattern.Mode; Pattern.Tick(1f / 120);
            if (isActiveAndEnabled) ApplyLighting();
        }
        public void Tick(float delta)
        {
            if (Pattern == null || !isActiveAndEnabled || !float.IsFinite(delta) || delta < 0) return;
            Pattern.HoldResult = HoldResult;
            Pattern.Tick(delta);
            if (autoPreview && Pattern.Mode == PetalWheelMode.Shimmer && Pattern.StageTime >= previewShimmerSeconds) Pattern.EnterWheel();
            else if (autoPreview && Pattern.Mode == PetalWheelMode.Ready && Pattern.StageTime >= previewReadySeconds) Pattern.Spin();
            if (Pattern.Mode == PetalWheelMode.Win && previousMode != PetalWheelMode.Win)
            {
                SelectedOutcome = flower.outcomeSet != null ? flower.outcomeSet.At(Pattern.Winner).Snapshot() : null;
                ResultSelected?.Invoke(Pattern.Winner, SelectedOutcome != null ? SelectedOutcome.palette : OutcomePalette(Pattern.Winner));
            }
            previousMode = Pattern.Mode;
            ApplyLighting();
        }
        public bool EnterWheel() => Pattern != null && Pattern.EnterWheel();
        public bool Spin(int target = -1) => Pattern != null && Pattern.Spin(target);
        public bool Shimmer() => Pattern != null && Pattern.ReturnToShimmer();
        public bool PreviewResult(int slot)
        {
            if (Pattern == null || !Pattern.PreviewWin(slot)) return false;
            flower.ResetToInitialState(); flower.SetPatternPaletteProgress(1); paletteStarted = false;
            SelectedOutcome = flower.outcomeSet != null ? flower.outcomeSet.At(slot).Snapshot() : null;
            return true;
        }

        public void ApplyLighting()
        {
            if (Pattern == null) return;
            var p = Pattern;
            float reveal = PetalWheelPattern.Ease(p.Bloom) * p.BloomAlpha;
            if (p.Mode == PetalWheelMode.Win && p.Bloom > 0 && !paletteStarted)
                paletteStarted = SelectedOutcome != null ? flower.BeginPatternOutcome(SelectedOutcome, innerClockwise)
                    : flower.BeginPatternPalette(OutcomePalette(p.Winner));
            if (paletteStarted)
            {
                // Existing radial wave uses the mockup's cubic 1.8-second envelope.
                flower.SetPatternPaletteProgress(PetalWheelPattern.Ease(p.Bloom), p.BloomAlpha);
                if (p.BloomAlpha <= 0) { flower.ResetToInitialState(); flower.SetPatternPaletteProgress(1); paletteStarted = false; }
            }
            flower.SetPatternLightHandoff(reveal);
            if (spillRig != null) spillRig.enabled = originalSpill && reveal > .01f;
            bool ambientMode = p.Mode == PetalWheelMode.Shimmer || p.Mode == PetalWheelMode.Pulse;
            EnsureShimmerLights();
            for (int i = 0; i < flower.PetalCount; i++)
            {
                int inner = Array.IndexOf(innerClockwise, i);
                int outer = inner < 0 ? Array.IndexOf(outerClockwise, i) : -1;
                float level = inner >= 0 ? p.Inner[inner] : p.Outer[outer];
                bool tip = inner >= 0 && !ambientMode;
                var light = tip ? roulette : ambient[i];
                light.color = style.warmColor;
                if (tip)
                {
                    light.tipBrightness = style.rouletteBrightness;
                    light.tipReach = style.rouletteTipReach;
                    light.iconBrightness = style.rouletteIconBrightness;
                }
                else
                {
                    light.edgeBrightness = style.ambientBrightness;
                    light.gapCoverage = Mathf.Clamp01(style.ambientGapCoverage + .08f * Mathf.Sin(p.Clock * .19f + i * 2.3f));
                    light.speed = style.ambientSpeed;
                }
                // Handoff to the authored full-palette lighting only when the result blooms.
                // The inner chase keeps exactly the old tip mask, zero wash, and icon channel.
                // Outer petals illuminate their full authored pink/blue artwork rather
                // than replacing it with the warm, broken-edge spatial effect.
                bool fullOuter = outer >= 0;
                float gain = fullOuter ? (p.Mode == PetalWheelMode.Shimmer ? outerShimmerIntensity : outerLightIntensity)
                    : ambientMode ? shimmerIntensity : 1;
                flower.SetPatternPetalIllumination(i, Mathf.Lerp(gain, 1, reveal), fullOuter ? outerSurfaceGlow * (1 - reveal) : 0);
                flower.SetSpatialLighting(i, light, inner >= 0 ? style.innerMap : style.outerMap, p.Clock, !fullOuter && reveal < 1);
                float surface = fullOuter ? Mathf.Lerp(.16f, 1, level) : ambientMode ? .48f : .3f;
                flower.SetPetalStageLevels(i, Mathf.Lerp(level, 1, reveal), fullOuter ? 1 : reveal,
                    Mathf.Lerp(surface, 1, reveal));
                UpdateShimmerLight(i, level);
            }
            float hub = Mathf.Lerp(p.Hub, 1, reveal), icon = Mathf.Lerp(p.CenterIcon, 1, reveal);
            float center = Mathf.Max(hub, icon);
            flower.SetPatternCenterIntensity(Mathf.Lerp(ambientMode ? shimmerIntensity : 1, 1, reveal));
            flower.SetCenterStageLevels(center, center > 0 ? icon / center : 0, Mathf.Lerp(.3f, 1, reveal), center > 0 ? hub / center : 0);
        }
        private void OnDisable()
        {
            ReleaseShimmerLights();
            if (Pattern == null || flower == null) return;
            flower.ClearSequenceLighting();
            flower.SetPatternLightHandoff(0); flower.SetPatternPaletteProgress(1);
            for (int i = 0; i < flower.PetalCount; i++) flower.SetPatternPetalIllumination(i, 1, 0);
            flower.SetPatternCenterIntensity(1);
            if (spillRig != null) spillRig.enabled = originalSpill;
        }
        private void OnEnable() { if (Pattern != null) ResetPreview(); }
    }
}
