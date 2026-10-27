using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DG.Tweening;

public enum TouchAction { Stop, Skip, None }

[Serializable]
public class VideoDetails
{
    public string label;
    public string videoName;
    public Texture2D thumbnail;
}

public class VideoManager : MonoBehaviour
{
    [Header("Prefabs & Refs")]
    [SerializeField] private GameObject videoButtonPrefabs;
    [SerializeField] private Transform videoButtonContainer;
    [SerializeField] private VideoControlPanel videoControlPanel;
    [SerializeField] private GameObject exitButton;
    [SerializeField] private GameObject blackPanel;

    [Header("Video")]
    public VideoPlayer videoPlayer;
    private RawImage videoScreen;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI debugText;

    [SerializeField] private string videoFilePath = "/Videos/";
    [SerializeField] public TouchAction touchAction = TouchAction.Stop;

    public string videoFile { get; private set; }

    [SerializeField] public bool repeatingLoop = false;
    [SerializeField] public float loopingTimer = 0;
    [SerializeField] public List<VideoDetails> videos = new List<VideoDetails>();

    [SerializeField] public float fadeInTime = 1f;
    [SerializeField] public float fadeOutTime = 1f;

    float currentTimer = 0f;
    bool isPlaying = false;
    bool hasEnded = false;
    bool isDebug = false;
    bool instantSkip;

    [Header("Video Control UI Auto-Fade")]
    [SerializeField] float fadeTimer = 2.0f;
    float currentFadeTimer = 0f;

    // -------------------- Carousel Settings --------------------
    [Header("Carousel Layout")]
    [SerializeField] private int visibleCount = 5;         // center ±2
    [SerializeField] private float spacing = 280f;         // edge-to-edge gap
    [SerializeField] private float yOffset = 0f;

    [Header("Carousel Scales")]
    [SerializeField] private float centerScale = 1.15f;
    [SerializeField] private float nearScale = 0.9f;       // offset ±1
    [SerializeField] private float farScale = 0.75f;       // offset ±2
    [SerializeField] private float hiddenScale = 0.6f;     // outside visible window

    [Header("Carousel Animation")]
    [SerializeField] private float tweenDuration = 0.35f;
    [SerializeField] private float staggerPerItem = 0.01f;
    [SerializeField] private Ease tweenEase = Ease.OutCubic;

    private readonly List<RectTransform> itemRects = new();
    private readonly List<Button> itemButtons = new();
    private int currentIndex = 0;
    private bool isCentering = false;

    // -------------------- Fullscreen Zoom State --------------------
    bool inFullscreen = false;
    int zoomedIndex = -1;

    // -------------------- Unity Lifecycle --------------------
    void Awake()
    {
        if (videoButtonContainer is RectTransform crt)
        {
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    void Start()
    {
        videoPlayer = GameObject.Find("Video Player")?.GetComponent<VideoPlayer>();
        videoPlayer.skipOnDrop = true;

        debugText = GameObject.Find("Video Debug Text")?.GetComponent<TextMeshProUGUI>();
        if (debugText && !isDebug) debugText.gameObject.SetActive(false);

        videoScreen = videoPlayer.GetComponent<RawImage>();
        canvasGroup = videoScreen.gameObject.GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = videoScreen.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        if (videoPlayer.targetTexture != null)
            videoPlayer.targetTexture.Release();

        isPlaying = false;
        blackPanel.SetActive(false);

        SetupVideoButtons();
        StartCoroutine(InitCarouselAfterLayout());
    }

    IEnumerator InitCarouselAfterLayout()
    {
        yield return null; // wait one frame
        var crt = videoButtonContainer as RectTransform;
        if (crt) LayoutRebuilder.ForceRebuildLayoutImmediate(crt);
        LayoutCarousel(false);
    }

    void SetupVideoButtons()
    {
        itemRects.Clear();
        itemButtons.Clear();

        for (int i = 0; i < videos.Count; i++)
        {
            var vd = videos[i];
            GameObject item = Instantiate(videoButtonPrefabs, videoButtonContainer, false);

            var rt = item.transform as RectTransform;
            if (!rt) continue;

            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            if (rt.sizeDelta.x <= 0f) rt.sizeDelta = new Vector2(750f, 435f);

            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one * hiddenScale;

            var vsb = item.GetComponentInChildren<VideoSelectButton>();
            if (vsb != null) 
                vsb.Setup(vd, this);

            var btn = item.GetComponentInChildren<Button>();
            int capturedIndex = i;
            btn.onClick.AddListener(() => OnCarouselItemClicked(capturedIndex));

            itemRects.Add(rt);
            itemButtons.Add(btn);
        }

        if (videoControlPanel != null)
        {
            videoControlPanel.leftButton.onLongPress.AddListener(ShowVideoButton);
            videoControlPanel.rightButton.onLongPress.AddListener(PauseVideo);
            videoControlPanel.gameObject.SetActive(false);
        }
    }

    // -------------------- Carousel Clicks --------------------
    void OnCarouselItemClicked(int index)
    {
        if (isCentering) return;

        if (index == currentIndex)
        {
            AnimateToFullscreenAndPlay(index);
            return;
        }

        isCentering = true;
        currentIndex = index;
        LayoutCarousel(false);
        DOVirtual.DelayedCall(tweenDuration + 0.02f, () => { isCentering = false; });
    }

    // -------------------- Fullscreen Zoom -> Play --------------------
    void AnimateToFullscreenAndPlay(int index)
    {
        if (index < 0 || index >= itemRects.Count) return;
        if (index != currentIndex) return;

        inFullscreen = true;
        zoomedIndex = index;

        var rootCanvas = videoButtonContainer.GetComponentInParent<Canvas>();
        var rootRT = rootCanvas ? rootCanvas.GetComponent<RectTransform>() : null;
        float canvasWidth = rootRT ? rootRT.rect.width : Screen.width;

        var rt = itemRects[index];
        var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();

        float baseWidth = rt.sizeDelta.x;
        float targetScale = canvasWidth / baseWidth * 1.25f;

        for (int i = 0; i < itemRects.Count; i++)
        {
            if (i == index) continue;
            var other = itemRects[i];
            var ocg = other.GetComponent<CanvasGroup>() ?? other.gameObject.AddComponent<CanvasGroup>();
            other.DOScale(hiddenScale, 0.2f).SetEase(Ease.OutQuad).SetLink(other.gameObject);
            ocg.DOFade(0f, 0.2f).SetEase(Ease.OutQuad).SetLink(other.gameObject);
            if (i < itemButtons.Count) itemButtons[i].interactable = false;
        }

        rt.SetAsLastSibling();
        DOTween.Kill(rt);

        var seq = DOTween.Sequence();
        seq.Join(rt.DOAnchorPos(Vector2.zero, 0.35f).SetEase(Ease.OutCubic).SetLink(rt.gameObject));
        seq.Join(rt.DOScale(targetScale, 0.4f).SetEase(Ease.OutCubic).SetLink(rt.gameObject));
        seq.OnComplete(() =>
        {
            blackPanel.SetActive(true);
            videoPlayer.loopPointReached -= OnVideoEnd;
            videoPlayer.loopPointReached += OnVideoEnd;
            PlayVideo(videos[index]);
        });
    }

    // -------------------- Carousel Layout (CoverFlow) --------------------
    void LayoutCarousel(bool instant)
    {
        if (itemRects.Count == 0) return;

        int n = itemRects.Count;
        int halfWindow = Mathf.Min(2, (n - 1) / 2);
        float baseWidth = itemRects[0].sizeDelta.x;
        const float zPerStep = 40f;

        for (int i = 0; i < n; i++)
        {
            int offset = CircularDelta(i, currentIndex, n);
            bool visible = Mathf.Abs(offset) <= halfWindow;

            RectTransform rt = itemRects[i];
            CanvasGroup cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();

            float targetScale =
                (offset == 0) ? centerScale :
                (Mathf.Abs(offset) == 1) ? nearScale :
                (Mathf.Abs(offset) == 2) ? farScale : hiddenScale;

            float targetX = 0f;
            if (offset != 0)
            {
                int dir = (offset > 0) ? 1 : -1;
                float x = 0f;

                for (int step = 0; step < Mathf.Abs(offset); step++)
                {
                    int neighborIndex = currentIndex + (step * dir);
                    int neighborOffset = CircularDelta(neighborIndex, currentIndex, n);

                    float neighborScale =
                        (neighborOffset == 0) ? centerScale :
                        (Mathf.Abs(neighborOffset) == 1) ? nearScale :
                        (Mathf.Abs(neighborOffset) == 2) ? farScale : hiddenScale;

                    float neighborWidth = baseWidth * neighborScale;
                    float thisWidth = baseWidth * targetScale;

                    x += (neighborWidth * 0.5f) + (thisWidth * 0.5f) + spacing;
                }
                targetX = x * dir;
            }
            float targetY = yOffset;

            float targetRotY = 0f;
            if (offset < 0) targetRotY = (Mathf.Abs(offset) == 1) ? 25f : 45f;
            else if (offset > 0) targetRotY = (Mathf.Abs(offset) == 1) ? -25f : -45f;
            Quaternion targetRotation = Quaternion.Euler(0f, targetRotY, 0f);

            float targetZ = -Mathf.Abs(offset) * zPerStep;
            float targetAlpha = visible ? 1f : 0f;

            int depthBase = 100 - Mathf.Abs(offset) * 10;
            if (offset < 0) depthBase -= 1;
            rt.SetSiblingIndex(Mathf.Clamp(depthBase, 0, n - 1));

            if (instant)
            {
                rt.anchoredPosition = new Vector2(targetX, targetY);
                var lp = rt.localPosition;
                rt.localPosition = new Vector3(lp.x, lp.y, targetZ);
                rt.localScale = Vector3.one * targetScale;
                rt.localRotation = targetRotation;
                cg.alpha = targetAlpha;
            }
            else
            {
                float delay = Mathf.Abs(offset) * staggerPerItem;

                rt.DOAnchorPos(new Vector2(targetX, targetY), tweenDuration)
                  .SetEase(tweenEase).SetDelay(delay).SetLink(rt.gameObject);

                rt.DOLocalMoveZ(targetZ, tweenDuration)
                  .SetEase(tweenEase).SetDelay(delay).SetLink(rt.gameObject);

                rt.DOScale(targetScale, tweenDuration)
                  .SetEase(tweenEase).SetDelay(delay).SetLink(rt.gameObject);

                rt.DOLocalRotateQuaternion(targetRotation, tweenDuration)
                  .SetEase(tweenEase).SetDelay(delay).SetLink(rt.gameObject);

                cg.DOFade(targetAlpha, tweenDuration * 0.9f)
                  .SetEase(Ease.OutQuad).SetDelay(delay * 0.7f).SetLink(rt.gameObject);
            }

            if (itemButtons[i].TryGetComponent<CanvasGroup>(out var btnCg))
            {
                btnCg.blocksRaycasts = visible;
                btnCg.interactable = visible;
            }
            else
            {
                itemButtons[i].interactable = visible;
            }
        }

        if (currentIndex >= 0 && currentIndex < itemRects.Count)
            itemRects[currentIndex].SetAsLastSibling();
    }

    static int CircularDelta(int i, int current, int n)
    {
        int raw = i - current;
        int half = n / 2;
        if (raw > half) raw -= n;
        if (raw < -half) raw += n;
        return raw;
    }

    void ResetCarouselState()
    {
        inFullscreen = false;
        zoomedIndex = -1;

        for (int i = 0; i < itemRects.Count; i++)
        {
            var rt = itemRects[i];
            DOTween.Kill(rt);

            rt.localScale = Vector3.one * hiddenScale;

            var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            if (i < itemButtons.Count) itemButtons[i].interactable = true;
        }
    }

    void HideVideoButtons()
    {
        videoButtonContainer.gameObject.SetActive(false);
    }

    public void ShowVideoButton(Vector2 _)
    {
        ReturnToMenu();
    }

    void Update()
    {
        currentFadeTimer += Time.deltaTime;

        if (Input.touchCount > 0)
        {
            currentFadeTimer = 0f;
            if (videoControlPanel) videoControlPanel.Show();
        }

        if (currentFadeTimer > fadeTimer && videoControlPanel)
        {
            videoControlPanel.Fade();
        }
    }

    // -------------------- Video Flow --------------------
    public void PlayVideo(VideoDetails vd)
    {
        string path = LoadVideo(vd.videoName);
        if (string.IsNullOrEmpty(path)) return;

        videoFile = path;
        videoPlayer.url = videoFile;
        videoPlayer.isLooping = repeatingLoop;

        videoPlayer.loopPointReached -= OnVideoEnd;
        videoPlayer.loopPointReached += OnVideoEnd;

        HideVideoButtons();
        if (videoControlPanel) 
            videoControlPanel.gameObject.SetActive(true);

        if (exitButton) 
            exitButton.SetActive(false);
        PlayVideo();
    }

    void PlayVideo()
    {
        canvasGroup.alpha = 1f;
        videoPlayer.Play();
        isPlaying = true;
    }

    public void PauseVideo(Vector2 _)
    {
        if (!videoControlPanel) return;

        videoControlPanel.Switch();
        if (videoControlPanel.IsPlay) videoPlayer.Pause();
        else videoPlayer.Play();
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        ReturnToMenu();
    }

    string LoadVideo(string file)
    {
        string executableDir = Path.GetDirectoryName(Application.streamingAssetsPath + videoFilePath);
        string videoPath = Path.Combine(executableDir, file.TrimStart('/'));

        if (File.Exists(videoPath))
        {
            Debug.Log("Video file found: " + videoPath);
            return videoPath;
        }
        else
        {
            Debug.LogError("Video file not found: " + videoPath);
            return "";
        }
    }

    void FadeIn()
    {
        canvasGroup.DOFade(1f, fadeInTime).SetEase(Ease.InOutQuad);
    }

    void FadeOut(TweenCallback onComplete)
    {
        canvasGroup.DOFade(0f, fadeOutTime).SetEase(Ease.InOutQuad).OnComplete(onComplete);
    }

    public void FadeOutInstant()
    {
        videoPlayer.Stop();
        canvasGroup.alpha = 0f;
        isPlaying = false;
        currentTimer = 0f;
    }

    private void ReturnToMenu()
    {
        if (videoControlPanel) videoControlPanel.gameObject.SetActive(false);
        if (exitButton)
            exitButton.SetActive(true);
        FadeOut(() =>
        {
            videoPlayer.Stop();
            isPlaying = false;

            ResetCarouselState();

            videoButtonContainer.gameObject.SetActive(true);
            LayoutCarousel(false);
        });

        blackPanel.SetActive(false);
    }
}
