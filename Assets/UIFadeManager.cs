using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIFadeManager : MonoBehaviour
{
    [System.Serializable]
    public class FadeableUIElement
    {
        public string elementName;
        public GameObject uiElement;
        public float fadeDuration = 1f;
        
        [HideInInspector]
        public Image imageComponent;
        [HideInInspector]
        public TextMeshProUGUI tmpComponent;
        [HideInInspector]
        public CanvasGroup canvasGroupComponent;
        
        public void Initialize()
        {
            if (uiElement != null)
            {
                imageComponent = uiElement.GetComponent<Image>();
                tmpComponent = uiElement.GetComponent<TextMeshProUGUI>();
                canvasGroupComponent = uiElement.GetComponent<CanvasGroup>();
                
                // Add CanvasGroup if none exists for easier fading
                if (canvasGroupComponent == null && (imageComponent != null || tmpComponent != null))
                {
                    canvasGroupComponent = uiElement.AddComponent<CanvasGroup>();
                }
            }
        }
    }
    
    [Header("Fadeable UI Elements")]
    public List<FadeableUIElement> fadeableElements = new List<FadeableUIElement>();
    
    [Header("Default Settings")]
    public float defaultFadeDuration = 1f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    private Dictionary<string, Coroutine> activeCoroutines = new Dictionary<string, Coroutine>();
    
    private void Start()
    {
        InitializeElements();
    }
    
    private void InitializeElements()
    {
        foreach (var element in fadeableElements)
        {
            element.Initialize();
        }
    }
    
    /// <summary>
    /// Fade out a UI element by name
    /// </summary>
    /// <param name="elementName">Name of the element to fade out</param>
    /// <param name="duration">Override fade duration (optional)</param>
    public void FadeOut(string elementName, float duration = -1f)
    {
        var element = GetElementByName(elementName);
        if (element != null)
        {
            float fadeDuration = duration > 0 ? duration : element.fadeDuration;
            StartFade(element, 0f, fadeDuration);
        }
        else
        {
            Debug.LogWarning($"UI Element '{elementName}' not found in fade list!");
        }
    }
    
    /// <summary>
    /// Fade in a UI element by name
    /// </summary>
    /// <param name="elementName">Name of the element to fade in</param>
    /// <param name="duration">Override fade duration (optional)</param>
    public void FadeIn(string elementName, float duration = -1f)
    {
        var element = GetElementByName(elementName);
        if (element != null)
        {
            float fadeDuration = duration > 0 ? duration : element.fadeDuration;
            StartFade(element, 1f, fadeDuration);
        }
        else
        {
            Debug.LogWarning($"UI Element '{elementName}' not found in fade list!");
        }
    }
    
    /// <summary>
    /// Fade out multiple UI elements at once
    /// </summary>
    /// <param name="elementNames">Array of element names to fade out</param>
    /// <param name="duration">Override fade duration (optional)</param>
    public void FadeOutMultiple(string[] elementNames, float duration = -1f)
    {
        foreach (string name in elementNames)
        {
            FadeOut(name, duration);
        }
    }
    
    /// <summary>
    /// Fade in multiple UI elements at once
    /// </summary>
    /// <param name="elementNames">Array of element names to fade in</param>
    /// <param name="duration">Override fade duration (optional)</param>
    public void FadeInMultiple(string[] elementNames, float duration = -1f)
    {
        foreach (string name in elementNames)
        {
            FadeIn(name, duration);
        }
    }
    
    /// <summary>
    /// Instantly set the alpha of a UI element
    /// </summary>
    /// <param name="elementName">Name of the element</param>
    /// <param name="alpha">Alpha value (0-1)</param>
    public void SetAlpha(string elementName, float alpha)
    {
        var element = GetElementByName(elementName);
        if (element != null)
        {
            // Stop any active fade
            StopActiveFade(elementName);
            
            // Set alpha immediately
            SetElementAlpha(element, alpha);
        }
    }
    
    /// <summary>
    /// Get the current alpha value of a UI element
    /// </summary>
    /// <param name="elementName">Name of the element</param>
    /// <returns>Current alpha value</returns>
    public float GetAlpha(string elementName)
    {
        var element = GetElementByName(elementName);
        if (element != null)
        {
            if (element.canvasGroupComponent != null)
                return element.canvasGroupComponent.alpha;
            else if (element.imageComponent != null)
                return element.imageComponent.color.a;
            else if (element.tmpComponent != null)
                return element.tmpComponent.color.a;
        }
        return 0f;
    }
    
    private FadeableUIElement GetElementByName(string name)
    {
        return fadeableElements.Find(element => element.elementName == name);
    }
    
    private void StartFade(FadeableUIElement element, float targetAlpha, float duration)
    {
        // Stop any existing fade for this element
        StopActiveFade(element.elementName);
        
        // Start new fade coroutine
        Coroutine fadeCoroutine = StartCoroutine(FadeCoroutine(element, targetAlpha, duration));
        activeCoroutines[element.elementName] = fadeCoroutine;
    }
    
    private void StopActiveFade(string elementName)
    {
        if (activeCoroutines.ContainsKey(elementName) && activeCoroutines[elementName] != null)
        {
            StopCoroutine(activeCoroutines[elementName]);
            activeCoroutines.Remove(elementName);
        }
    }
    
    private IEnumerator FadeCoroutine(FadeableUIElement element, float targetAlpha, float duration)
    {
        float startAlpha = GetElementAlpha(element);
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float curveValue = fadeCurve.Evaluate(progress);
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            
            SetElementAlpha(element, currentAlpha);
            
            yield return null;
        }
        
        // Ensure we reach the exact target alpha
        SetElementAlpha(element, targetAlpha);
        
        // Remove from active coroutines
        activeCoroutines.Remove(element.elementName);
    }
    
    private float GetElementAlpha(FadeableUIElement element)
    {
        if (element.canvasGroupComponent != null)
            return element.canvasGroupComponent.alpha;
        else if (element.imageComponent != null)
            return element.imageComponent.color.a;
        else if (element.tmpComponent != null)
            return element.tmpComponent.color.a;
        
        return 1f;
    }
    
    private void SetElementAlpha(FadeableUIElement element, float alpha)
    {
        if (element.canvasGroupComponent != null)
        {
            element.canvasGroupComponent.alpha = alpha;
        }
        else
        {
            if (element.imageComponent != null)
            {
                Color color = element.imageComponent.color;
                color.a = alpha;
                element.imageComponent.color = color;
            }
            
            if (element.tmpComponent != null)
            {
                Color color = element.tmpComponent.color;
                color.a = alpha;
                element.tmpComponent.color = color;
            }
        }
    }
    
    // Utility methods for easy access
    public void FadeOutAll()
    {
        foreach (var element in fadeableElements)
        {
            StartFade(element, 0f, element.fadeDuration);
        }
    }
    
    public void FadeInAll()
    {
        foreach (var element in fadeableElements)
        {
            StartFade(element, 1f, element.fadeDuration);
        }
    }
    
    public bool IsFading(string elementName)
    {
        return activeCoroutines.ContainsKey(elementName);
    }
}