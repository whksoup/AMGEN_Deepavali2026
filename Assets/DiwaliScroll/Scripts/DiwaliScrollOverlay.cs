using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Three reusable players, explicit preparation, first-frame handoffs and cancellable playback.</summary>
[RequireComponent(typeof(RawImage))]
public class DiwaliScrollOverlay : MonoBehaviour
{
    public enum State { Hidden, Spawning, Shown, Hiding }
    public enum VariantOrder { Random, InOrder }
    [Serializable] public class Variant
    {
        public string name;
        public VideoClip spawnClip, idleClip, hideClip;
    }
    public Variant[] variants;
    public VariantOrder variantOrder;
    public bool showOnStart, toggleOnMouseClick;
    public bool ignoreClicksOverUI = true, queueHideDuringSpawn = true;
    public bool waitForIdleLoopEnd, preserveAspect = true;
    [Min(1)] public float prepareTimeoutSeconds = 15;
    [Min(1)] public float stallTimeoutSeconds = 5;
    public event Action Spawned, HideFinished;
    public event Action<string> Failed;
    public State CurrentState { get; private set; }
    public int CurrentVariant { get; private set; } = -1;
    public string LastError { get; private set; }
    public bool IsPrepared => loadedVariant >= 0 && spawnPrepared && idlePrepared && hidePrepared &&
        Time.frameCount > preparedFrame && spawnPlayer != null &&
        spawnPlayer.isPrepared && idlePlayer.isPrepared && hidePlayer.isPrepared;

    private RawImage image;
    private VideoPlayer spawnPlayer, idlePlayer, hidePlayer, pending, playing;
    private int loadedVariant = -1;
    private bool hideQueued, waitingToShow, preparing;
    private bool spawnPrepared, idlePrepared, hidePrepared;
    private int preparedFrame;
    private double prepareStarted, lastProgress, playbackStarted;
    private long lastFrame = -1;

    private void Awake()
    {
        image = GetComponent<RawImage>();
        image.enabled = false; image.raycastTarget = false;
        if (!ValidateVariants(out var error)) { Fail(error); return; }
        var first = variants[0].spawnClip;
        if (preserveAspect)
        {
            var fitter = GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)first.width / Mathf.Max(1, first.height);
        }
        spawnPlayer = CreatePlayer(first, false);
        idlePlayer = CreatePlayer(first, true);
        hidePlayer = CreatePlayer(first, false);
    }
    private void Start() { if (showOnStart) Show(); }
    public bool ValidateVariants(out string error)
    {
        error = null;
        if (variants == null || variants.Length == 0) { error = "No scroll variants assigned."; return false; }
        for (int i = 0; i < variants.Length; i++)
        {
            var v = variants[i];
            if (v == null || string.IsNullOrWhiteSpace(v.name) || v.spawnClip == null || v.idleClip == null || v.hideClip == null)
            { error = "Incomplete scroll variant at index " + i; return false; }
            foreach (var clip in new[] { v.spawnClip, v.idleClip, v.hideClip })
                if (clip.width != variants[0].spawnClip.width || clip.height != variants[0].spawnClip.height)
                { error = "Scroll clips must share dimensions: " + v.name; return false; }
            for (int j = 0; j < i; j++) if (string.Equals(variants[j].name, v.name, StringComparison.OrdinalIgnoreCase))
            { error = "Duplicate scroll variant: " + v.name; return false; }
        }
        return true;
    }
    public int FindVariant(string variantName) => variants == null ? -1 : Array.FindIndex(variants,
        v => v != null && string.Equals(v.name, variantName, StringComparison.OrdinalIgnoreCase));
    private VideoPlayer CreatePlayer(VideoClip first, bool loop)
    {
        var vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false; vp.isLooping = loop;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        vp.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        vp.waitForFirstFrame = true; vp.skipOnDrop = true;
        vp.targetTexture = new RenderTexture((int)first.width, (int)first.height, 0, RenderTextureFormat.ARGB32)
            { name = "Diwali Scroll Playback", hideFlags = HideFlags.DontSave };
        vp.targetTexture.Create();
        vp.prepareCompleted += OnPrepared;
        vp.frameReady += OnFrameReady; vp.loopPointReached += OnClipFinished; vp.errorReceived += OnVideoError;
        return vp;
    }
    private void OnPrepared(VideoPlayer vp)
    {
        if (!preparing || loadedVariant < 0) return;
        if (vp == spawnPlayer) spawnPrepared = true;
        if (vp == idlePlayer) idlePrepared = true;
        if (vp == hidePlayer) hidePrepared = true;
        // Start playback on a subsequent update after all three preparation callbacks have completed.
        preparedFrame = Time.frameCount;
    }
    public bool PrepareVariant(string name) => PrepareVariant(FindVariant(name));
    public bool PrepareVariant(int index)
    {
        if (!isActiveAndEnabled || CurrentState != State.Hidden) return false;
        if (spawnPlayer == null || index < 0 || index >= variants.Length)
        { Fail("Cannot prepare scroll variant " + index); return false; }
        if (loadedVariant == index && (preparing || IsPrepared)) return true;
        CancelImmediate();
        var v = variants[index];
        if (v == null || v.spawnClip == null || v.idleClip == null || v.hideClip == null)
        { Fail("Missing clips for scroll variant " + index); return false; }
        loadedVariant = index; preparing = true; prepareStarted = Time.realtimeSinceStartupAsDouble;
        spawnPlayer.clip = v.spawnClip; idlePlayer.clip = v.idleClip; hidePlayer.clip = v.hideClip;
        spawnPlayer.Prepare(); idlePlayer.Prepare(); hidePlayer.Prepare();
        return true;
    }
    public void Show(string name) => Show(FindVariant(name));
    public void Show() => Show(PickVariant());
    public void Show(int index)
    {
        if (CurrentState != State.Hidden || !PrepareVariant(index)) return;
        CurrentVariant = index; CurrentState = State.Spawning;
        hideQueued = false; waitingToShow = true;
        if (IsPrepared) BeginOpening();
    }
    public void Toggle() { if (CurrentState == State.Hidden) Show(); else Hide(); }
    public void Hide()
    {
        if (CurrentState == State.Spawning) { hideQueued = queueHideDuringSpawn; return; }
        if (CurrentState != State.Shown) return;
        if (waitForIdleLoopEnd) { hideQueued = true; return; }
        StartHide();
    }
    private void BeginOpening() { waitingToShow = false; PlayFromStart(spawnPlayer); }
    private void StartHide() { hideQueued = false; CurrentState = State.Hiding; PlayFromStart(hidePlayer); }
    private void PlayFromStart(VideoPlayer vp)
    {
        pending = playing = vp;
        lastFrame = -1; playbackStarted = lastProgress = Time.realtimeSinceStartupAsDouble;
        // CancelImmediate stops all players before each preparation, so each clip is already at its start.
        vp.sendFrameReadyEvents = true; vp.Play();
    }
    private void OnFrameReady(VideoPlayer vp, long frame)
    {
        if (!isActiveAndEnabled || vp != pending || CurrentState == State.Hidden) return;
        if ((vp == hidePlayer) != (CurrentState == State.Hiding)) return;
        pending = null; vp.sendFrameReadyEvents = false;
        image.texture = vp.targetTexture; image.enabled = true;
        foreach (var other in new[] { spawnPlayer, idlePlayer, hidePlayer })
            if (other != vp && other.isPlaying) other.Pause();
        if (vp == idlePlayer && CurrentState == State.Spawning)
        {
            CurrentState = State.Shown;
            // The idle frame is visible before notification; a subscriber can safely call Hide().
            Spawned?.Invoke();
            if (hideQueued && !waitForIdleLoopEnd && CurrentState == State.Shown) StartHide();
        }
    }
    private void OnClipFinished(VideoPlayer vp)
    {
        if (!isActiveAndEnabled || vp != playing || pending != null) return;
        if (vp == spawnPlayer && CurrentState == State.Spawning) PlayFromStart(idlePlayer);
        else if (vp == idlePlayer && CurrentState == State.Shown && hideQueued) StartHide();
        else if (vp == hidePlayer && CurrentState == State.Hiding) { CancelImmediate(); HideFinished?.Invoke(); }
    }
    private void OnVideoError(VideoPlayer vp, string error)
    {
        if (loadedVariant >= 0 && (preparing || CurrentState != State.Hidden)) Fail(variants[loadedVariant].name + ": " + error);
    }
    private int PickVariant()
    {
        if (variants == null || variants.Length == 0) return -1;
        if (variantOrder == VariantOrder.InOrder) return (CurrentVariant + 1) % variants.Length;
        if (variants.Length == 1) return 0;
        if (CurrentVariant < 0) return UnityEngine.Random.Range(0, variants.Length);
        int i = UnityEngine.Random.Range(0, variants.Length - 1);
        return i >= CurrentVariant ? i + 1 : i;
    }
    private void Update()
    {
        if (toggleOnMouseClick)
        {
#if ENABLE_INPUT_SYSTEM
            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            bool clicked = Input.GetMouseButtonDown(0);
#endif
            if (clicked && !(ignoreClicksOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) Toggle();
        }
        double now = Time.realtimeSinceStartupAsDouble;
        if (preparing)
        {
            if (IsPrepared) { preparing = false; if (waitingToShow) BeginOpening(); }
            else if (now - prepareStarted > prepareTimeoutSeconds) { Fail("Scroll preparation timed out."); return; }
        }
        if (playing == null || CurrentState == State.Hidden) return;
        if (playing.frame != lastFrame) { lastFrame = playing.frame; lastProgress = now; }
        if (now - lastProgress > stallTimeoutSeconds ||
            (!playing.isLooping && now - playbackStarted > playing.clip.length + prepareTimeoutSeconds))
            Fail("Scroll playback stalled: " + variants[CurrentVariant].name);
    }
    private void Fail(string error)
    {
        CancelImmediate(); LastError = error;
        Debug.LogError("DiwaliScrollOverlay: " + error, this); Failed?.Invoke(error);
    }
    public void CancelImmediate()
    {
        pending = playing = null; waitingToShow = preparing = hideQueued = false;
        spawnPrepared = idlePrepared = hidePrepared = false;
        loadedVariant = -1; CurrentState = State.Hidden; LastError = null;
        if (image != null) { image.enabled = false; image.texture = null; }
        foreach (var vp in new[] { spawnPlayer, idlePlayer, hidePlayer })
            if (vp != null) { vp.sendFrameReadyEvents = false; vp.Stop(); }
    }
    private void OnDisable() => CancelImmediate();
    private void OnDestroy()
    {
        foreach (var vp in new[] { spawnPlayer, idlePlayer, hidePlayer })
        {
            if (vp == null) continue;
            vp.prepareCompleted -= OnPrepared;
            vp.frameReady -= OnFrameReady; vp.loopPointReached -= OnClipFinished; vp.errorReceived -= OnVideoError;
            if (vp.targetTexture != null) { vp.targetTexture.Release(); Destroy(vp.targetTexture); }
        }
    }
}
