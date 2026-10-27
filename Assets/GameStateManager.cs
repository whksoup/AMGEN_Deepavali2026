using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for CanvasGroup

public class GameStateManager : MonoBehaviour
{
    [System.Serializable]
    public class GameState
    {
        public string stateName;
        public GameObject[] objectsToActivate;
        public GameObject[] objectsToDeactivate;
        [Header("Fade Settings")]
        public bool useFade = true;
        public float fadeDuration = 0.5f;
    }

    [Header("Game States")]
    public List<GameState> gameStates = new List<GameState>();
    
    [Header("UI Fade Manager")]
    public UIFadeManager uiFadeManager;
    
    [SerializeField] private string currentStateName;
    private Dictionary<string, GameState> stateDictionary = new Dictionary<string, GameState>();

    private void Awake()
    {
        foreach (var state in gameStates)
        {
            stateDictionary.Add(state.stateName, state);
        }
    }

    public void SwitchState(string newStateName)
    {
        if (!stateDictionary.ContainsKey(newStateName)) return;

        StartCoroutine(TransitionStates(newStateName));
    }

    private IEnumerator TransitionStates(string newStateName)
    {
        // Handle current state exit
        if (!string.IsNullOrEmpty(currentStateName))
        {
            var currentState = stateDictionary[currentStateName];
            
            if (currentState.useFade)
            {
                yield return StartCoroutine(FadeObjects(currentState.objectsToActivate, 1, 0, currentState.fadeDuration));
            }
            
            SetObjectsActive(currentState.objectsToActivate, false);
            SetObjectsActive(currentState.objectsToDeactivate, true);
        }

        // Handle new state enter
        var newState = stateDictionary[newStateName];
        SetObjectsActive(newState.objectsToDeactivate, false);
        SetObjectsActive(newState.objectsToActivate, true);
        
        if (newState.useFade)
        {
            yield return StartCoroutine(FadeObjects(newState.objectsToActivate, 0, 1, newState.fadeDuration));
        }

        currentStateName = newStateName;
    }

    private IEnumerator FadeObjects(GameObject[] objects, float startAlpha, float endAlpha, float duration)
    {
        List<CanvasGroup> canvasGroups = new List<CanvasGroup>();
        
        // Ensure all objects have CanvasGroup components
        foreach (var obj in objects)
        {
            if (obj == null) continue;
            
            var cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.AddComponent<CanvasGroup>();
            cg.alpha = startAlpha;
            canvasGroups.Add(cg);
        }

        // Perform fade
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            foreach (var cg in canvasGroups)
            {
                cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            }
            yield return null;
        }

        // Finalize alpha values
        foreach (var cg in canvasGroups)
        {
            cg.alpha = endAlpha;
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        foreach (var obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
                
                // Ensure CanvasGroup doesn't interfere with activation
                var cg = obj.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.interactable = active;
                    cg.blocksRaycasts = active;
                }
            }
        }
    }

    // Convenience methods
    public void ToHome() => SwitchState("Home");
    
    public void ToGame() 
    {
        SwitchState("Game");
        
        // Call FadeInAll() from UI Fade Manager when switching to Game state
        if (uiFadeManager != null)
        {
            uiFadeManager.FadeInAll();
        }
        else
        {
            Debug.LogWarning("UI Fade Manager is not assigned!");
        }
    }
    
    public void ToAmbient() => SwitchState("Ambient");
    // Add more as needed
}