using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    public enum FlowerLightingStage { Sleeping, Ambient, Roulette, Result }

    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    public sealed class FlowerLightSequence : MonoBehaviour
    {
        public FlowerPaletteController flower;
        public FlowerSequencePreset preset;
        [Tooltip("Renderer indices in clockwise screen order, starting at the top.")]
        public int[] innerClockwise = { 0, 1, 2, 3, 4, 5, 6, 7 };
        public bool autoPreview;
        public FlowerLightingStage Stage { get; private set; }
        public FlowerSequencePreset Config { get; private set; }
        public int WinnerSlot { get; private set; } = -1;
        public int ActiveSlot { get; private set; } = -1;
        public FlowerPaletteState WinnerPalette { get; private set; }
        public float StageTime { get; private set; }
        public float SpinProgress => spinSeconds > 0 ? Mathf.Clamp01(StageTime / spinSeconds) : 0;
        public bool WinnerLanded => Stage == FlowerLightingStage.Roulette && StageTime >= spinSeconds;
        public event Action<FlowerLightingStage> StageChanged;
        public event Action<int, FlowerPaletteState> ResultSelected;

        private PetalLightingSettings[] ambient;
        private PetalLightingSettings roulette;
        private FlowerPaletteLighting spillRig;
        private bool originalSpill, ready;
        private float clock, spinSeconds, holdSeconds;
        private int spinTurns;

        private void Start()
        {
            if (flower == null || preset == null || preset.innerMap == null || preset.outerMap == null ||
                innerClockwise == null || innerClockwise.Length != 8 || preset.outcomes == null || preset.outcomes.Length != 8)
            { Fail("Assign a flower, maps, eight clockwise petals and eight color outcomes."); return; }
            for (int i = 0; i < 8; i++)
            {
                if (flower.GetPetalRenderer(innerClockwise[i]) == null || preset.outcomes[i] == FlowerPaletteState.Base || !flower.SupportsPalette(preset.outcomes[i]))
                { Fail("Missing petal or color palette at roulette slot " + i); return; }
                for (int j = 0; j < i; j++)
                    if (innerClockwise[i] == innerClockwise[j] || preset.outcomes[i] == preset.outcomes[j])
                    { Fail("Roulette needs eight distinct petals and eight distinct colors."); return; }
            }
            Config = Instantiate(preset); Config.name = preset.name; Config.hideFlags = HideFlags.DontSave;
            ambient = new PetalLightingSettings[flower.PetalCount];
            for (int i = 0; i < ambient.Length; i++)
                ambient[i] = new PetalLightingSettings { growth = false, tipBrightness = 0, gapDepth = 1,
                    washWidth = .2f, washStrength = .12f, shimmer = .8f, seed = 3 + i * 7.13f };
            // The adjustable tip mask follows the edge coverage. Icon emission
            // shares the same per-petal fade. No whole-edge emission or inward wash.
            roulette = new PetalLightingSettings { growth = false, edgeBrightness = 0, tipBrightness = 0, shimmer = 0,
                gapCoverage = 0, gapDepth = 0, washStrength = 0, washWidth = .15f };
            spillRig = FindFirstObjectByType<FlowerPaletteLighting>();
            originalSpill = spillRig != null && spillRig.enabled;
            ready = true;
            Sleep();
        }

        private void Fail(string message) { Debug.LogError(message, this); enabled = false; }
        private void LateUpdate() => Tick(Time.deltaTime);

        /// <summary>Explicit delta supports deterministic preview and host-controlled playback.</summary>
        public void Tick(float delta)
        {
            if (!ready || !isActiveAndEnabled || !float.IsFinite(delta) || delta < 0) return;
            StageTime += delta; clock += delta;
            if (autoPreview && Stage == FlowerLightingStage.Sleeping && StageTime >= Mathf.Max(.1f, Config.sleepingSeconds)) WakeAmbient();
            else if (autoPreview && Stage == FlowerLightingStage.Ambient && StageTime >= Mathf.Max(.1f, Config.ambientSeconds)) StartRoulette();
            else if (Stage == FlowerLightingStage.Roulette)
            {
                ActiveSlot = FlowerRoulettePath.Slot(SpinProgress, spinTurns, WinnerSlot);
                if (StageTime >= spinSeconds + holdSeconds) RevealWinner();
            }
            else if (autoPreview && Stage == FlowerLightingStage.Result && StageTime >= Mathf.Max(.1f, Config.resultSeconds)) Sleep();
            ApplyLighting();
        }

        public void Sleep()
        {
            if (!ready) return;
            flower.ResetToInitialState(); WinnerSlot = ActiveSlot = -1; clock = 0;
            SetStage(FlowerLightingStage.Sleeping);
        }

        public void WakeAmbient()
        {
            if (!ready) return;
            flower.ResetToInitialState(); WinnerSlot = ActiveSlot = -1;
            SetStage(FlowerLightingStage.Ambient);
        }

        public bool StartRoulette()
        {
            if (!ready || Stage == FlowerLightingStage.Roulette) return false;
            flower.ResetToInitialState();
            // Sample once. Frame timing and slowdown never change the selected outcome.
            WinnerSlot = UnityEngine.Random.Range(0, innerClockwise.Length);
            WinnerPalette = Config.outcomes[WinnerSlot];
            spinSeconds = Mathf.Max(2, Config.rouletteSeconds); spinTurns = Mathf.Clamp(Config.rouletteTurns, 2, 10);
            holdSeconds = Mathf.Max(0, Config.winnerHoldSeconds); ActiveSlot = 0;
            SetStage(FlowerLightingStage.Roulette); return true;
        }

        public bool PreviewResult(int slot)
        {
            if (!ready || slot < 0 || slot >= Config.outcomes.Length) return false;
            autoPreview = false;
            WinnerSlot = slot; WinnerPalette = Config.outcomes[slot];
            RevealWinner(); return true;
        }

        private void RevealWinner()
        {
            if (!flower.TransitionTo(WinnerPalette)) { Fail("Result palette unavailable: " + WinnerPalette); return; }
            ActiveSlot = WinnerSlot;
            SetStage(FlowerLightingStage.Result);
            ResultSelected?.Invoke(WinnerSlot, WinnerPalette);
        }

        public void PlaySequence() { if (!ready) return; autoPreview = true; Sleep(); }

        private void SetStage(FlowerLightingStage stage)
        {
            Stage = stage; StageTime = 0;
            ApplyLighting(); StageChanged?.Invoke(stage);
        }

        public void ApplyLighting()
        {
            if (!ready) return;
            bool result = Stage == FlowerLightingStage.Result;
            if (spillRig != null) spillRig.enabled = originalSpill && result;
            float reveal = result ? Mathf.SmoothStep(0, 1, StageTime / .9f) : 0;
            float wake = Mathf.SmoothStep(0, 1, StageTime / 1.3f);
            float roulettePosition = Stage == FlowerLightingStage.Roulette
                ? FlowerRoulettePath.Position(SpinProgress, spinTurns, WinnerSlot) : 0;
            float rouletteEnvelope = Mathf.SmoothStep(0, 1, StageTime / .25f)
                * Mathf.SmoothStep(0, 1, (spinSeconds + holdSeconds - StageTime) / .3f);
            for (int i = 0; i < flower.PetalCount; i++)
            {
                int slot = Array.IndexOf(innerClockwise, i);
                Texture2D map = slot >= 0 ? Config.innerMap : Config.outerMap;
                if (Stage == FlowerLightingStage.Ambient)
                {
                    var settings = ambient[i];
                    float drift = Mathf.PerlinNoise(i * 1.731f + 12, clock * Mathf.Clamp(Config.ambientSpeed, .05f, 1) * .38f);
                    float pool = Mathf.Pow(Mathf.Clamp01((drift - .27f) / .5f), 3);
                    settings.edgeBrightness = Mathf.Clamp(Config.ambientBrightness, 0, 8) * (.025f + .975f * pool);
                    settings.gapCoverage = Mathf.Clamp01(Config.ambientGapCoverage + .08f * Mathf.Sin(clock * .19f + i * 2.3f));
                    settings.speed = Config.ambientSpeed; settings.color = Config.warmColor;
                    flower.SetSpatialLighting(i, settings, map, clock, true);
                    flower.SetPetalStageLevels(i, wake, 0, .48f);
                }
                else if (Stage == FlowerLightingStage.Roulette)
                {
                    float weight = FlowerRoulettePath.TipWeight(roulettePosition, slot) * rouletteEnvelope;
                    roulette.tipBrightness = Config.rouletteBrightness; roulette.color = Config.warmColor;
                    roulette.tipReach = Config.rouletteTipReach; roulette.iconBrightness = Config.rouletteIconBrightness;
                    flower.SetSpatialLighting(i, roulette, map, clock, slot >= 0);
                    flower.SetPetalStageLevels(i, weight, 0, slot >= 0 ? .3f : .16f);
                }
                else
                {
                    flower.SetSpatialLighting(i, null, null, 0, false);
                    flower.SetPetalStageLevels(i, result ? reveal : 0, result ? 1 : 0, result ? Mathf.Lerp(.3f, 1, reveal) : .35f);
                }
            }
            flower.SetCenterStageLevels(result ? reveal : 0, result ? 1 : 0, result ? Mathf.Lerp(.3f, 1, reveal) : .3f, result ? 1 : 0);
        }

        private void OnDisable()
        {
            if (!ready) return;
            flower.ClearSequenceLighting();
            if (spillRig != null) spillRig.enabled = originalSpill;
        }
        private void OnEnable() { if (ready) Sleep(); }
        private void OnDestroy() { if (Config != null) Destroy(Config); }
    }
}
