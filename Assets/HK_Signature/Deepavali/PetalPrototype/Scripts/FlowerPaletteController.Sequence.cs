using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class FlowerPaletteController
    {
        private static readonly int StageLevels = Shader.PropertyToID("_FlowerStageLevels");

        public bool SupportsPalette(FlowerPaletteState state) => Ready && ValidState(state);

        public void SetPetalStageLevels(int index, float emission, float ornament, float surface)
        {
            if (!ValidPetal(index)) return;
            runtimePetals[index].SetVector(StageLevels, new Vector4(Mathf.Clamp01(emission), Mathf.Clamp01(ornament), Mathf.Clamp01(surface), 1));
        }

        public void SetCenterStageLevels(float emission, float ornament, float surface, float rim)
        {
            if (!Ready) return;
            runtimeCenter.SetVector(StageLevels, new Vector4(Mathf.Clamp01(emission), Mathf.Clamp01(ornament), Mathf.Clamp01(surface), Mathf.Clamp01(rim)));
        }

        public void ClearSequenceLighting()
        {
            if (!Ready) return;
            foreach (var material in runtimePetals)
            {
                material.SetVector(StageLevels, Vector4.one);
                material.SetFloat(SpatialEnabled, 0);
            }
            runtimeCenter.SetVector(StageLevels, Vector4.one);
        }
    }
}
