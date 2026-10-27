using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SignatureWall : MonoBehaviour
{
    private string signaturesFolderPath;

    [Header("Signature Properties")]
    public float universalScale = 0.5f;
    public float minRotation = -15f;
    public float maxRotation = 15f;
    public float minSpacing = 0.5f; 

    // Tracking
    private List<Vector3> placedPositions = new List<Vector3>();
    private List<GameObject> signatureObjects = new List<GameObject>();

    // Cached quad area
    private Bounds quadBounds;
    private Vector3 quadCenter;
    private Vector3 quadSize;

    void Start()
    {
        // Read the quad’s world-space bounds
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            quadBounds = renderer.bounds;
            quadCenter = quadBounds.center;
            quadSize = quadBounds.size;
        }
        else
        {
            Debug.LogError("SignatureWall requires a MeshRenderer (Quad)!");
            return;
        }

        // Setup save/load path
        signaturesFolderPath = Path.Combine(Application.persistentDataPath, "Signatures");
        if (!Directory.Exists(signaturesFolderPath))
        {
            Directory.CreateDirectory(signaturesFolderPath);
            Debug.Log("Created directory: " + signaturesFolderPath);
        }

        // Load and place all signatures
        LoadAndPlaceSignatures();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            ClearAndReloadSignatures();
        }
    }

    void ClearAndReloadSignatures()
    {
        foreach (GameObject obj in signatureObjects)
        {
            Destroy(obj);
        }
        signatureObjects.Clear();
        placedPositions.Clear();

        LoadAndPlaceSignatures();
    }

    void LoadAndPlaceSignatures()
    {
        string[] signatureFiles = Directory.GetFiles(signaturesFolderPath, "*.png");

        foreach (string filePath in signatureFiles)
        {
            Texture2D texture = LoadTexture(filePath);
            if (texture == null) continue;

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            PlaceSignature(sprite, filePath);
        }
    }

    Texture2D LoadTexture(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        return texture;
    }

    void PlaceSignature(Sprite sprite, string originalPath)
    {
        GameObject signatureObj = new GameObject(Path.GetFileName(originalPath));
        SpriteRenderer renderer = signatureObj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 1;

        float spriteAspect = (float)sprite.texture.width / sprite.texture.height;
        float scaledWidth = universalScale * spriteAspect;
        float scaledHeight = universalScale;

        Vector3 position;
        bool positionFound;
        int attempts = 0;
        const int maxAttempts = 100;

        do
        {
            positionFound = true;

            // Pick random point within quad bounds
            position = new Vector3(
                Random.Range(quadCenter.x - quadSize.x / 2 + scaledWidth / 2, quadCenter.x + quadSize.x / 2 - scaledWidth / 2),
                quadCenter.y, // fixed Y (quad plane)
                Random.Range(quadCenter.z - quadSize.z / 2 + scaledHeight / 2, quadCenter.z + quadSize.z / 2 - scaledHeight / 2)
            );

            foreach (Vector3 placedPos in placedPositions)
            {
                if (Vector3.Distance(new Vector3(position.x, 0, position.z),
                                     new Vector3(placedPos.x, 0, placedPos.z)) < minSpacing)
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
            Destroy(signatureObj);
            return;
        }

        signatureObj.transform.position = position;
        signatureObj.transform.localScale = Vector3.one * universalScale;
        signatureObj.transform.rotation = Quaternion.Euler(
            90,
            0,
            Random.Range(minRotation, maxRotation)
        );

        placedPositions.Add(position);
        signatureObjects.Add(signatureObj);
    }

    void OnDrawGizmosSelected()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Bounds b = renderer.bounds;
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawCube(b.center, b.size);
        }
    }
}
