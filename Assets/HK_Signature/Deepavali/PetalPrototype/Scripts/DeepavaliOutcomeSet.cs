using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    [Serializable]
    public sealed class DeepavaliOutcome
    {
        public FlowerPaletteState palette;
        public string symbol;
        public int openingSymbol;
        public int icon;
        public string scrollVariant;

        public DeepavaliOutcome Snapshot() => (DeepavaliOutcome)MemberwiseClone();
    }

    [CreateAssetMenu(menuName = "Deepavali/Eight Outcomes")]
    public sealed class DeepavaliOutcomeSet : ScriptableObject
    {
        public FlowerSymbolSet icons;
        public int openingCenterIcon = 8;
        [Tooltip("Wheel order, clockwise. This is the shared mapping for opening petals, result and scroll.")]
        public DeepavaliOutcome[] entries = new DeepavaliOutcome[8];

        public DeepavaliOutcome At(int slot) => entries != null && slot >= 0 && slot < entries.Length ? entries[slot] : null;
        public DeepavaliOutcome ForPalette(FlowerPaletteState palette) => entries == null ? null : Array.Find(entries, e => e != null && e.palette == palette);
        public DeepavaliOutcome ForOpeningSymbol(int symbol) => entries == null ? null : Array.Find(entries, e => e != null && e.openingSymbol == symbol);

        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Length != 8 || icons == null || !icons.TryGet(openingCenterIcon, out _))
            { error = "Assign eight outcomes and an icon atlas including the opening mandala."; return false; }
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e == null || e.palette < FlowerPaletteState.Gold || e.palette > FlowerPaletteState.Red ||
                    string.IsNullOrWhiteSpace(e.symbol) || string.IsNullOrWhiteSpace(e.scrollVariant) ||
                    e.openingSymbol < 0 || !icons.TryGet(e.icon, out _))
                { error = "Incomplete outcome at slot " + i; return false; }
                for (int j = 0; j < i; j++)
                    if (entries[j].palette == e.palette || entries[j].openingSymbol == e.openingSymbol || entries[j].icon == e.icon ||
                        string.Equals(entries[j].scrollVariant, e.scrollVariant, StringComparison.OrdinalIgnoreCase))
                    { error = "Outcomes must have distinct palettes, symbols and scroll variants."; return false; }
            }
            return true;
        }
    }
}
