using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;// Required for TextMeshPro
using UnityEngine.UI; // Required for Button

public class SceneButton : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI sceneLabel;
    [SerializeField]
    Image icon;
    Button button;
    string sceneName;

    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
        if(button)
        {
            button.onClick.AddListener(OnClick);
        }
        else
        {
            Debug.LogError("Button component not found on the GameObject.");
        }
        sceneLabel = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    public void Initialize(string scene, string label, Texture2D texture)
    {
        if (sceneLabel != null)
        {
            sceneLabel.text = label;
        }
        if (icon != null && texture != null)
        {
            // Convert Texture2D to Sprite
            icon.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
        }

        sceneName = scene;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnClick()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
