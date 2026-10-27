using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    public enum FlowerPaletteState { Base, Gold, Pink, Purple, Green, Blue, Orange }

    [Serializable]
    public sealed class FlowerPalette
    {
        public string label;
        public Color fill;
        public Color ornament;
        public Color accent;
        [Tooltip("Legacy coordinated edge color. Current emission derives from fill; retained for existing assets.")]
        public Color edge;
        public Color center;
        [Min(0f)] public float edgeIntensity = 3f;
    }

    [CreateAssetMenu(menuName = "Deepavali/Flower Palette Set")]
    public sealed class FlowerPaletteSet : ScriptableObject
    {
        [Tooltip("Enum order: Base, Gold, Pink, Purple, Green, Blue, Orange. Colors use sRGB.")]
        [SerializeField] private FlowerPalette[] states = new FlowerPalette[7];

        public bool TryGet(FlowerPaletteState state, out FlowerPalette palette)
        {
            int index = (int)state;
            palette = states != null && index >= 0 && index < states.Length && index < 7 ? states[index] : null;
            return palette != null;
        }
    }
}
