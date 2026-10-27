using UnityEngine;

public class ToggleBlackColorShader : MonoBehaviour
{
    private Renderer rend;

    [Header("Custom Colors (editable in Inspector)")]
    public Color color1 = Color.red;
    public Color color2 = Color.green;
    public Color color3 = Color.blue;
    public Color color4 = Color.yellow;
    public Color color5 = Color.magenta;
    public GameObject signature;

    private Color[] colors;
    private int currentIndex = 0;

    void Start()
    {
        rend = signature.GetComponent<Renderer>();

        // Put the colors into an array for easy cycling
        colors = new Color[] { color1, color2, color3, color4, color5 };

        // Apply the first color
        ApplyColor(colors[currentIndex]);
    }

    

    private void ApplyColor(Color c)
    {
        if (rend == null) rend = GetComponent<Renderer>();
        rend.material.SetColor("_ReplaceColor", c);
       
    }

    // Optional: direct function calls for UI buttons
    public void SetColor1() => ApplyColor(color1);
    public void SetColor2() => ApplyColor(color2);
    public void SetColor3() => ApplyColor(color3);
    public void SetColor4() => ApplyColor(color4);
    public void SetColor5() => ApplyColor(color5);
}
