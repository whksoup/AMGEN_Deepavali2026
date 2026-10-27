using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    [Serializable]
    public sealed class FlowerSymbol
    {
        public string label;
        [Tooltip("Atlas UV rectangle, excluding padded gutters. Array index is the symbol ID.")]
        public Rect uvRect;
    }

    [CreateAssetMenu(menuName = "Deepavali/Flower Symbol Set")]
    public sealed class FlowerSymbolSet : ScriptableObject
    {
        [SerializeField] private Texture2D atlas;
        [SerializeField, Range(0, 8)] private float maxMipLevel = 3;
        [SerializeField] private FlowerSymbol[] symbols = Array.Empty<FlowerSymbol>();
        public Texture2D Atlas => atlas;
        public float MaxMipLevel => maxMipLevel;
        public int Count => symbols == null ? 0 : symbols.Length;
        public bool TryGet(int index, out FlowerSymbol symbol)
        {
            symbol = index >= 0 && index < Count ? symbols[index] : null;
            if (atlas == null || symbol == null) return false;
            var r = symbol.uvRect;
            return r.width > 0 && r.height > 0 && r.xMin >= 0 && r.yMin >= 0 && r.xMax <= 1 && r.yMax <= 1;
        }
    }
}
