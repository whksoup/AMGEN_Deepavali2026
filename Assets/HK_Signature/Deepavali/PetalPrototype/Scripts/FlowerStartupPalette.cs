using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>One-time opening colors; normal palette requests replace them.</summary>
    [CreateAssetMenu(menuName = "Deepavali/Flower Startup Palette")]
    public sealed class FlowerStartupPalette : ScriptableObject
    {
        [SerializeField] private FlowerPalette colors = new FlowerPalette();
        [NonSerialized] private int revision;
        public FlowerPalette Colors => colors;
        public int Revision => revision;
        private void OnValidate() { unchecked { revision++; } }
    }
}
