using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class FlowerPaletteController
    {
        [Header("Opening artwork")]
        [Tooltip("Full-petal masks used only while a petal is in its startup palette. Normal artwork stays independently selectable.")]
        [SerializeField] private FlowerSymbolSet startupSymbolSet;
        [Tooltip("Optional initial center colors, consumed by the first explicit center/whole-flower palette request.")]
        [SerializeField] private FlowerStartupPalette startupCenterPalette;
        [Tooltip("Opening icon index in Center Icon Set. The six phase icon indices remain 0-5.")]
        [SerializeField] private int startupCenterIcon = -1;

        private bool startupCenterConsumed;
        private FlowerPaletteState lastAuthoredCenterState;
        private int startupCenterRevision;
        private FlowerPalette StartupCenter => startupCenterPalette != null ? startupCenterPalette.Colors : null;
        private int StartupCenterRevision => startupCenterPalette != null ? startupCenterPalette.Revision : -1;

        public FlowerSymbolSet StartupSymbolSet => startupSymbolSet;
        public bool IsUsingStartupCenter => Ready && settled[runtimePetals.Length] == StartupState;
        public bool IsStartupRequested(int index) => ValidPetal(index) && requested[index] == StartupState;
        public int GetStartupSymbol(int index) => index >= 0 && index < PetalCount ? petalAppearances[index].startupSymbol : -1;

        private void ConfigureStartupArtwork(int index)
        {
            var mat = runtimePetals[index];
            int symbol = petalAppearances[index].startupSymbol;
            bool valid = petalAppearances[index].useStartupArtwork && startupSymbolSet != null && startupSymbolSet.TryGet(symbol, out _);
            mat.SetFloat("_StartupSymbolEnabled", valid ? 1f : 0f);
            if (!valid) return;
            startupSymbolSet.TryGet(symbol, out var entry);
            var rect = entry.uvRect;
            mat.SetTexture("_StartupAtlas", startupSymbolSet.Atlas);
            mat.SetFloat("_StartupMaxMip", startupSymbolSet.MaxMipLevel);
            mat.SetVector("_StartupRect", new Vector4(rect.width, rect.height, rect.x, rect.y));
        }

        private static Color EdgeHue(FlowerPalette palette) => palette.useEdgeColor ? palette.edge.linear : palette.fill.linear;

        /// <summary>Replay the authored opening palettes/artwork, keeping regular phase settings intact.</summary>
        public void ResetToInitialState()
        {
            if (!Ready) return;
            ClearOutcomeAppearance();
            for (int i = 0; i < PetalCount; i++)
                if (StartupFor(i) != null) startupConsumed[i] = false;
            startupCenterConsumed = false;
            ApplyAuthoredAppearance();
        }
    }
}
