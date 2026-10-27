using UnityEngine;

public class StartUpController : MonoBehaviour
{
    GameObject launcherPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        launcherPanel = GameObject.Find("Launcher Canvas");
        if(launcherPanel == null)
        {
            Debug.LogError("Launcher Canvas not found in the scene.");
            return;
        }
        launcherPanel.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
