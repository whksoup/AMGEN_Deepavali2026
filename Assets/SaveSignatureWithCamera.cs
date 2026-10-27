using UnityEngine;
using System.IO;

public class SaveSignatureWithCamera : MonoBehaviour
{
    public Camera captureCamera;       // Assign your orthographic camera here
    public int outputWidth = 1024;
    public int outputHeight = 1024;
    public bool useCameraSize = true; // New bool to control auto-sizing
    public SignatureWallV3 signatureWall;

    void Start()
    {
        captureCamera.enabled = false;
        
        if (useCameraSize)
        {
            // Set output dimensions to match camera's pixel dimensions
            outputWidth = (int)captureCamera.pixelWidth;
            outputHeight = (int)captureCamera.pixelHeight;
            Debug.Log($"Set output dimensions to camera size: {outputWidth}x{outputHeight}");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SaveSignatureImage();
        }
    }

    public void SaveSignatureImage()
    {
        // Create RenderTexture
        RenderTexture rt = new RenderTexture(outputWidth, outputHeight, 24);
        captureCamera.targetTexture = rt;

        // Render without enabling
        captureCamera.Render();

        // Read pixels
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, outputWidth, outputHeight), 0, 0);
        tex.Apply();

        RenderTexture.active = null;
        captureCamera.targetTexture = null;
        rt.Release();

        // White to transparent filter
        Color[] pixels = tex.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].r > 0.95f && pixels[i].g > 0.95f && pixels[i].b > 0.95f)
                pixels[i] = new Color(0, 0, 0, 0);
        }
        tex.SetPixels(pixels);
        tex.Apply();

        // Save PNG
        string folderPath = Path.Combine(Application.persistentDataPath, "Signatures");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string fileName = $"Signature_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string fullPath = Path.Combine(folderPath, fileName);

        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngData);

        Debug.Log($"Saved signature to: {fullPath}");
        signatureWall.AddNewSignature();
    }
}