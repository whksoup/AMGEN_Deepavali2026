using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour
{
    Image image;
    Color defaultColor;
    Color pressedColor;
    
    void Start()
    {
        image = GetComponent<Image>();
        defaultColor = image.color;
        
        // Convert hex color #0075C9 to Unity Color
        ColorUtility.TryParseHtmlString("#0075C9", out pressedColor);
    }

    void Update()
    {
        
    }

    public void OnTouch()
    {
        image.DOColor(pressedColor, 0.5f);
    }

    public void OnRelease()
    {
        image.DOColor(defaultColor, 0.5f);
    }
}