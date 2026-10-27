    using UnityEngine;
using UnityEngine.UI;

public class VideoControlPanel : MonoBehaviour
{
    public TouchAreaButton leftButton;
    public TouchAreaButton rightButton;
    CanvasGroup canvasGroup;

    bool isPlayButton = false;
    public bool IsPlay
    {
        get { return isPlayButton; }
    }

    [SerializeField]
    Sprite play;
    [SerializeField]
    Sprite pause;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Show()
    {
        canvasGroup.alpha = 1;
    }

    public void Fade()
    {
        canvasGroup.alpha = 0f;
    }

    public void Switch()
    {
        isPlayButton = !isPlayButton;
        if (isPlayButton)
        {
            rightButton.transform.GetChild(0).GetComponent<Image>().sprite = play;
        }
        else
        {
            rightButton.transform.GetChild(0).GetComponent<Image>().sprite = pause;
        }
    }
}
