using UnityEngine;

namespace Amgen.Deepavali
{
    [CreateAssetMenu(menuName = "Deepavali/Flower Light Sequence")]
    public sealed class FlowerSequencePreset : ScriptableObject
    {
        public Texture2D innerMap, outerMap;
        [Header("Demo timing (seconds)")]
        [Min(.1f)] public float sleepingSeconds = 2;
        [Min(.1f)] public float ambientSeconds = 6;
        [Min(2)] public float rouletteSeconds = 8;
        [Range(2, 10)] public int rouletteTurns = 4;
        [Min(0)] public float winnerHoldSeconds = 1.2f;
        [Min(.1f)] public float resultSeconds = 8;
        [Header("Ambient")]
        [Range(0, 8)] public float ambientBrightness = 3;
        [Range(.05f, 1)] public float ambientSpeed = .3f;
        [Range(0, 1)] public float ambientGapCoverage = .58f;
        [Header("Roulette")]
        [Range(1, 20)] public float rouletteBrightness = 9;
        [Tooltip("Fraction of petal length illuminated downward from the tip.")]
        [Range(.05f, 1)] public float rouletteTipReach = .35f;
        [Range(0, 20)] public float rouletteIconBrightness = 6;
        public Color warmColor = new Color(1, .65f, .22f);
        [Header("Clockwise outcome order, starting at the top")]
        public FlowerPaletteState[] outcomes = {
            FlowerPaletteState.Gold, FlowerPaletteState.Pink, FlowerPaletteState.Purple, FlowerPaletteState.Green,
            FlowerPaletteState.Blue, FlowerPaletteState.Orange, FlowerPaletteState.Cyan, FlowerPaletteState.Red
        };
    }
}
