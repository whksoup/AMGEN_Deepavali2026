using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Full-screen, transparent "Happy Deepavali" title played from three VP8 + alpha WebM clips:
///   spawn (title is written, plays once)  ->  idle (loops while shown)  ->  hide (fades out, ends empty).
/// The logo under the title is not in the clips: it is a separate Raw Image (see logo) that this component
/// fades in, rises into place and fades out in sync with the clips, so it can be swapped and resized freely.
///
/// Setup: GameObject > UI > Happy Deepavali Title (or Tools > Happy Deepavali > Add Title To Scene).
///   Left mouse click shows the title when hidden and hides it when shown.
///   From code, call Show(), Hide() or Toggle().
/// </summary>
[RequireComponent(typeof(RawImage))]
public class HappyDeepavaliOverlay : MonoBehaviour
{
    public enum State { Hidden, Spawning, Shown, Hiding }

    [Header("Clips (VP8 WebM, Keep Alpha)")]
    public VideoClip spawnClip;
    public VideoClip idleClip;
    public VideoClip hideClip;

    [Header("Logo (drag logo.png onto its Texture; set its Width / Height to resize)")]
    public RawImage logo;
    [Tooltip("Seconds into the spawn clip when the logo starts to fade in.")]
    public float logoFadeInStart = 3.9f;
    public float logoFadeInDuration = 1.0f;
    [Tooltip("How far below its place the logo starts, in 2560 x 1118 reference pixels.")]
    public float logoRiseDistance = 28f;
    [Tooltip("Seconds the logo takes to fade out at the start of the hide clip.")]
    public float logoFadeOutDuration = 0.7f;

    [Header("Behaviour")]
    [Tooltip("Show the title as soon as the scene starts.")]
    public bool showOnStart = false;
    [Tooltip("Left click shows the title when hidden and hides it when shown.")]
    public bool toggleOnMouseClick = true;
    [Tooltip("Ignore clicks that land on other UI elements (buttons etc.).")]
    public bool ignoreClicksOverUI = false;
    [Tooltip("A click while the title is being written hides it as soon as the writing finishes.")]
    public bool queueHideDuringSpawn = true;
    [Tooltip("Keep the clip's aspect ratio when the screen is not 2560x1118.")]
    public bool preserveAspect = true;

    public event Action Spawned;   // writing finished, idle loop running
    public event Action HideFinished;   // hide finished, overlay invisible

    public State CurrentState { get; private set; } = State.Hidden;
    public string LastError { get; private set; }
    [Min(1)] public float playbackTimeoutSeconds = 15;

    RawImage image;
    VideoPlayer spawnPlayer, idlePlayer, hidePlayer;
    VideoPlayer pending;   // player whose first frame we are waiting for before it is put on screen
    bool hideQueued;
    Vector2 logoRest;
    double lastProgress, playbackStarted;
    long lastFrame = -1;

    void Awake()
    {
        image = GetComponent<RawImage>();
        image.enabled = false;
        image.raycastTarget = false;

        var rt = (RectTransform)transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        if (spawnClip == null || idleClip == null || hideClip == null)
        {
            Debug.LogError("HappyDeepavaliOverlay: assign the spawn, idle and hide clips.", this);
            enabled = false;
            return;
        }
        if (preserveAspect)
        {
            var fitter = GetComponent<AspectRatioFitter>();
            if (!fitter) fitter = gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)spawnClip.width / spawnClip.height;
        }

        spawnPlayer = CreatePlayer(spawnClip, false);
        idlePlayer = CreatePlayer(idleClip, true);
        hidePlayer = CreatePlayer(hideClip, false);

        spawnPlayer.loopPointReached += OnClipFinished;
        hidePlayer.loopPointReached += OnClipFinished;

        if (logo)
        {
            logo.raycastTarget = false;
            logoRest = logo.rectTransform.anchoredPosition;
            ApplyLogo(0f, 1f);
        }
    }

    void Start()
    {
        if (showOnStart) Show();
    }

    VideoPlayer CreatePlayer(VideoClip clip, bool loop)
    {
        var vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = loop;
        vp.clip = clip;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        vp.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        vp.waitForFirstFrame = true;
        vp.skipOnDrop = true;
        var tex = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32);
        tex.Create();
        vp.targetTexture = tex;
        vp.frameReady += OnFrameReady;
        vp.errorReceived += OnVideoError;
        vp.Prepare();   // decode ahead of time so switching clips is instant
        return vp;
    }

    void Update()
    {
        if (toggleOnMouseClick && ClickedThisFrame()) Toggle();
        if (CurrentState == State.Hidden) return;
        var active = pending != null ? pending : CurrentState == State.Spawning ? spawnPlayer :
            CurrentState == State.Hiding ? hidePlayer : idlePlayer;
        double now = Time.realtimeSinceStartupAsDouble;
        if (active.frame != lastFrame) { lastFrame = active.frame; lastProgress = now; }
        float timeout = float.IsFinite(playbackTimeoutSeconds) ? Mathf.Max(1, playbackTimeoutSeconds) : 15;
        if (now - lastProgress > timeout ||
            (CurrentState != State.Shown && now - playbackStarted > active.clip.length + timeout))
            Fail("Happy Deepavali playback timed out.");
    }

    void LateUpdate()
    {
        if (!logo || spawnPlayer == null) return;
        switch (CurrentState)
        {
            case State.Spawning:
                float p = Mathf.Clamp01(((float)spawnPlayer.time - logoFadeInStart) / Mathf.Max(.01f, logoFadeInDuration));
                float e = 1f - Mathf.Pow(1f - p, 3f);   // ease out
                ApplyLogo(e, 1f - e);
                break;
            case State.Shown:
                ApplyLogo(1f, 0f);
                break;
            case State.Hiding:
                float h = Mathf.Clamp01((float)hidePlayer.time / Mathf.Max(.01f, logoFadeOutDuration));
                ApplyLogo(1f - (1f - Mathf.Cos(Mathf.PI * h)) / 2f, 0f);   // ease in-out
                break;
            default:
                ApplyLogo(0f, 1f);
                break;
        }
    }

    void ApplyLogo(float alpha, float drop)
    {
        var c = logo.color;
        c.a = alpha;
        logo.color = c;
        logo.enabled = alpha > 0f && logo.texture != null;
        logo.rectTransform.anchoredPosition = logoRest - new Vector2(0f, drop * logoRiseDistance);
    }

    bool ClickedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        bool clicked = Input.GetMouseButtonDown(0);
#endif
        if (clicked && ignoreClicksOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;
        return clicked;
    }

    public void Toggle()
    {
        if (CurrentState == State.Hidden) Show();
        else Hide();
    }

    /// <summary>Write the title. Ignored unless it is fully hidden.</summary>
    public void Show()
    {
        if (!isActiveAndEnabled || CurrentState != State.Hidden || spawnPlayer == null) return;
        LastError = null;
        hideQueued = false;
        CurrentState = State.Spawning;
        PlayFromStart(spawnPlayer);
    }

    /// <summary>Fade the title and logo out.</summary>
    public void Hide()
    {
        if (CurrentState == State.Spawning)
        {
            if (queueHideDuringSpawn) hideQueued = true;
            return;
        }
        if (CurrentState != State.Shown) return;
        StartHide();
    }

    void StartHide()
    {
        hideQueued = false;
        CurrentState = State.Hiding;
        PlayFromStart(hidePlayer);
    }

    void OnSpawnFinished()
    {
        PlayFromStart(idlePlayer);
    }

    void OnClipFinished(VideoPlayer vp)
    {
        if (!isActiveAndEnabled || pending != null) return;
        if (vp == spawnPlayer && CurrentState == State.Spawning) OnSpawnFinished();
        else if (vp == hidePlayer && CurrentState == State.Hiding) OnHideFinished();
    }

    void OnHideFinished()
    {
        CancelImmediate();
        HideFinished?.Invoke();
    }

    void PlayFromStart(VideoPlayer vp)
    {
        // Keep the previous clip on screen until this one has decoded its first frame,
        // so there is never an empty or stale frame between clips.
        pending = vp;
        lastFrame = -1;
        playbackStarted = lastProgress = Time.realtimeSinceStartupAsDouble;
        vp.sendFrameReadyEvents = true;
        if (vp.isPrepared) vp.frame = 0;
        vp.Play();
    }

    void OnFrameReady(VideoPlayer vp, long frame)
    {
        if (!isActiveAndEnabled || CurrentState == State.Hidden || vp != pending) return;
        pending = null;
        vp.sendFrameReadyEvents = false;
        image.texture = vp.targetTexture;
        image.enabled = true;
        if (vp != spawnPlayer) Rewind(spawnPlayer);
        if (vp != idlePlayer) Rewind(idlePlayer);
        if (vp == idlePlayer && CurrentState == State.Spawning)
        {
            CurrentState = State.Shown;
            Spawned?.Invoke();
            if (hideQueued && CurrentState == State.Shown) StartHide();
        }
    }

    static void Rewind(VideoPlayer vp)
    {
        // Pause rather than Stop: Stop releases the decoder and the next Play would stall while re-preparing.
        if (!vp.isPrepared) return;
        vp.Pause();
        vp.frame = 0;
    }

    void OnVideoError(VideoPlayer vp, string error)
    {
        if (CurrentState != State.Hidden) Fail(error);
    }

    void Fail(string error)
    {
        CancelImmediate();
        LastError = error;
        Debug.LogError("HappyDeepavaliOverlay: " + error, this);
    }

    public void CancelImmediate()
    {
        pending = null;
        hideQueued = false;
        CurrentState = State.Hidden;
        LastError = null;
        if (image != null) { image.enabled = false; image.texture = null; }
        if (logo != null) ApplyLogo(0, 1);
        foreach (var vp in new[] { spawnPlayer, idlePlayer, hidePlayer })
            if (vp != null) { vp.sendFrameReadyEvents = false; vp.Stop(); }
    }

    void OnDisable() => CancelImmediate();

    void OnDestroy()
    {
        foreach (var vp in new[] { spawnPlayer, idlePlayer, hidePlayer })
        {
            if (vp != null)
            {
                vp.frameReady -= OnFrameReady;
                vp.errorReceived -= OnVideoError;
                vp.loopPointReached -= OnClipFinished;
            }
            if (vp == null || vp.targetTexture == null) continue;
            vp.targetTexture.Release();
            Destroy(vp.targetTexture);
        }
    }
}
