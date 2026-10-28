using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class LaunchSceneController : MonoBehaviour
{
    public static LaunchSceneController Instance { get; private set; }
    // An active experience can own its gradual inactivity return instead of the host's screensaver timeout.
    public Behaviour InactivityOwner { get; set; }

    private Controls controls;

    [Serializable]
    public struct SceneButtonDetails
    {
        public string key;
        public string label;
        public Texture2D icon;
    }

    [SerializeField]
    private static GameObject launcherPanel;

    [SerializeField]
    private List<SceneButtonDetails> sceneList = new List<SceneButtonDetails>();

    [SerializeField]
    private SceneButton buttonPrefab;
    [SerializeField]
    private Transform buttonContainer;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;   // drag your .inputactions here
    [SerializeField] private string actionMapName = "UI";     // set to your map name
    [SerializeField] private string actionName = "ChangeScene"; // set to your action name

    [Header("Scenes")]
    [SerializeField] private string scene1 = "Scene1";
    [SerializeField] private string scene2 = "Scene2";
    [SerializeField] private string scene3 = "Scene3";
    [SerializeField] private List<string> screenSaverList = new List<string>();

    [Header("Transition Settings")]
    [SerializeField] private GameObject transitionPanel;
    private CanvasGroup transitionPanelCanvasGroup;
    [SerializeField] float transitionDuration = 0.5f;
    [Header("Controls")]
    [SerializeField] private float timeToSwitch = 15.0f;
    [SerializeField] private float timeToCloseLauncher = 10.0f;
    private static float timeSinceLastInput = 0.0f;

    [Header("Setting")]
    [SerializeField] private SelectionControl selectionControl;

    private InputAction changeSceneAction;
    private InputAction anyInputAction;

    private int currentScreenSaverIndex = -1;
    private bool launcherCloseFired = false;
    private VideoManager videoManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        controls = new Controls(); // Generated from your input actions asset
        timeSinceLastInput = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        // If this is not the first copy, destroy it
        var existing = FindObjectsByType<LaunchSceneController>(FindObjectsSortMode.None);
        if (existing.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        launcherPanel = GameObject.Find("Launcher Canvas");
        transitionPanelCanvasGroup = transitionPanel.GetComponent<CanvasGroup>();
        timeSinceLastInput = 0;
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            var pi = GetComponent<PlayerInput>();
            if (pi != null) inputActions = pi.actions;
        }

        var map = inputActions?.FindActionMap(actionMapName, throwIfNotFound: false);
        changeSceneAction = map?.FindAction(actionName, throwIfNotFound: false);

        if (changeSceneAction != null)
        {
            changeSceneAction.performed += OnChangeScene;
            changeSceneAction.Enable();
        }

        anyInputAction = new InputAction("AnyTouch", binding: "<Touchscreen>/press");
        anyInputAction.performed += OnAnyInput;
        anyInputAction.Enable();


        //selectionControl.Open();
    }

    private void OnDisable()
    {
        if (changeSceneAction != null)
        {
            changeSceneAction.performed -= OnChangeScene;
            changeSceneAction.Disable();
        }

        if (anyInputAction != null)
        {
            anyInputAction.performed -= OnAnyInput;
            anyInputAction.Disable();
            anyInputAction.Dispose();
            anyInputAction = null;
        }
    }

    private void OnAnyInput(InputAction.CallbackContext ctx)
    {
        RegisterUserInteraction();
    }
    // Update is called once per frame
    void Update()
    {
        if (InactivityOwner != null && InactivityOwner.isActiveAndEnabled)
        {
            timeSinceLastInput = 0;
            launcherCloseFired = false;
            return;
        }
        var activeSceneName = SceneManager.GetActiveScene().name;

        // Skip idle handling while we are already in a screensaver scene
        if (activeSceneName == "Video Player ScreenSaver")
        {
            return;
        }

        // Suppress idle transitions while a video is actively playing
        if (activeSceneName == "VideoPlayer" && IsVideoCurrentlyPlaying())
        {
            timeSinceLastInput = 0f;
            return;
        }

        timeSinceLastInput += Time.deltaTime;

        if (!launcherCloseFired && timeSinceLastInput >= timeToCloseLauncher)
        {
            launcherCloseFired = true; // <- fire once
            Debug.Log("Closing launcher due to inactivity");
            selectionControl.RevserveBackToTouchButton();
        }

        if(timeSinceLastInput >= timeToSwitch)
        {
            GoToNextScreenSaver();
        }
    }

    private bool IsVideoCurrentlyPlaying()
    {
        if (videoManager == null)
        {
            videoManager = FindAnyObjectByType<VideoManager>();
        }

        if (videoManager == null)
        {
            return false;
        }

        var player = videoManager.videoPlayer;
        if (player == null)
        {
            return false;
        }

        return player.isPlaying;
    }

    public void GoToNextScreenSaver()
    {
        currentScreenSaverIndex += 1;
        currentScreenSaverIndex = currentScreenSaverIndex % screenSaverList.Count;
        StartCoroutine(OnChangeScene(screenSaverList[currentScreenSaverIndex]));
    }

    bool CheckIfScreenSaver(string sceneName)
    {
        return screenSaverList.Contains(sceneName);

    }

    private void OnChangeScene(InputAction.CallbackContext ctx)
    {
        var path = ctx.control?.path ?? "";   // e.g. "<Keyboard>/1"
        if (path.EndsWith("/1")) SceneManager.LoadScene(scene1);
        else if (path.EndsWith("/2")) SceneManager.LoadScene(scene2);
        else if (path.EndsWith("/3")) SceneManager.LoadScene(scene3);
    }

    public void TriggerChangeScene(string sceneName)
    {
        StartCoroutine(OnChangeScene(sceneName));
    }

    public IEnumerator OnChangeScene(string sceneName)
    {
        timeSinceLastInput = 0;
        videoManager = null;

        yield return DOTween
            .To(() => transitionPanelCanvasGroup.alpha, a => transitionPanelCanvasGroup.alpha = a, 1f, transitionDuration)
            .SetUpdate(true)
            .WaitForCompletion();

        if (launcherPanel == null)
        {
            launcherPanel = GameObject.Find("Launcher Canvas");
        }
        if (sceneName == "Startup")
        {
            // If going back to startup, just show the panel
            launcherPanel.SetActive(true);
            selectionControl.RestartOpening();
        }
        else
        {
            //launcherPanel.SetActive(false);
            //Check the scene to determine if we hide or show the launcher panel
            if(sceneName == "ScreenSaver" ||
                sceneName == "Video Player ScreenSaver" ||
                sceneName == "Warning Screensaver" ||
                sceneName == "Signature Wall ScreenSaver")
            {
                launcherPanel.SetActive(true);
                selectionControl.ResetLauncher();
            }
            else
            {
                launcherPanel.SetActive(false);
            }

        }
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) 
            yield return null;

        // Optionally wait one frame so new scene UI is ready
        yield return null;

        yield return transitionPanelCanvasGroup.DOFade(0f, transitionDuration).SetUpdate(true).WaitForCompletion();
        transitionPanelCanvasGroup.blocksRaycasts = false;

    }

    public static void RegisterUserInteraction()
    {
        Instance?.ResetIdleTimer();
    }
    private void ResetIdleTimer()
    {
        timeSinceLastInput = 0f;
        launcherCloseFired = false;
        Debug.Log("Idle timer reset due to user input.");
    }
}
