using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class FlowerPaletteController
    {
        [Header("Integrated eight-state experience (optional)")]
        public DeepavaliOutcomeSet outcomeSet;
        private FlowerPalette[] outcomeOpeningPalettes;
        public DeepavaliOutcome CurrentOutcome { get; private set; }

        private void InitializeOutcomeAppearance()
        {
            outcomeOpeningPalettes = null;
            CurrentOutcome = null;
            if (outcomeSet == null) return;
            outcomeOpeningPalettes = new FlowerPalette[PetalCount];
            for (int i = 0; i < PetalCount; i++)
            {
                var p = petalAppearances[i];
                var outcome = p.useStartupArtwork ? outcomeSet.ForOpeningSymbol(p.startupSymbol) : null;
                if (outcome == null || p.startupPalette == null || !PaletteFor(i).TryGet(outcome.palette, out var colors)) continue;
                // Retain the authored ornament/edge; the fill comes from the actual result palette.
                var opening = JsonUtility.FromJson<FlowerPalette>(JsonUtility.ToJson(p.startupPalette.Colors));
                opening.fill = colors.fill;
                outcomeOpeningPalettes[i] = opening;
            }
        }

        private FlowerPalette OutcomeOpeningPalette(int index) => outcomeOpeningPalettes != null && index < outcomeOpeningPalettes.Length
            ? outcomeOpeningPalettes[index] : null;

        public bool BeginPatternOutcome(DeepavaliOutcome outcome, int[] innerPetals)
        {
            if (!Ready || outcomeSet == null || outcome == null || !outcomeSet.icons.TryGet(outcome.icon, out var icon)) return false;
            if (!BeginPatternPalette(outcome.palette)) return false;
            CurrentOutcome = outcome.Snapshot();
            var r = icon.uvRect;
            foreach (int index in innerPetals)
            {
                var mat = runtimePetals[index];
                mat.SetTexture("_OutcomeIconAtlas", outcomeSet.icons.Atlas);
                mat.SetVector("_OutcomeIconRect", new Vector4(r.width, r.height, r.x, r.y));
                mat.SetFloat("_OutcomeIconMaxMip", outcomeSet.icons.MaxMipLevel);
                // Derive orientation from this leaf's actual rest mapping, including refined asymmetric petals.
                var x = mat.GetVector(RestX); var z = mat.GetVector(RestZ);
                float angle = Mathf.Atan2(-x.y, -z.y);
                mat.SetVector("_OutcomeIconRotation", new Vector4(Mathf.Cos(angle), Mathf.Sin(angle), 0, 0));
                mat.SetFloat("_OutcomeIconEnabled", 1);
            }
            return true;
        }

        private void ClearOutcomeAppearance()
        {
            CurrentOutcome = null;
            if (runtimePetals != null) foreach (var mat in runtimePetals)
                if (mat != null) mat.SetFloat("_OutcomeIconEnabled", 0);
        }
    }
}
