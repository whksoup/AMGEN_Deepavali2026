using UnityEngine;

namespace Amgen.Deepavali
{
    public sealed partial class FlowerPaletteController
    {
        // Independent clocks and one latest-request slot per petal. No frame allocations.
        private struct ArtworkWave
        {
            public int settled, target, requested;
            public bool active;
            public double startedAt;
            public float duration, progress;
        }
        private ArtworkWave[] artworkWaves;
        private static readonly int SymbolEnabled = Shader.PropertyToID("_SymbolEnabled");
        private static readonly int SymbolAtlas = Shader.PropertyToID("_SymbolAtlas");
        private static readonly int SymbolRect = Shader.PropertyToID("_SymbolRect");
        private static readonly int SymbolMaxMip = Shader.PropertyToID("_SymbolMaxMip");
        private static readonly int TargetSymbolEnabled = Shader.PropertyToID("_TargetSymbolEnabled");
        private static readonly int TargetSymbolRect = Shader.PropertyToID("_TargetSymbolRect");
        private static readonly int ArtworkProgress = Shader.PropertyToID("_ArtworkProgress");
        private static readonly int ArtworkSoftness = Shader.PropertyToID("_ArtworkSoftness");
        private static readonly int ArtworkUvToRoot = Shader.PropertyToID("_ArtworkUvToRoot");
        private static readonly int ArtworkRadius = Shader.PropertyToID("_ArtworkRadius");

        private bool ValidSymbol(int symbol) => symbol == -1 ||
            (symbol >= 0 && symbolSet != null && symbolSet.TryGet(symbol, out _));

        private void InitializeArtwork()
        {
            artworkWaves = new ArtworkWave[runtimePetals.Length];
            for (int i = 0; i < runtimePetals.Length; i++)
            {
                var p = petalAppearances[i];
                // Imported mesh root is (0,0). Map planar UV into root-relative XY,
                // normalized by petal length, so the front is a circular arc.
                var mapping = new Vector4(p.artworkSize.x / p.artworkSize.y, 1f,
                    p.artworkMin.x / p.artworkSize.y, p.artworkMin.y / p.artworkSize.y);
                runtimePetals[i].SetVector(ArtworkUvToRoot, mapping);
                float radius = 0f;
                for (int u = 0; u < 2; u++) for (int v = 0; v < 2; v++)
                    radius = Mathf.Max(radius, new Vector2(u * mapping.x + mapping.z, v + mapping.w).magnitude);
                runtimePetals[i].SetFloat(ArtworkRadius, radius);
            }
        }

        /// <summary>Immediate artwork change; cancels this petal's active/pending artwork only.</summary>
        public bool SetSymbol(int index, int symbol)
        {
            if (!ValidPetal(index) || !ValidSymbol(symbol)) return false;
            petalAppearances[index].symbol = symbol;
            artworkWaves[index] = new ArtworkWave { settled = symbol, target = symbol, requested = symbol, progress = 1f };
            ApplySymbolPair(index, symbol, symbol);
            runtimePetals[index].SetFloat(ArtworkProgress, 1f);
            return true;
        }

        /// <summary>Reveal artwork from root to tip. Finish the active reveal, then use the latest request.</summary>
        public bool TransitionSymbol(int index, int symbol)
        {
            if (!ValidPetal(index) || !ValidSymbol(symbol)) return false;
            petalAppearances[index].symbol = symbol;
            artworkWaves[index].requested = symbol;
            if (!artworkWaves[index].active) StartNextArtwork(index);
            return true;
        }

        public int GetSettledSymbol(int index) => artworkWaves == null ? GetSymbol(index) : artworkWaves[index].settled;
        public bool IsArtworkTransitioning(int index) => ValidPetal(index) && artworkWaves[index].active;
        public bool HasPendingArtwork(int index) => ValidPetal(index) && artworkWaves[index].requested != artworkWaves[index].target;
        public float GetArtworkProgress(int index) => ValidPetal(index) ? artworkWaves[index].progress : 1f;

        private void StartNextArtwork(int index)
        {
            ref var wave = ref artworkWaves[index];
            if (wave.requested == wave.settled) return;
            var p = petalAppearances[index];
            wave.target = wave.requested;
            wave.duration = Finite(p.artworkDuration) ? Mathf.Max(0.01f, p.artworkDuration) : 1.2f;
            wave.startedAt = Time.timeAsDouble;
            wave.progress = 0f;
            wave.active = true;
            ApplySymbolPair(index, wave.settled, wave.target);
            runtimePetals[index].SetFloat(ArtworkSoftness,
                Finite(p.artworkSoftness) ? Mathf.Clamp(p.artworkSoftness, 0.001f, 0.5f) : 0.04f);
            runtimePetals[index].SetFloat(ArtworkProgress, 0f);
        }

        private void ApplySymbolPair(int index, int from, int to)
        {
            var mat = runtimePetals[index];
            if (symbolSet != null)
            {
                mat.SetTexture(SymbolAtlas, symbolSet.Atlas);
                mat.SetFloat(SymbolMaxMip, symbolSet.MaxMipLevel);
            }
            ApplySymbolSlot(mat, from, SymbolEnabled, SymbolRect);
            ApplySymbolSlot(mat, to, TargetSymbolEnabled, TargetSymbolRect);
        }

        private void ApplySymbolSlot(Material mat, int symbol, int enabledId, int rectId)
        {
            bool enabled = symbol >= 0 && symbolSet != null && symbolSet.TryGet(symbol, out _);
            mat.SetFloat(enabledId, enabled ? 1f : 0f);
            if (!enabled) return;
            symbolSet.TryGet(symbol, out var entry);
            var rect = entry.uvRect;
            mat.SetVector(rectId, new Vector4(rect.width, rect.height, rect.x, rect.y));
        }

        private void UpdateArtwork()
        {
            if (!Ready || artworkWaves == null) return;
            for (int i = 0; i < artworkWaves.Length; i++)
            {
                ref var wave = ref artworkWaves[i];
                if (!wave.active) continue;
                wave.progress = Mathf.Clamp01((float)((Time.timeAsDouble - wave.startedAt) / wave.duration));
                runtimePetals[i].SetFloat(ArtworkProgress, wave.progress);
                if (wave.progress < 1f) continue;
                wave.settled = wave.target;
                wave.active = false;
                ApplySymbolPair(i, wave.settled, wave.settled);
                StartNextArtwork(i);
            }
        }
    }
}
