using System.Text;
using TMPro;
using UnityEngine;

public class GameOverPanelController : MonoBehaviour
{
    [SerializeField]
    GameController gameController;
    [SerializeField]
    NameInputController nameInputController;
    [SerializeField]
    LeaderboardManager leaderboardManager;
    [SerializeField]
    ScoreSection scoreSection;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Setup()
    {
        nameInputController.gameObject.SetActive(true);
        nameInputController.Setup();
        leaderboardManager.gameObject.SetActive(false);
        scoreSection.SetScore(gameController.GetScore());
        scoreSection.SetupButton(() => OnConfirm());
    }

    public void OnConfirm()
    {
        nameInputController.gameObject.SetActive(false);
        leaderboardManager.gameObject.SetActive(true);

        leaderboardManager.AddScore(nameInputController.GetName(), gameController.GetScore());
        leaderboardManager.UpdateLeaderboard();

        scoreSection.ChangeButtonToConfirm();
        scoreSection.SetupButton(()=>gameController.ReplayGame());
    }
}
