using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoPlayerScreenSaverController : MonoBehaviour
{
    [Header("Video Setup")]
    [SerializeField] private VideoPlayer videoPlayer; // Assign in Inspector
    LaunchSceneController controller;
    private void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            // Subscribe to video end event
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        else
        {
            Debug.LogWarning("VideoPlayer not assigned!");
        }

        controller = GameObject.FindFirstObjectByType<LaunchSceneController>();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        controller?.GoToNextScreenSaver();
    }

    private void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }
}
