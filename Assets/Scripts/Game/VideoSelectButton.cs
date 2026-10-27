using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VideoSelectButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI videoLabel;
    [SerializeField] private Button button; // assign in Inspector if not auto-detected
    [SerializeField] private RawImage image;
    private string videoName;
    private VideoDetails videoDetails;
    private VideoManager videoManager;
    [SerializeField]
    private TouchAreaButton touchArea;

    public VideoManager Manager
    {
        get => videoManager;
        set => videoManager = value;
    }

    private void Awake()
    {
        if (!button) button = GetComponent<Button>();
        touchArea = GetComponent<TouchAreaButton>();
        
    }

    private void OnEnable()
    {
        if (Application.isPlaying && button != null)
            button.onClick.AddListener(PlayVideo);

        touchArea = GetComponent<TouchAreaButton>();
        if (touchArea != null)
            touchArea.onLongPress.AddListener(PlayVideo);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayVideo);

        if (touchArea != null)
            touchArea.onLongPress.RemoveListener(PlayVideo);
    }

    public void Setup(VideoDetails details, VideoManager vm)
    {
        videoDetails = details;
        videoName = details.videoName;
        videoManager = vm;
        image.texture = details.thumbnail;

        if (videoLabel != null)
            videoLabel.text = details.label;

        // NOTE:
        // The RawImage on this prefab will already have your default sprite/texture set in Inspector.
        // Nothing else is required here — you manage your own images.
    }

    private void PlayVideo()
    {
        if (videoManager != null && videoDetails != null)
        {
            Debug.Log("Play video: " + videoDetails.videoName);
            // Un-comment if you want to directly play:
            // videoManager.PlayVideo(videoDetails);
        }
        else
        {
            Debug.LogWarning("VideoManager or VideoDetails not set on VideoSelectButton.");
        }
    }

    private void PlayVideo(Vector2 _)
    {
        PlayVideo();
    }
}
