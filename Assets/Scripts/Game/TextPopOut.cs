using UnityEngine;
using TMPro;
using DG.Tweening;

public class TextPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshPro tmp; 

    [Header("Animation")]
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private float moveUp = 1.5f;     // world units
    [SerializeField] private Ease scaleEase = Ease.OutBack;
    [SerializeField] private bool destroyOnComplete = true;
    [SerializeField] private bool playOnStart = false;

    private Sequence seq;

    public void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    public void Play(string text = null, Vector3? worldPos = null)
    {
        if (seq != null && seq.IsActive()) seq.Kill();

        if (!tmp) 
            tmp = GetComponent<TextMeshPro>();
        if (!tmp) 
        { Debug.LogWarning("TMPWorldPopup: Missing TextMeshPro."); return; }

        if (!string.IsNullOrEmpty(text)) 
            tmp.text = text;

        if (worldPos.HasValue) 
            transform.position = worldPos.Value;

        // reset state
        transform.localScale = Vector3.zero;

        var c = tmp.color;
        c.a = 1f;
        tmp.color = c;

        float startY = transform.position.y;

        seq = DOTween.Sequence()
            .Join(transform.DOScale(1f, duration).SetEase(scaleEase))
            .Join(tmp.DOFade(0f, duration))
            .Join(transform.DOMoveY(startY + moveUp, duration).SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                if (destroyOnComplete)
                    Destroy(gameObject);
            });
    }

    private void OnDisable()
    {
        if (seq != null && seq.IsActive()) 
            seq.Kill();
    }
}
