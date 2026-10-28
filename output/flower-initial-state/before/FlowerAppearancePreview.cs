using UnityEngine;
using UnityEngine.UI;

namespace Amgen.Deepavali
{
    /// <summary>Review-scene controls. App/session code can call the palette controller directly.</summary>
    public sealed class FlowerAppearancePreview : MonoBehaviour
    {
        [SerializeField] private FlowerPaletteController flower;
        [SerializeField] private PetalLayerPointerInput pointerInput;
        [SerializeField] private Text selectionLabel;
        [SerializeField] private Slider emissionSlider;
        [SerializeField] private PetalArtworkController[] artworkControls;
        private int selectedPetal = -1;
        private FlowerPaletteState[] mixedLayout;
        public int SelectedPetal => selectedPetal;

        private void OnEnable()
        {
            if (flower == null || pointerInput == null) return;
            mixedLayout = new FlowerPaletteState[flower.PetalCount];
            for (int i = 0; i < mixedLayout.Length; i++) mixedLayout[i] = (FlowerPaletteState)(i % 7);
            // A larger flower can inherit this review UI from the inner-layer scene.
            if (artworkControls == null || artworkControls.Length != flower.PetalCount)
            {
                artworkControls = new PetalArtworkController[flower.PetalCount];
                foreach (var control in flower.GetComponentsInChildren<PetalArtworkController>(true))
                {
                    int index = control.PetalIndex;
                    if (index >= 0 && index < artworkControls.Length) artworkControls[index] = control;
                }
            }
            pointerInput.PetalPressed += OnPetalPressed;
            RefreshLabel();
        }
        private void OnDisable() { if (pointerInput != null) pointerInput.PetalPressed -= OnPetalPressed; }
        private void OnPetalPressed(PetalPickTarget petal)
        {
            selectedPetal = flower.GetPetalIndex(petal.Surface);
            RefreshLabel();
        }
        public void SelectAll() { selectedPetal = -1; RefreshLabel(); }
        public void SelectPalette(int state)
        {
            if (selectedPetal < 0) flower.TransitionTo((FlowerPaletteState)state);
            else flower.TransitionPetalTo(selectedPetal, (FlowerPaletteState)state);
            RefreshLabel();
        }
        public void ShowMixedPalettes()
        {
            flower.TransitionToLayout(mixedLayout, flower.SelectedState);
            RefreshLabel();
        }
        public void CycleSymbol()
        {
            if (flower.SymbolSet == null || flower.SymbolSet.Count == 0) return;
            int current = flower.GetSymbol(selectedPetal < 0 ? 0 : selectedPetal);
            int next = current + 1;
            if (next >= flower.SymbolSet.Count) next = -1;
            if (selectedPetal < 0) for (int i = 0; i < flower.PetalCount; i++) RequestArtwork(i, next);
            else RequestArtwork(selectedPetal, next);
            RefreshLabel();
        }
        private void RequestArtwork(int index, int symbol)
        {
            if (artworkControls != null && index < artworkControls.Length && artworkControls[index] != null)
                artworkControls[index].SetArtwork(symbol);
            else flower.TransitionSymbol(index, symbol);
        }
        public void SetEmission(float value)
        {
            if (selectedPetal < 0) for (int i = 0; i < flower.PetalCount; i++) flower.SetPetalEmissionScale(i, value);
            else flower.SetPetalEmissionScale(selectedPetal, value);
        }
        private void RefreshLabel()
        {
            if (flower == null) return;
            string subject = selectedPetal < 0 ? "All petals" : "Petal " + (selectedPetal + 1).ToString("00");
            int symbol = flower.GetSymbol(selectedPetal < 0 ? 0 : selectedPetal);
            string artwork = flower.SymbolSet != null && flower.SymbolSet.TryGet(symbol, out var entry) ? entry.label : "Original";
            if (selectedPetal < 0)
                for (int i = 1; i < flower.PetalCount; i++)
                    if (flower.GetSymbol(i) != symbol) { artwork = "Mixed"; break; }
            if (selectionLabel != null) selectionLabel.text = subject + " | Artwork: " + artwork + "\nTap a petal to select it. Choose a palette below.";
            if (emissionSlider != null) emissionSlider.SetValueWithoutNotify(flower.GetPetalEmissionScale(selectedPetal < 0 ? 0 : selectedPetal));
        }
    }
}
