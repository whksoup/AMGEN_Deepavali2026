using System.Collections;
using UnityEngine;

// Temporary feedback for the Deepavali mesh blockout's TouchAreaButton regions.
public class DeepavaliPlaceholderInteraction : MonoBehaviour
{
    [SerializeField] private Transform visual;
    private Vector3 baseScale;
    private Coroutine pulse;

    private void Awake()
    {
        if (visual != null) baseScale = visual.localScale;
    }

    public void OnTouch(Vector2 screenPosition)
    {
        LaunchSceneController.RegisterUserInteraction();
        Debug.Log($"[Deepavali] {name} touched at {screenPosition}", this);
        if (visual == null) return;
        if (pulse != null) StopCoroutine(pulse);
        pulse = StartCoroutine(Pulse());
    }

    private IEnumerator Pulse()
    {
        float elapsed = 0f;
        while (elapsed < 0.45f)
        {
            elapsed += Time.unscaledDeltaTime;
            visual.localScale = baseScale * (1f + 0.12f * Mathf.Sin(Mathf.Clamp01(elapsed / 0.45f) * Mathf.PI));
            yield return null;
        }
        visual.localScale = baseScale;
        pulse = null;
    }

    private void OnDisable()
    {
        if (pulse != null) StopCoroutine(pulse);
        if (visual != null) visual.localScale = baseScale;
        pulse = null;
    }
}
