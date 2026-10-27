using UnityEngine;
using TMPro;

public class LetterButton : MonoBehaviour
{
    TextMeshProUGUI letterText;
    [SerializeField]
    string letter;
    TouchAreaButton touchAreaButton;
    NameInputController nameInputController;


    void Start()
    {
        nameInputController = GameObject.Find("Name Input Controller").GetComponent<NameInputController>();
        touchAreaButton = GetComponent<TouchAreaButton>();
        if(letter == "~")
        {
            touchAreaButton.onLongPress.AddListener((UnityEngine.Vector2 _) => nameInputController?.OnDeletePress());
            return;
        }

        letterText = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        letterText.text = letter;


        // Subscribe using AddListener; ignore the Vector2 parameter and call OnLetterPress with the letter.
        touchAreaButton.onLongPress.AddListener((UnityEngine.Vector2 _) => nameInputController.OnLetterPress(letter));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Called whenever a value changes in the Inspector (edit mode)
    void OnValidate()
    {
        // Only run in Editor (to avoid null ref during builds)
        if (!Application.isPlaying)
        {
            var textComp = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = letter;
            }
        }
    }
}
