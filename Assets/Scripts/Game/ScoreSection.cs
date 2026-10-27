using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class ScoreSection : MonoBehaviour
{
    [SerializeField]
    TMPro.TextMeshProUGUI scoreText;

    [SerializeField]
    TouchAreaButton button;

    [SerializeField]
    TextMeshProUGUI buttonText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetScore(int score)
    {
        scoreText.text = score.ToString();
        buttonText.text = "Confirm";

    }

    public void ChangeButtonToConfirm()
    {
        buttonText.text = "Play Again?";
    }

    public void SetupButton(UnityAction action)
    {
        button.onLongPress.RemoveAllListeners();
        button.onLongPress.AddListener((Vector2 _vec)=>action.Invoke());
    }
}
