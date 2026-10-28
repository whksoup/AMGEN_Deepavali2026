using UnityEngine;

namespace Amgen.Deepavali
{
    /// <summary>Per-petal artwork API and UnityEvent target. The flower remains the sole material owner.</summary>
    [DisallowMultipleComponent]
    public sealed class PetalArtworkController : MonoBehaviour
    {
        [SerializeField] private FlowerPaletteController flower;
        [SerializeField] private Renderer surface;

        public int PetalIndex => flower == null || surface == null ? -1 : flower.GetPetalIndex(surface);
        public int RequestedArtwork => PetalIndex < 0 ? -1 : flower.GetSymbol(PetalIndex);
        public int CurrentArtwork => PetalIndex < 0 ? -1 : flower.GetSettledSymbol(PetalIndex);
        public bool IsTransitioning => PetalIndex >= 0 && flower.IsArtworkTransitioning(PetalIndex);
        public float Progress => PetalIndex < 0 ? 1f : flower.GetArtworkProgress(PetalIndex);
        public bool HasPendingArtwork => PetalIndex >= 0 && flower.HasPendingArtwork(PetalIndex);

        // Void wrappers appear in the UnityEvent Inspector. Use Try... from code to check acceptance.
        public void SetArtwork(int symbol) => TrySetArtwork(symbol);
        public bool TrySetArtwork(int symbol) => isActiveAndEnabled && PetalIndex >= 0 && flower.TransitionSymbol(PetalIndex, symbol);
        public void SetArtworkImmediate(int symbol) => TrySetArtworkImmediate(symbol);
        public bool TrySetArtworkImmediate(int symbol) => isActiveAndEnabled && PetalIndex >= 0 && flower.SetSymbol(PetalIndex, symbol);

        [ContextMenu("Restore Original Artwork (Play Mode)")]
        public void RestoreOriginal() => SetArtwork(-1);

        [ContextMenu("Next Artwork (Play Mode)")]
        public void NextArtwork()
        {
            if (flower == null || flower.SymbolSet == null) return;
            int next = RequestedArtwork + 1;
            SetArtwork(next >= flower.SymbolSet.Count ? -1 : next);
        }

        private void Reset()
        {
            flower = GetComponentInParent<FlowerPaletteController>();
            surface = GetComponentInChildren<SkinnedMeshRenderer>();
        }
    }
}
