using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionControl : MonoBehaviour
{
    [SerializeField] GameObject touchButton;
    [SerializeField] GameObject selectionObject;
    [SerializeField] List<RectTransform> selectionList = new List<RectTransform>();
    RectTransform rectTransform;
    Sequence sequence;
    Vector3 initialRectScale;
    readonly List<Vector3> selectionInitialScales = new List<Vector3>();
    readonly List<Quaternion> selectionInitialRotations = new List<Quaternion>();

    // Tweens for pulsing the touch button
    Tween touchScaleTween;
    Tween touchAlphaTween;

    // flag to fire once when reverse hits the start
    bool reverseReachedStart;
    bool isOpened = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialRectScale = rectTransform != null ? rectTransform.localScale : Vector3.one;

        selectionInitialScales.Clear();
        selectionInitialRotations.Clear();

        for (int i = 0; i < selectionList.Count; i++)
        {
            var selection = selectionList[i];
            if (selection == null)
            {
                selectionInitialScales.Add(Vector3.one);
                selectionInitialRotations.Add(Quaternion.identity);
                continue;
            }

            selectionInitialScales.Add(selection.localScale);
            selectionInitialRotations.Add(selection.localRotation);
        }
    }

    public void Start()
    {
        if (touchButton != null)
        {
            touchButton.SetActive(true);
            PulseTouchButton();
        }

        if (selectionObject != null)
            selectionObject.SetActive(false);

        isOpened = false;
    }

    void PulseTouchButton()
    {
        touchButton.transform.localScale = Vector3.one;

        touchScaleTween?.Kill();
        touchScaleTween = touchButton.transform
            .DOScale(1.15f, 1.25f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        var img = touchButton.GetComponent<Image>();
        if (img != null)
        {
            var c = img.color;
            c.a = 0.5f;
            img.color = c;

            touchAlphaTween?.Kill();
            touchAlphaTween = img
                .DOFade(0.75f, 1.25f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    public void OnTouch(int value)
    {
        if (value >= 0 && value < selectionList.Count)
            selectionList[value].GetComponent<Image>().DOColor(Color.blue, 0.25f);
    }

    public void OnHoldButtonTouch()
    {
        touchScaleTween?.Kill();
        touchAlphaTween?.Kill();

        var holdSeq = DOTween.Sequence();

        if (touchButton != null)
        {
            var img = touchButton.GetComponent<Image>();
            if (img != null)
            {
                holdSeq.Append(img.DOFade(0f, 0.5f).SetEase(Ease.InOutSine));
                holdSeq.AppendCallback(() => touchButton.SetActive(false));
            }
            else
            {
                holdSeq.AppendInterval(0.5f);
                holdSeq.AppendCallback(() => touchButton.SetActive(false));
            }
        }
        else
        {
            holdSeq.AppendInterval(0.5f);
        }

        holdSeq.onComplete += ShowOpening;
    }

    public void ShowOpening()
    {
        if (selectionObject != null) selectionObject.SetActive(true);
        RestartOpening();
    }

    public void RestartOpening()
    {
        isOpened = false;

        // kill old and rebuild fresh
        if (sequence != null && sequence.IsActive()) sequence.Kill();

        // Ensure previously running tweens don't leave transforms mid-animation
        ResetSelectionTransforms();

        reverseReachedStart = false;

        sequence = DOTween.Sequence()
            .SetAutoKill(false)
            .Pause();

        // main container scale-in
        sequence.Append(rectTransform.DOScale(Vector3.zero, 0.5f)
            .From()
            .SetEase(Ease.OutBack));

        // stagger each selection
        for (int i = 0; i < selectionList.Count; i++)
        {
            float startTime = 0.5f + i * 0.1f;

            sequence.Insert(startTime,
                selectionList[i].DOScale(Vector3.zero, 0.15f)
                    .From()
                    .SetEase(Ease.InSine));

            sequence.Insert(startTime,
                selectionList[i].DOLocalRotate(new Vector3(0, 0, -90), 0.15f, RotateMode.FastBeyond360)
                    .From()
                    .SetEase(Ease.InSine));
        }

        sequence.OnComplete(() =>
        {
            isOpened = true;
        });

        sequence.PlayForward();
    }

    void HandleReachedStart()
    {
        if (selectionObject != null) selectionObject.SetActive(false);

        if (touchButton != null)
        {
            touchButton.SetActive(true);

            var img = touchButton.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                c.a = 1.0f;
                img.color = c;
                img.DOFade(0.5f, 0.5f).SetEase(Ease.InOutSine)
                   .OnComplete(PulseTouchButton);
            }
            else
            {
                PulseTouchButton();
            }
        }
    }

    public void ResetLauncher()
    {
        ResetSelectionTransforms();

        if (selectionObject != null)
            selectionObject.SetActive(false);

        if (touchButton != null)
        {
            touchButton.SetActive(true);
            PulseTouchButton();
        }
    }

    public void RevserveBackToTouchButton()
    {
        sequence?.Kill();
        sequence = DOTween.Sequence();
        sequence.Append(this.rectTransform.DOScale(Vector3.zero, 1.0f)
            .SetEase(Ease.InBack));
        sequence.onComplete = ResetLauncher;
    }

    void ResetSelectionTransforms()
    {
        if (rectTransform != null)
        {
            rectTransform.DOKill();
            rectTransform.localScale = initialRectScale;
        }

        for (int i = 0; i < selectionList.Count; i++)
        {
            var selection = selectionList[i];
            if (selection == null) continue;

            selection.DOKill();
            var baseScale = i < selectionInitialScales.Count ? selectionInitialScales[i] : Vector3.one;
            var baseRotation = i < selectionInitialRotations.Count ? selectionInitialRotations[i] : Quaternion.identity;
            selection.localScale = baseScale;
            selection.localRotation = baseRotation;
        }
    }

    void OnDisable()
    {
        touchScaleTween?.Kill();
        touchAlphaTween?.Kill();
        sequence?.Kill();
    }
}
