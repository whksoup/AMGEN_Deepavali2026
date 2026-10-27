using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToStartButton : MonoBehaviour
{
    public void GoBackToStartUp()
    {
       LaunchSceneController.Instance.TriggerChangeScene("Startup");
    }
}