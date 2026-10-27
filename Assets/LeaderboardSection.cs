using UnityEngine;
using TMPro;    

public class LeaderboardSection : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI nameText;
    [SerializeField]    
    TextMeshProUGUI scoreText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Setup(string playerName, int score)
    {
        nameText.text = playerName;
        scoreText.text = score.ToString();
    }
}
