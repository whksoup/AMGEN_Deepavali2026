using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpacityTrigger : MonoBehaviour
{
    [Header("Target Object")]
    public GameObject targetObject;
    
    [Header("Trigger Settings")]
    [Range(0f, 1f)]
    public float opacityThreshold = 0.5f;
    
    [Header("Objects to Control")]
    public List<GameObject> objectsToControl = new List<GameObject>();
    
    private CanvasGroup targetCanvasGroup;
    private bool lastState = false;

    private void Start()
    {
        if (targetObject != null)
        {
            targetCanvasGroup = targetObject.GetComponent<CanvasGroup>();
            if (targetCanvasGroup == null)
            {
                Debug.LogWarning($"OpacityTrigger: Target object '{targetObject.name}' doesn't have a CanvasGroup component.");
            }
        }
        else
        {
            Debug.LogWarning("OpacityTrigger: No target object assigned.");
        }
    }

    private void Update()
    {
        if (targetObject == null || targetCanvasGroup == null) return;

        bool shouldActivate = targetObject.activeInHierarchy && 
                              targetCanvasGroup.alpha > opacityThreshold;

        if (shouldActivate != lastState)
        {
            SetObjectsActive(shouldActivate);
            lastState = shouldActivate;
        }
    }

    private void SetObjectsActive(bool active)
    {
        foreach (var obj in objectsToControl)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }

    // Optional: Call this manually if you want to force an update
    public void ForceUpdate()
    {
        if (targetObject == null || targetCanvasGroup == null) return;

        bool shouldActivate = targetObject.activeInHierarchy && 
                              targetCanvasGroup.alpha > opacityThreshold;
        
        SetObjectsActive(shouldActivate);
        lastState = shouldActivate;
    }
}