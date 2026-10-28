using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchAreaButton : MonoBehaviour
{
    [Header("Target UI Region (Canvas)")]
    [Tooltip("RectTransform area that should accept the long press.")]
    private RectTransform hotZone;

    [Tooltip("Canvas containing the hotZone. Only needed if Screen Space - Camera or World Space.")]
    private Canvas canvas;

    [Header("Long Press Settings")]
    [Tooltip("Seconds the finger must stay down inside the region.")]
    public float requiredHoldSeconds = 0.6f;

    [Tooltip("Max pixel drift allowed from the initial down position (useful for small finger wobble).")]
    public float moveTolerancePixels = 20f;

    [Tooltip("Fire as soon as the threshold time is reached (true), or only on release (false).")]
    public bool fireImmediatelyOnThreshold = false;
    [Tooltip("Fire as soon as it starts ignoring threshold time")]
    public bool fireAtStart = true;

    [Header("Events")]
    public UnityEvent<Vector2> onLongPress; // passes screen position of the press
    public UnityEvent onFirstTouch; // passes screen position of the press
    public UnityEvent onRelease; // passes screen position of the press

    [Header("Prefabs")]
    [SerializeField]
    private TouchIndicator touchIndicator;
    private TouchIndicator touchIndicatorSpawn;

    private class HoldState
    {
        public float tDown;
        public Vector2 startPos;
        public bool inside;
        public bool fired;
    }

    // Track by finger index (unique per active touch)
    private readonly Dictionary<int, HoldState> _holds = new();

    Camera EventCam =>
        canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

    void Awake()
    {
        // Find the RectTransform in the scene
        hotZone = GetComponent<RectTransform>();
        if (hotZone == null)
        {
            Debug.LogError("TouchAreaButton requires a RectTransform component on the same GameObject.");
            enabled = false; // Disable this script if no RectTransform found
        }

        canvas = GetComponentInParent<Canvas>();
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        TouchSimulation.Enable(); // allows mouse to simulate touch in Editor if needed
    }

    void OnDisable()
    {
        TouchSimulation.Disable();
        EnhancedTouchSupport.Disable();
        _holds.Clear();

        // Ensure any indicator is removed when disabled
        RemoveIndicator();
    }

    private void SpawnIndicator(Vector2 screenPos)
    {
        if (touchIndicator == null)
            return;

        //Remove the indicator
        RemoveIndicator();

        // Convert screen position to world position in canvas space
        Vector3 worldPos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(hotZone, screenPos, EventCam, out worldPos))
        {
            Debug.Log("Spawning for " + transform.parent.name);
            touchIndicatorSpawn = Instantiate(touchIndicator, worldPos, Quaternion.identity, hotZone);
        }
    }

    private void RemoveIndicator()
    {
        if (touchIndicatorSpawn)
        {
            // Destroy the entire spawned GameObject so the visual is removed
            Destroy(touchIndicatorSpawn.gameObject);
        }
        touchIndicatorSpawn = null;
    }

    void Update()
    {
        if (hotZone == null) 
            return;

        // Process current active touches
        foreach (var t in Touch.activeTouches)
        {
            int id = t.finger.index;

            switch (t.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    {
                        bool inside = RectTransformUtility.RectangleContainsScreenPoint(hotZone, t.screenPosition, EventCam);
                        if (inside)
                        {
                            LaunchSceneController.RegisterUserInteraction();
                            Debug.Log(transform.parent.name);
                            _holds[id] = new HoldState
                            {
                                tDown = Time.unscaledTime,
                                startPos = t.screenPosition,
                                inside = true,
                                fired = false
                            };

                            if (fireAtStart)
                            {
                                onLongPress?.Invoke(t.screenPosition);
                                _holds.Remove(id);
                            }

                            onFirstTouch?.Invoke();
                            SpawnIndicator(t.screenPosition);
                        }
                        break;
                    }

                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    {
                        if (!_holds.TryGetValue(id, out var st))
                            break;

                        // Update "inside" state
                        st.inside = RectTransformUtility.RectangleContainsScreenPoint(hotZone, t.screenPosition, EventCam);

                        // Check small drift tolerance
                        if (Vector2.Distance(st.startPos, t.screenPosition) > moveTolerancePixels)
                            st.inside = false;

                        if(st.inside)
                        {
                            if (touchIndicatorSpawn)
                                touchIndicatorSpawn.SetFillAmount((Time.unscaledTime - st.tDown)/ requiredHoldSeconds);
                        }
                        else
                        {
                            RemoveIndicator();
                            onRelease?.Invoke();
                        }

                        // Optionally fire immediately on threshold
                        if (!fireAtStart && fireImmediatelyOnThreshold && !st.fired && st.inside &&
                            (Time.unscaledTime - st.tDown) >= requiredHoldSeconds)
                        {
                            st.fired = true;
                            onLongPress?.Invoke(t.screenPosition);
                            AudioManager.Instance?.PlaySFX("Confirm");

                            Debug.Log($"[LongPress] finger {id} fired at {t.screenPosition}");
                            // Remove if one-shot; keep if you want to allow multiple fires (not typical)
                            _holds.Remove(id);
                            RemoveIndicator();
                        }
                        break;
                    }

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    {
                        if (_holds.TryGetValue(id, out var st))
                        {
                            bool longEnough = (Time.unscaledTime - st.tDown) >= requiredHoldSeconds;
                            if (!fireImmediatelyOnThreshold && !st.fired && st.inside && longEnough)
                            {
                                onLongPress?.Invoke(t.screenPosition);
                                Debug.Log($"[LongPress] finger {id} fired on release at {t.screenPosition}");
                            }
                            _holds.Remove(id);
                        }
                        RemoveIndicator();
                        onRelease?.Invoke();
                        break;
                    }
            }
        }

        // Cleanup: if for some reason a finger disappeared without Ended/Canceled
        // (rare), you could scan for stale ids here.
    }
}
