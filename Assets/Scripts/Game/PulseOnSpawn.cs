using DG.Tweening;
using UnityEngine;

public class PulseOnSpawn : MonoBehaviour
{
    [SerializeField] float targetScale = 1.2f;
    [SerializeField] float duration = 0.3f;
    [SerializeField] Ease ease = Ease.InOutSine;
    [SerializeField] int loops = -1;

    private Tween pulse;

    void OnEnable()
    {
        transform.localScale = Vector3.one;
        pulse = transform.DOScale(targetScale, duration)
                         .SetLoops(loops, LoopType.Yoyo)
                         .SetEase(ease);
    }

    void OnDisable()
    {
        pulse?.Kill();
        transform.localScale = Vector3.one;
    }
}