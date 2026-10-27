using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScreenSaverModeController : MonoBehaviour
{
    [SerializeField]
    List<GameObject> flowersList;
    [SerializeField]
    List<Image> flowersSelectionList;
    [SerializeField]
    List<ParticleSystem> particleSystemsList;
    [Header("Text Tween Settings")]
    [SerializeField]
    TextMeshProUGUI screenText1;
    [SerializeField]
    TextMeshProUGUI screenText2;
    [SerializeField] float fadeOutDur = 0.35f;
    [SerializeField] float fadeInDur = 0.45f;
    [SerializeField] float betweenTextsDelay = 0.5f;
    [SerializeField] bool typeOnWords = true;  // animate words after setting text
    [SerializeField] float typeOnDurPerText = 0.8f;

    Sequence pairSequence; // keep a handle to cancel/replace
    [SerializeField]
    bool showDebug = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ShowDebug();

       
    }

    private void OnEnable()
    {
        //Play the music
        AudioManager.Instance?.PlayBackgroundMusic("Screen Saver BGM", 0.5f);
    }

    private void OnDisable()
    {
        AudioManager.Instance?.StopBackgroundMusic(0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.D))
        {
            showDebug = !showDebug;
            ShowDebug();
        }
    }

    public void OnFlowerPressed(int value)
    {
        Debug.Log("Flower pressed: " + flowersList[value].name);
        Flower f = flowersList[value].GetComponent<Flower>();

        f.OnTouched();
        PlayTextPair(f.Text1, f.Text2);
        particleSystemsList[value].Play();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Bloom");
        }
        else
        {
            Debug.LogWarning("[Flower] AudioManager not found - sound effect skipped");
        }
    }

    public void ShowDebug()
    {
        if(showDebug) 
        {
            foreach(var f in flowersSelectionList)
            {
                f.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
        else
        {
            foreach (var f in flowersSelectionList)
            {
                f.color = new Color(0, 0, 0, 0);
            }
        }
    }

    public void PlayTextPair(string text1, string text2)
    {
        // Ensure any current animation is stopped
        if (pairSequence != null && pairSequence.IsActive()) pairSequence.Kill();

        // Also kill tweens on each TMP in case they have their own sequences
        if (screenText1) 
            DOTween.Kill(screenText1);
        if (screenText2) 
            DOTween.Kill(screenText2);

        screenText1.text = "";
        screenText2.text = "";

        pairSequence = DOTween.Sequence();

        // First line
        BuildSingleLineTween(pairSequence, screenText1, text1);

        // Delay between the two lines (so they don�t swap at the same time)
        pairSequence.AppendInterval(betweenTextsDelay);

        // Second line
        BuildSingleLineTween(pairSequence, screenText2, text2);

        // Optionally: pairSequence.OnComplete(() => { /* anything after both done */ });
    }

    //void ShowWordsOneByOne()
    //{
    //    // Reset to zero visible words
    //    screenText.maxVisibleWords = 0;

    //    // Count how many words are in the text
    //    int wordCount = screenText.textInfo.wordCount;

    //    // Animate maxVisibleWords from 0 to wordCount
    //    DOTween.To(
    //        () => screenText.maxVisibleWords,
    //        x => screenText.maxVisibleWords = x,
    //        wordCount,
    //        5.0f
    //    ).SetEase(Ease.Linear);
    //}

    void BuildSingleLineTween(Sequence host, TextMeshProUGUI tmp, string newText)
    {
        if (tmp == null) return;

        // Start from current alpha
        float startAlpha = tmp.color.a;

        // Fade out to 0
        host.Append(tmp.DOFade(0f, fadeOutDur));
       // host.Append(tmp.transform.DOMoveY(-50, fadeOutDur).From().SetEase(Ease.OutCubic));

        // Swap text, reset type-on state while alpha is 0
        host.AppendCallback(() =>
        {
            tmp.text = newText ?? string.Empty;

            if (typeOnWords)
            {
                // Reset word visibility
                tmp.ForceMeshUpdate();
                tmp.maxVisibleWords = 0;
            }
        });

        if (typeOnWords)
        {
            // Animate words while still invisible (cleaner pop when we fade in)
            int wordCount = Mathf.Max(1, tmp.textInfo.wordCount);
            host.Append(DOTween.To(() => tmp.maxVisibleWords, x => tmp.maxVisibleWords = x, wordCount, typeOnDurPerText)
                               .SetEase(Ease.Linear));
        }

        // Fade in to 1
        host.Append(tmp.DOFade(1f, fadeInDur));
        host.Join(tmp.transform.DOScale(new Vector3(1.35f,1.35f,1.35f), fadeInDur).From().SetEase(Ease.OutBack));

       // host.Join(tmp.transform.DOMoveY(tmp.transform.position.y - 100, fadeInDur).From().SetEase(Ease.InCubic));
    }

    //void FadeIn()
    //{
    //    DOTween.To(
    //        () => screenText.color.a,
    //        x => screenText.color = new Color(screenText.color.r, screenText.color.g, screenText.color.b, x),
    //        1.0f,
    //        2.0f
    //    ).SetEase(Ease.Linear);
    //}
}
