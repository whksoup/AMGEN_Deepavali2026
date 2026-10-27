using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class NameInputController : MonoBehaviour
{
    [SerializeField]
    List<TextMeshProUGUI> nameInitital;

    int letterCounter = 0;

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
        foreach (var letter in nameInitital)
        {
            letter.text = "_";
        }
        letterCounter = 0;
    }

    public void OnLetterPress(string value)
    {
        if (letterCounter >= nameInitital.Count)
        {
            return;
        }
        nameInitital[letterCounter].text = value;
        letterCounter += 1;
    }

    public void OnDeletePress()
    {
        if (letterCounter <= 0)
        {
            return;
        }
        letterCounter -= 1;
        nameInitital[letterCounter].text = "_";
    }

    public string GetName()
    {
        return nameInitital[0].text + nameInitital[1].text + nameInitital[2].text;
    }
}
