using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class SignatureTextureCombiner : MonoBehaviour
{
    public string signaturesFolderPath;
    public RenderTexture renderTexture;
    public Material wallMaterial;
    public int textureSize = 4096; // Size of your combined texture

    private Texture2D combinedTexture;
    private List<Rect> occupiedAreas = new List<Rect>();
    private float padding = 0.1f; // Padding between signatures

    void Start()
    {
        signaturesFolderPath = Path.Combine(Application.persistentDataPath, "Signatures");

        // Initialize combined texture
        combinedTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        ClearTexture(combinedTexture, Color.clear);

        // Load existing signatures
        LoadAllSignatures();

        // Start monitoring for new signatures
        StartCoroutine(MonitorSignaturesFolder());
    }

    void ClearTexture(Texture2D texture, Color color)
    {
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply();
    }

    IEnumerator MonitorSignaturesFolder()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            LoadNewSignatures();
        }
    }
    void LoadAllSignatures()
    {
        if (!Directory.Exists(signaturesFolderPath))
        {
            Directory.CreateDirectory(signaturesFolderPath);
            return;
        }

        string[] files = Directory.GetFiles(signaturesFolderPath, "*.png");
        foreach (string file in files)
        {
            AddSignatureToTexture(file);
        }

        ApplyCombinedTexture();
    }

    void LoadNewSignatures()
    {
        string[] files = Directory.GetFiles(signaturesFolderPath, "*.png");
        foreach (string file in files)
        {
            if (!IsFileAlreadyAdded(file))
            {
                AddSignatureToTexture(file);
            }
        }

        ApplyCombinedTexture();
    }

    bool IsFileAlreadyAdded(string filePath)
    {
        // Check if we've already processed this file
        // You might want to implement a more robust check
        return File.Exists(filePath + ".processed");
    }
    void AddSignatureToTexture(string filePath)
    {
        // Load the signature texture
        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D signatureTex = new Texture2D(2, 2);
        signatureTex.LoadImage(fileData);

        // Find available space
        if (FindAvailablePosition(signatureTex.width, signatureTex.height, out Vector2Int position))
        {
            // Copy pixels to combined texture
            Color[] signaturePixels = signatureTex.GetPixels();
            combinedTexture.SetPixels(position.x, position.y, signatureTex.width, signatureTex.height, signaturePixels);

            // Mark area as occupied
            occupiedAreas.Add(new Rect(position.x, position.y, signatureTex.width, signatureTex.height));

            // Mark file as processed
            File.WriteAllText(filePath + ".processed", "");
        }
        else
        {
            Debug.LogWarning("No space left in combined texture for: " + filePath);
            // You could expand the texture here if needed
        }
    }

    bool FindAvailablePosition(int width, int height, out Vector2Int position)
    {
        // Simple bin packing algorithm - you might want to use a more sophisticated one
        for (int y = 0; y < textureSize - height; y += height + (int)(padding * height))
        {
            for (int x = 0; x < textureSize - width; x += width + (int)(padding * width))
            {
                Rect testRect = new Rect(x, y, width, height);
                bool overlaps = false;

                foreach (Rect occupied in occupiedAreas)
                {
                    if (testRect.Overlaps(occupied))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    position = new Vector2Int(x, y);
                    return true;
                }
            }
        }

        position = Vector2Int.zero;
        return false;
    }
    void ApplyCombinedTexture()
    {
        combinedTexture.Apply();

        // Apply to material
        wallMaterial.mainTexture = combinedTexture;

        // Optionally save the combined texture
        SaveCombinedTexture();
    }

    void SaveCombinedTexture()
    {
        byte[] bytes = combinedTexture.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(signaturesFolderPath, "CombinedSignatures.png"), bytes);
    }
}