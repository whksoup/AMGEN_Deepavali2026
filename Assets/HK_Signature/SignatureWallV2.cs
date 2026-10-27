using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Events;

[System.Serializable]
public class SignaturePlacementData
{
    public List<Vector2> usedPositions = new List<Vector2>();
    public List<string> signatureFiles = new List<string>();
}

public class SignatureWallV2 : MonoBehaviour
{
    // Configuration
    public string signaturesFolderPath = @"C:\Users\He Kai\AppData\LocalLow\DefaultCompany\SignatureSaver\Signatures";
    public string placementDataPath = "SignaturePlacements.json";

    [Header("Placement Area")]
    public Vector2 areaCenter = Vector2.zero;
    public Vector2 areaSize = new Vector2(10f, 5f); // X (width), Z (depth)
    public float yPosition = 0f; // Vertical position

    [Header("Signature Properties")]
    public float universalScale = 0.5f;
    public float minRotation = -15f;
    public float maxRotation = 15f;
    public float minSpacing = 0.5f;

    [Header("Events")]
    public UnityEvent onSignaturesLoaded;
    public UnityEvent onPlacementsSaved;

    // Runtime data
    private SignaturePlacementData placementData;
    private List<GameObject> signatureObjects = new List<GameObject>();

    void Start()
    {
        placementDataPath = Path.Combine(Application.persistentDataPath, placementDataPath);
        signaturesFolderPath = Path.Combine(Application.persistentDataPath, "Signatures");

        // Ensure directory exists
        if (!Directory.Exists(signaturesFolderPath))
        {
            Directory.CreateDirectory(signaturesFolderPath);
            Debug.Log("Created directory: " + signaturesFolderPath);
        }

        // Load placement data
        LoadPlacementData();

        // Load and place signatures
        LoadAndPlaceSignatures();
    }

    void LoadPlacementData()
    {
        if (File.Exists(placementDataPath))
        {
            string jsonData = File.ReadAllText(placementDataPath);
            placementData = JsonUtility.FromJson<SignaturePlacementData>(jsonData);
        }
        else
        {
            placementData = new SignaturePlacementData();
        }
    }

    void SavePlacementData()
    {
        string jsonData = JsonUtility.ToJson(placementData, true);
        File.WriteAllText(placementDataPath, jsonData);
        onPlacementsSaved?.Invoke();
        Debug.Log("Saved placement data to: " + placementDataPath);
    }

    void LoadAndPlaceSignatures()
    {
        // Clear existing signatures
        ClearAllSignatures();

        // Get all PNG files
        string[] signatureFiles = Directory.GetFiles(signaturesFolderPath, "*.png");

        foreach (string filePath in signatureFiles)
        {
            // Skip already placed signatures
            if (placementData.signatureFiles.Contains(Path.GetFileName(filePath)))
                continue;

            // Create sprite from PNG
            Texture2D texture = LoadTexture(filePath);
            if (texture == null) continue;

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            // Place sprite in world space
            PlaceSignature(sprite, filePath);
        }

        onSignaturesLoaded?.Invoke();
    }

    void PlaceSignature(Sprite sprite, string filePath)
    {
        // Calculate scaled size in world units
        float spriteAspect = (float)sprite.texture.width / sprite.texture.height;
        float scaledWidth = universalScale * spriteAspect;
        float scaledHeight = universalScale;

        // Find random position that doesn't overlap
        Vector2 position;
        bool positionFound;
        int attempts = 0;
        const int maxAttempts = 100;

        do
        {
            positionFound = true;
            position = new Vector2(
                Random.Range(areaCenter.x - areaSize.x / 2 + scaledWidth / 2, areaCenter.x + areaSize.x / 2 - scaledWidth / 2),
                Random.Range(areaCenter.y - areaSize.y / 2 + scaledHeight / 2, areaCenter.y + areaSize.y / 2 - scaledHeight / 2)
            );

            // Check against all used positions (current and saved)
            foreach (Vector2 usedPos in placementData.usedPositions)
            {
                if (Vector2.Distance(position, usedPos) < minSpacing)
                {
                    positionFound = false;
                    break;
                }
            }

            attempts++;
        } while (!positionFound && attempts < maxAttempts);

        if (!positionFound)
        {
            Debug.LogWarning("Couldn't find non-overlapping position for signature");
            return;
        }

        // Create and place GameObject
        GameObject signatureObj = new GameObject(Path.GetFileName(filePath));
        SpriteRenderer renderer = signatureObj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 1;

        signatureObj.transform.position = new Vector3(position.x, yPosition, position.y);
        signatureObj.transform.localScale = Vector3.one * universalScale;
        signatureObj.transform.rotation = Quaternion.Euler(90, 0, Random.Range(minRotation, maxRotation));

        // Track placement
        placementData.usedPositions.Add(position);
        placementData.signatureFiles.Add(Path.GetFileName(filePath));
        signatureObjects.Add(signatureObj);
    }

    void ClearAllSignatures()
    {
        foreach (GameObject obj in signatureObjects)
        {
            Destroy(obj);
        }
        signatureObjects.Clear();
    }

    Texture2D LoadTexture(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        return texture;
    }

    // Public API
    public void SaveCurrentPlacements()
    {
        SavePlacementData();
    }

    public void ReloadSignatures()
    {
        LoadAndPlaceSignatures();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Vector3 center = new Vector3(areaCenter.x, yPosition, areaCenter.y);
        Vector3 size = new Vector3(areaSize.x, 0.1f, areaSize.y);
        Gizmos.DrawCube(center, size);
    }
}