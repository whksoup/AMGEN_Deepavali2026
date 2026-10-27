using UnityEngine;
using UnityEngine.UI;

public class TouchIndicator : MonoBehaviour
{
    private Image image;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        image = GetComponent<Image>();    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Setup()
    {

    }

    public void SetFillAmount(float value)
    {
        Debug.Log(value);
        image.fillAmount = value;
    }
}
