using UnityEngine;

public class MouseVisualizer : MonoBehaviour
{
    public bool displayMouseCoords = false;
    void OnGUI()
    {
        // Get mouse position (bottom-left origin)
        Vector3 mousePos = Input.mousePosition;

        // Convert Y since OnGUI origin is top-left
        float y = Screen.height - mousePos.y;
        if (displayMouseCoords)
        {
                   // Draw coordinates as text
        GUI.Label(new Rect(mousePos.x + 15, y - 5, 200, 20),
                  $"Mouse: {mousePos.x}, {mousePos.y}"); 
        }


        // Draw a small red dot at the cursor
        GUI.color = Color.red;
        GUI.Box(new Rect(mousePos.x - 3, y - 3, 6, 6), GUIContent.none);
        GUI.color = Color.white; // reset
    }
}
