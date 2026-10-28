using System;
using UnityEngine;

namespace Amgen.Deepavali
{
    [Serializable]
    public sealed class PetalAppearance
    {
        public FlowerPaletteState palette;
        [Tooltip("Optional per-petal tone set. Whole-flower state controls still apply; null uses the main palette.")]
        public FlowerPaletteSet paletteOverride;
        [Tooltip("Optional opening colors. Replaced by the first palette request for this petal, including a request for its existing state.")]
        public FlowerStartupPalette startupPalette;
        [Tooltip("Optional artwork material. Re-enable after changing this during Play Mode.")]
        public Material materialOverride;
        [Min(0f)] public float emissionScale = 1f;
        [Tooltip("-1 keeps the original ornament. Otherwise, index in the shared symbol set.")]
        public int symbol = -1;
        [Tooltip("Seconds for this petal's root-to-tip artwork reveal.")]
        [Min(0.01f)] public float artworkDuration = 1.2f;
        [Tooltip("Reveal half-width as a fraction of the petal length.")]
        [Range(0.001f, 0.5f)] public float artworkSoftness = 0.04f;
        [Tooltip("Planar ArtworkUV bounds in imported mesh XY coordinates, independent of bones.")]
        public Vector2 artworkMin = new Vector2(-0.091f, 0f);
        public Vector2 artworkSize = new Vector2(0.182f, 0.263f);
    }

    /// <summary>Sole material owner: shared radial palette wave and independent petal artwork reveals.</summary>
    [DisallowMultipleComponent]
    public sealed partial class FlowerPaletteController : MonoBehaviour
    {
        [SerializeField] private FlowerPaletteSet paletteSet;
        [Tooltip("Center palette and initial default for newly added petals.")]
        [SerializeField] private FlowerPaletteState selectedState;
        [SerializeField] private Material petalMaterial;
        [SerializeField] private Renderer[] petals;
        [SerializeField] private Renderer centerDisc;
        [SerializeField] private PetalAppearance[] petalAppearances = Array.Empty<PetalAppearance>();
        [SerializeField] private FlowerSymbolSet symbolSet;
        [Header("Outward palette wave")]
        [SerializeField, Min(0.01f)] private float transitionDuration = 1.2f;
        [SerializeField, Min(0.001f)] private float waveSoftness = 0.025f;
        [SerializeField] private Vector2 centerArtworkMin = new Vector2(-0.121f, -0.121f);
        [SerializeField] private Vector2 centerArtworkSize = new Vector2(0.242f, 0.242f);

        private Material[] runtimePetals, originalPetals;
        private Material runtimeCenter, originalCenter;
        private Renderer[] boundPetals;
        private Renderer boundCenter;
        // Last slot is the center. Allocate only when enabled.
        private FlowerPaletteState[] settled, targets, requested;
        private bool transitioning, appearanceDirty;
        private double waveStartedAt;
        private float activeDuration, activeSoftness, maxRadius, progress = 1f;
        private int paletteRevision;
        private int[] overrideRevisions;
        // Private state only: serialized/public seven-state palette indices stay unchanged.
        private const FlowerPaletteState StartupState = (FlowerPaletteState)(-1);
        private bool[] startupConsumed;
        private FlowerPaletteState[] lastAuthoredStates;
        private int[] startupRevisions;

        private static readonly int Fill = Shader.PropertyToID("_BaseColor");
        private static readonly int Ornament = Shader.PropertyToID("_PatternColor");
        private static readonly int Accent = Shader.PropertyToID("_AccentColor");
        private static readonly int Intensity = Shader.PropertyToID("_EdgeIntensity");
        private static readonly int TargetFill = Shader.PropertyToID("_TargetBaseColor");
        private static readonly int TargetOrnament = Shader.PropertyToID("_TargetPatternColor");
        private static readonly int TargetAccent = Shader.PropertyToID("_TargetAccentColor");
        private static readonly int TargetIntensity = Shader.PropertyToID("_TargetEdgeIntensity");
        private static readonly int EmissionScale = Shader.PropertyToID("_EmissionScale");
        private static readonly int Wave = Shader.PropertyToID("_WaveProgress");
        private static readonly int Radius = Shader.PropertyToID("_WaveRadius");
        private static readonly int Softness = Shader.PropertyToID("_WaveSoftness");
        private static readonly int RestX = Shader.PropertyToID("_RestUvToFlowerX");
        private static readonly int RestZ = Shader.PropertyToID("_RestUvToFlowerZ");
        private static readonly int CenterSurface = Shader.PropertyToID("_CenterSurface");

        public FlowerPaletteState SelectedState => selectedState;
        public FlowerPaletteSet PaletteSet => paletteSet;
        public FlowerSymbolSet SymbolSet => symbolSet;
        public int PetalCount => petals == null ? 0 : petals.Length;
        public bool IsTransitioning => transitioning;
        public float TransitionProgress => progress;
        public float TransitionDuration => transitionDuration;
        public bool HasPendingTransition
        {
            get
            {
                if (requested == null) return false;
                for (int i = 0; i < requested.Length; i++) if (requested[i] != targets[i]) return true;
                return false;
            }
        }

        private void EnsureAppearances()
        {
            if (petalAppearances == null || petalAppearances.Length != PetalCount)
                Array.Resize(ref petalAppearances, PetalCount);
            for (int i = 0; i < PetalCount; i++)
                if (petalAppearances[i] == null) petalAppearances[i] = new PetalAppearance { palette = selectedState };
        }

        private void OnEnable()
        {
            EnsureAppearances();
            if (paletteSet == null || !paletteSet.TryGet(selectedState, out _) || petalMaterial == null ||
                !petalMaterial.HasProperty(TargetFill) || PetalCount == 0 || centerDisc == null || centerDisc.sharedMaterial == null)
            {
                FailSetup("Assign palettes, the Petal Palette Lit material, petal renderers and center disc.");
                return;
            }
            for (int i = 0; i < PetalCount; i++)
            {
                if (petals[i] == null || petals[i] == centerDisc || petals[i].sharedMaterial == null ||
                    !PaletteFor(i).TryGet(petalAppearances[i].palette, out _) || !ValidAppearance(petalAppearances[i]) ||
                    !MaterialFor(i).HasProperty(TargetFill))
                { FailSetup("Invalid petal appearance or renderer at index " + i); return; }
                for (int j = 0; j < i; j++)
                    if (petals[j] == petals[i]) { FailSetup("Duplicate petal renderer at index " + i); return; }
            }
            boundPetals = (Renderer[])petals.Clone();
            boundCenter = centerDisc;
            runtimePetals = new Material[PetalCount];
            originalPetals = new Material[PetalCount];
            settled = new FlowerPaletteState[PetalCount + 1];
            targets = new FlowerPaletteState[PetalCount + 1];
            requested = new FlowerPaletteState[PetalCount + 1];
            overrideRevisions = new int[PetalCount];
            startupRevisions = new int[PetalCount];
            lastAuthoredStates = new FlowerPaletteState[PetalCount];
            if (startupConsumed == null || startupConsumed.Length != PetalCount)
                startupConsumed = new bool[PetalCount];
            maxRadius = 0f;
            for (int i = 0; i < runtimePetals.Length; i++)
            {
                originalPetals[i] = boundPetals[i].sharedMaterial;
                var mat = new Material(MaterialFor(i)) { name = $"Flower Petal {i + 1:00} (Runtime)", hideFlags = HideFlags.DontSave };
                runtimePetals[i] = mat;
                mat.SetFloat(CenterSurface, 0f);
                ConfigureMapping(mat, boundPetals[i], petalAppearances[i].artworkMin, petalAppearances[i].artworkSize);
                boundPetals[i].sharedMaterial = mat;
            }
            originalCenter = boundCenter.sharedMaterial;
            runtimeCenter = new Material(petalMaterial) { name = "Flower Center (Runtime)", hideFlags = HideFlags.DontSave };
            runtimeCenter.SetFloat(CenterSurface, 1f);
            runtimeCenter.SetFloat("_Smoothness", originalCenter.HasProperty("_Smoothness") ? originalCenter.GetFloat("_Smoothness") : 0.25f);
            ConfigureMapping(runtimeCenter, boundCenter, centerArtworkMin, centerArtworkSize);
            boundCenter.sharedMaterial = runtimeCenter;
            InitializeArtwork();
            ApplyAuthoredAppearance();
        }

        private void FailSetup(string message) { Debug.LogError(message, this); enabled = false; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool ValidAppearance(PetalAppearance p) => Finite(p.emissionScale) && p.emissionScale >= 0f &&
            Finite(p.artworkMin.x) && Finite(p.artworkMin.y) && Finite(p.artworkSize.x) && Finite(p.artworkSize.y) && p.artworkSize.x > 0f && p.artworkSize.y > 0f;

        private void ConfigureMapping(Material mat, Renderer surface, Vector2 min, Vector2 size)
        {
            // Imported artwork is mesh XY. Snapshot renderer placement, never animated bones.
            var toFlower = transform.worldToLocalMatrix * surface.transform.localToWorldMatrix;
            var origin = toFlower.MultiplyPoint3x4(new Vector3(min.x, min.y, 0f));
            var u = toFlower.MultiplyVector(new Vector3(size.x, 0f, 0f));
            var v = toFlower.MultiplyVector(new Vector3(0f, size.y, 0f));
            mat.SetVector(RestX, new Vector4(u.x, v.x, origin.x, 0f));
            mat.SetVector(RestZ, new Vector4(u.z, v.z, origin.z, 0f));
            mat.SetTextureScale("_BaseMap", Vector2.one);
            mat.SetTextureOffset("_BaseMap", Vector2.zero);
            for (int a = 0; a < 2; a++) for (int b = 0; b < 2; b++)
            {
                var point = origin + u * a + v * b;
                maxRadius = Mathf.Max(maxRadius, new Vector2(point.x, point.z).magnitude);
            }
        }

        private bool Ready => isActiveAndEnabled && runtimeCenter != null && runtimePetals != null &&
            petalAppearances != null && petalAppearances.Length == runtimePetals.Length;
        private bool ValidPetal(int index) => Ready && index >= 0 && index < runtimePetals.Length;
        private FlowerPaletteSet PaletteFor(int index) => petalAppearances[index].paletteOverride != null ? petalAppearances[index].paletteOverride : paletteSet;
        private FlowerPalette StartupFor(int index) => petalAppearances[index].startupPalette != null ? petalAppearances[index].startupPalette.Colors : null;
        private int StartupRevision(int index) => petalAppearances[index].startupPalette != null ? petalAppearances[index].startupPalette.Revision : -1;
        private static bool TryAppearance(FlowerPaletteSet colors, FlowerPaletteState state, FlowerPalette startup, out FlowerPalette palette)
        {
            if (state == StartupState) { palette = startup; return palette != null; }
            return colors.TryGet(state, out palette);
        }
        private Material MaterialFor(int index) => petalAppearances[index].materialOverride != null ? petalAppearances[index].materialOverride : petalMaterial;
        private bool ValidState(FlowerPaletteState state)
        {
            if (paletteSet == null || !paletteSet.TryGet(state, out _)) return false;
            for (int i = 0; i < petalAppearances.Length; i++)
                if (!PaletteFor(i).TryGet(state, out _)) return false;
            return true;
        }
        public FlowerPaletteState GetPetalState(int index) => settled == null || settled[index] == StartupState ? petalAppearances[index].palette : settled[index];
        public bool IsUsingStartupPalette(int index) => ValidPetal(index) && settled[index] == StartupState;
        public FlowerPaletteState GetRequestedPetalState(int index) => petalAppearances[index].palette;
        public float GetPetalEmissionScale(int index) => petalAppearances[index].emissionScale;
        public int GetSymbol(int index) => petalAppearances[index].symbol;
        public int GetPetalIndex(Renderer surface)
        {
            if (petals != null) for (int i = 0; i < petals.Length; i++) if (petals[i] == surface) return i;
            return -1;
        }

        /// <summary>Immediate whole-flower palette reset; artwork waves continue independently.</summary>
        public bool SetState(FlowerPaletteState state)
        {
            if (!Ready || !ValidState(state)) return false;
            selectedState = state;
            for (int i = 0; i < runtimePetals.Length; i++) { startupConsumed[i] = true; petalAppearances[i].palette = state; }
            ApplyAuthoredAppearance(false);
            return true;
        }

        /// <summary>Immediate change to one petal; other petals' waves continue.</summary>
        public bool SetPetalState(int index, FlowerPaletteState state)
        {
            if (!ValidPetal(index) || !ValidState(state)) return false;
            petalAppearances[index].palette = state;
            startupConsumed[index] = true;
            settled[index] = targets[index] = requested[index] = state;
            ApplyPair(runtimePetals[index], state, state, false, PaletteFor(index), StartupFor(index));
            return true;
        }

        public bool SetPetalEmissionScale(int index, float scale)
        {
            if (!ValidPetal(index) || !Finite(scale) || scale < 0f) return false;
            petalAppearances[index].emissionScale = scale;
            runtimePetals[index].SetFloat(EmissionScale, scale);
            return true;
        }

        public bool TransitionTo(FlowerPaletteState state)
        {
            if (!Ready || !ValidState(state)) return false;
            selectedState = state;
            for (int i = 0; i < runtimePetals.Length; i++) { startupConsumed[i] = true; petalAppearances[i].palette = state; requested[i] = state; }
            requested[runtimePetals.Length] = state;
            if (!transitioning) StartNextWave();
            return true;
        }

        public bool TransitionPetalTo(int index, FlowerPaletteState state)
        {
            if (!ValidPetal(index) || !ValidState(state)) return false;
            petalAppearances[index].palette = state;
            startupConsumed[index] = true;
            requested[index] = state;
            if (!transitioning) StartNextWave();
            return true;
        }

        /// <summary>Atomic mixed layout. Copies the input into fixed storage.</summary>
        public bool TransitionToLayout(FlowerPaletteState[] states, FlowerPaletteState center)
        {
            if (!Ready || states == null || states.Length != runtimePetals.Length || !ValidState(center)) return false;
            for (int i = 0; i < states.Length; i++) if (!ValidState(states[i])) return false;
            for (int i = 0; i < states.Length; i++) { startupConsumed[i] = true; requested[i] = states[i]; petalAppearances[i].palette = states[i]; }
            selectedState = center;
            requested[states.Length] = center;
            if (!transitioning) StartNextWave();
            return true;
        }

        private void ApplyAuthoredAppearance(bool resetArtwork = true)
        {
            if (!Ready) return;
            // Structural Inspector changes require re-enable; never resize live ownership.
            if (petalAppearances.Length != runtimePetals.Length) return;
            transitioning = false;
            if (resetArtwork) appearanceDirty = false;
            for (int i = 0; i < runtimePetals.Length; i++)
            {
                var p = petalAppearances[i];
                if (!ValidState(p.palette)) p.palette = selectedState;
                var state = !startupConsumed[i] && StartupFor(i) != null ? StartupState : p.palette;
                settled[i] = targets[i] = requested[i] = state;
                lastAuthoredStates[i] = p.palette;
                ApplyPair(runtimePetals[i], state, state, false, PaletteFor(i), StartupFor(i));
                runtimePetals[i].SetFloat(EmissionScale, Mathf.Max(0f, p.emissionScale));
                if (resetArtwork) SetSymbol(i, ValidSymbol(p.symbol) ? p.symbol : -1);
            }
            int center = runtimePetals.Length;
            settled[center] = targets[center] = requested[center] = selectedState;
            ApplyPair(runtimeCenter, selectedState, selectedState, true, paletteSet);
            CachePaletteRevisions();
            activeSoftness = waveSoftness;
            ConfigureWave();
            SetProgress(1f);
        }

        private void StartNextWave()
        {
            bool changed = false;
            for (int i = 0; i < requested.Length; i++) changed |= requested[i] != settled[i];
            if (!changed) return;
            for (int i = 0; i < requested.Length; i++) targets[i] = requested[i];
            for (int i = 0; i < runtimePetals.Length; i++) ApplyPair(runtimePetals[i], settled[i], targets[i], false, PaletteFor(i), StartupFor(i));
            int center = runtimePetals.Length;
            ApplyPair(runtimeCenter, settled[center], targets[center], true, paletteSet);
            activeDuration = Mathf.Max(0.01f, transitionDuration);
            activeSoftness = Mathf.Max(0.001f, waveSoftness);
            waveStartedAt = Time.timeAsDouble;
            transitioning = true;
            ConfigureWave();
            SetProgress(0f);
        }

        private void SyncInspectorChanges()
        {
            appearanceDirty = false;
            if (!Ready || !ValidState(selectedState)) return;
            for (int i = 0; i < runtimePetals.Length; i++)
            {
                var p = petalAppearances[i];
                // Glow/artwork Inspector changes must not consume opening colors.
                if (ValidState(p.palette) && (startupConsumed[i] || settled[i] != StartupState || p.palette != lastAuthoredStates[i]))
                {
                    startupConsumed[i] = true;
                    requested[i] = p.palette;
                }
                lastAuthoredStates[i] = p.palette;
                float scale = Finite(p.emissionScale) ? Mathf.Max(0f, p.emissionScale) : 0f;
                p.emissionScale = scale;
                runtimePetals[i].SetFloat(EmissionScale, scale);
                if (!TransitionSymbol(i, p.symbol)) p.symbol = artworkWaves[i].requested;
            }
            requested[runtimePetals.Length] = selectedState;
            if (!transitioning) StartNextWave();
            RefreshPaletteColors();
        }

        private void Update()
        {
            if (appearanceDirty) SyncInspectorChanges();
            if (PaletteAssetsChanged()) RefreshPaletteColors();
            UpdateArtwork();
            if (!transitioning) return;
            SetProgress(Mathf.Clamp01((float)((Time.timeAsDouble - waveStartedAt) / activeDuration)));
            if (progress < 1f) return;
            transitioning = false;
            Array.Copy(targets, settled, targets.Length);
            StartNextWave();
        }

        private void ApplyPair(Material mat, FlowerPaletteState from, FlowerPaletteState to, bool center, FlowerPaletteSet colors, FlowerPalette startup = null)
        {
            if (!TryAppearance(colors, from, startup, out var a) || !TryAppearance(colors, to, startup, out var b)) return;
            // Color shader properties are converted to linear by Unity. The center
            // palette is authored in sRGB; pre-converting it makes the disc nearly black.
            mat.SetColor(Fill, center ? a.center : a.fill.linear);
            mat.SetColor(TargetFill, center ? b.center : b.fill.linear);
            mat.SetColor(Ornament, a.ornament.linear); mat.SetColor(TargetOrnament, b.ornament.linear);
            mat.SetColor(Accent, a.accent.linear); mat.SetColor(TargetAccent, b.accent.linear);
            mat.SetColor("_TertiaryColor", (a.tertiary.a > 0f ? a.tertiary : a.accent).linear);
            mat.SetColor("_TargetTertiaryColor", (b.tertiary.a > 0f ? b.tertiary : b.accent).linear);
            mat.SetFloat(Intensity, Mathf.Max(0f, a.edgeIntensity));
            mat.SetFloat(TargetIntensity, Mathf.Max(0f, b.edgeIntensity));
            mat.SetFloat("_MainPatternIntensity", Mathf.Max(0f, a.mainPatternIntensity));
            mat.SetFloat("_TargetMainPatternIntensity", Mathf.Max(0f, b.mainPatternIntensity));
            mat.SetFloat("_AccentPatternIntensity", Mathf.Max(0f, a.accentPatternIntensity));
            mat.SetFloat("_TargetAccentPatternIntensity", Mathf.Max(0f, b.accentPatternIntensity));
            if (center)
            {
                // The rim follows the same fill hue/intensity as the petal edges.
                mat.SetColor(CenterRingColor, a.fill.linear);
                mat.SetColor(TargetCenterRingColor, b.fill.linear);
                ApplyCenterArtworkPair(mat, from, to);
            }
        }

        private void CachePaletteRevisions()
        {
            paletteRevision = paletteSet.Revision;
            for (int i = 0; i < overrideRevisions.Length; i++)
            {
                overrideRevisions[i] = PaletteFor(i).Revision;
                startupRevisions[i] = StartupRevision(i);
            }
        }

        private bool PaletteAssetsChanged()
        {
            if (!Ready) return false;
            if (paletteRevision != paletteSet.Revision) return true;
            for (int i = 0; i < overrideRevisions.Length; i++)
                if (overrideRevisions[i] != PaletteFor(i).Revision || startupRevisions[i] != StartupRevision(i)) return true;
            return false;
        }

        /// <summary>Refresh manually authored tones without restarting color or artwork waves.</summary>
        public void RefreshPaletteColors()
        {
            if (!Ready) return;
            for (int i = 0; i < runtimePetals.Length; i++)
                ApplyPair(runtimePetals[i], settled[i], targets[i], false, PaletteFor(i), StartupFor(i));
            int center = runtimePetals.Length;
            ApplyPair(runtimeCenter, settled[center], targets[center], true, paletteSet);
            CachePaletteRevisions();
        }

        private void ConfigureWave()
        {
            for (int i = 0; i <= runtimePetals.Length; i++)
            {
                var mat = i == runtimePetals.Length ? runtimeCenter : runtimePetals[i];
                mat.SetFloat(Radius, maxRadius); mat.SetFloat(Softness, activeSoftness);
            }
        }

        private void SetProgress(float value)
        {
            progress = value;
            for (int i = 0; i < runtimePetals.Length; i++) runtimePetals[i].SetFloat(Wave, value);
            runtimeCenter.SetFloat(Wave, value);
        }

        public void SelectState(int index) => TransitionTo((FlowerPaletteState)index);
        [ContextMenu("Apply Authored Appearance (Play Mode)")]
        public void ApplySelectedState() => ApplyAuthoredAppearance();

        private void OnValidate()
        {
            EnsureAppearances();
            transitionDuration = Finite(transitionDuration) ? Mathf.Max(0.01f, transitionDuration) : 1.2f;
            waveSoftness = Finite(waveSoftness) ? Mathf.Max(0.001f, waveSoftness) : 0.025f;
            ValidateCenterAppearance();
            for (int i = 0; i < PetalCount; i++)
            {
                var p = petalAppearances[i];
                p.artworkDuration = Finite(p.artworkDuration) ? Mathf.Max(0.01f, p.artworkDuration) : 1.2f;
                p.artworkSoftness = Finite(p.artworkSoftness) ? Mathf.Clamp(p.artworkSoftness, 0.001f, 0.5f) : 0.04f;
            }
            // OnValidate can run off the main thread. Defer material changes.
            appearanceDirty = true;
        }

        private void OnDisable()
        {
            transitioning = false;
            if (runtimePetals != null)
                for (int i = 0; i < runtimePetals.Length; i++)
                {
                    if (boundPetals[i] != null && boundPetals[i].sharedMaterial == runtimePetals[i]) boundPetals[i].sharedMaterial = originalPetals[i];
                    DestroyRuntime(runtimePetals[i]);
                }
            if (runtimeCenter != null)
            {
                if (boundCenter != null && boundCenter.sharedMaterial == runtimeCenter) boundCenter.sharedMaterial = originalCenter;
                DestroyRuntime(runtimeCenter);
            }
            runtimePetals = null; runtimeCenter = null; originalPetals = null; originalCenter = null;
            boundPetals = null; boundCenter = null; settled = targets = requested = null;
            artworkWaves = null;
            overrideRevisions = null;
            startupRevisions = null;
            lastAuthoredStates = null;
            // Keep consumption across disable/enable. A new game instance gets opening colors.
        }

        private static void DestroyRuntime(Material material)
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
        }
    }
}
