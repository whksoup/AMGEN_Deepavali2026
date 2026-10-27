using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class DVDSignatureScreensaver : MonoBehaviour
{
    public Camera orthoCam;

    [Header("Signature Properties")]
    public float signatureHeight = -45f;
    
    [Header("DVD Movement Properties")]
    public float minSpeed = 1f;
    public float maxSpeed = 3f;
    public bool randomizeInitialDirection = true;
    public bool colorChangeOnBounce = false;
    public Color[] bounceColors = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };
    
    [Header("Bounce Settings")]
    public float bounceBuffer = 0.1f; // Small buffer to prevent getting stuck on edges
    
    // Tracking
    private List<GameObject> signatureObjects = new List<GameObject>();
    private List<Vector3> velocities = new List<Vector3>();
    private List<Color> originalColors = new List<Color>();
    private HashSet<string> displayedSignatures = new HashSet<string>(); // Track which signatures are displayed
    
    // Cached quad area
    public Bounds quadBounds;
    public Vector3 quadCenter;
    public Vector3 quadSize;
    private bool canCheckSignatures = false;
    
    void Start()
    {
        // Read the quad's world-space bounds
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            UpdateCameraBounds();
        }
        else
        {
            Debug.LogError("DVDSignatureScreensaver requires a MeshRenderer (Quad)!");
            return;
        }

        StartCoroutine(DelayedInitialization());
    }
    
    private IEnumerator DelayedInitialization()
    {
        // Wait for SignatureManager to be ready
        while (SignatureManager.Instance == null)
        {
            yield return null;
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // Load all preloaded signatures
        LoadAllSignatures();
        canCheckSignatures = true;
    }
    
    private void UpdateCameraBounds()
    {
        if (orthoCam == null) return;

        // Get corners in world space
        Vector3 bl = orthoCam.ViewportToWorldPoint(new Vector3(0.05f, 0.05f, orthoCam.nearClipPlane)); // bottom-left
        Vector3 tl = orthoCam.ViewportToWorldPoint(new Vector3(0.05f, 0.95f, orthoCam.nearClipPlane)); // top-left
        Vector3 tr = orthoCam.ViewportToWorldPoint(new Vector3(0.95f, 0.95f, orthoCam.nearClipPlane)); // top-right
        Vector3 br = orthoCam.ViewportToWorldPoint(new Vector3(0.95f, 0.05f, orthoCam.nearClipPlane)); // bottom-right

        // Compute bounds
        quadCenter = (bl + tr) / 2f;
        quadSize = new Vector3(
            Vector3.Distance(bl, br),   // width
            0,                          // flat plane
            Vector3.Distance(bl, tl)    // height
        );

        quadBounds = new Bounds(quadCenter, quadSize);

        // Debug draw
        Debug.DrawLine(bl, br, Color.green);
        Debug.DrawLine(br, tr, Color.green);
        Debug.DrawLine(tr, tl, Color.green);
        Debug.DrawLine(tl, bl, Color.green);
    }
    
    void Update()
    {
        UpdateCameraBounds();
        MoveDVDSignatures();
        
        // Reload signatures on D key press
        if (Input.GetKeyDown(KeyCode.D))
        {
            ReloadSignatures();
        }
        
        // Check for new signatures periodically
        if (Time.frameCount % 60 == 0 && canCheckSignatures)
        {
            CheckForNewSignatures();
        }
    }
    
    void MoveDVDSignatures()
    {
        for (int i = 0; i < signatureObjects.Count; i++)
        {
            GameObject sig = signatureObjects[i];
            Vector3 velocity = velocities[i];
            
            // Get current position
            Vector3 currentPos = sig.transform.position;
            
            // Calculate sprite bounds
            SpriteRenderer renderer = sig.GetComponent<SpriteRenderer>();
            float spriteWidth = renderer.bounds.size.x;
            float spriteHeight = renderer.bounds.size.z; // Z because we're on XZ plane
            
            // Calculate movement bounds
            float minX = quadCenter.x - quadSize.x / 2 + spriteWidth / 2 + bounceBuffer;
            float maxX = quadCenter.x + quadSize.x / 2 - spriteWidth / 2 - bounceBuffer;
            float minZ = quadCenter.z - quadSize.z / 2 + spriteHeight / 2 + bounceBuffer;
            float maxZ = quadCenter.z + quadSize.z / 2 - spriteHeight / 2 - bounceBuffer;
            
            // Move signature
            Vector3 newPos = currentPos + velocity * Time.deltaTime;
            
            // Check for bounces and reverse direction
            bool bounced = false;
            
            // X-axis bounce
            if (newPos.x <= minX || newPos.x >= maxX)
            {
                velocity.x = -velocity.x;
                newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
                bounced = true;
            }
            
            // Z-axis bounce
            if (newPos.z <= minZ || newPos.z >= maxZ)
            {
                velocity.z = -velocity.z;
                newPos.z = Mathf.Clamp(newPos.z, minZ, maxZ);
                bounced = true;
            }
            
            // Handle color change on bounce
            if (bounced && colorChangeOnBounce && bounceColors.Length > 0)
            {
                Color newColor = bounceColors[Random.Range(0, bounceColors.Length)];
                renderer.color = newColor;
            }
            
            // Update position and velocity
            sig.transform.position = newPos;
            velocities[i] = velocity;
        }
    }
    
    /// <summary>
    /// Load all signatures from the SignatureManager
    /// </summary>
    void LoadAllSignatures()
    {
        if (SignatureManager.Instance == null)
        {
            Debug.LogWarning("SignatureManager not found!");
            return;
        }
        
        ClearAllSignatures();
        
        List<SignatureManager.SignatureData> allSignatures = SignatureManager.Instance.GetAllSignatures();
        
        if (allSignatures.Count == 0)
        {
            Debug.Log("No signatures available in SignatureManager");
            return;
        }

        foreach (var signatureData in allSignatures)
        {
            CreateDVDSignatureObject(signatureData);
            displayedSignatures.Add(signatureData.filePath);
        }
        
        Debug.Log($"Loaded {signatureObjects.Count} signatures for DVD screensaver");
    }
    
    /// <summary>
    /// Check for new signatures and add them to the screensaver
    /// </summary>
    void CheckForNewSignatures()
    {
        if (SignatureManager.Instance == null) return;
        
        List<SignatureManager.SignatureData> newSignatures = SignatureManager.Instance.CheckForNewSignatures();
        
        foreach (var signatureData in newSignatures)
        {
            if (!displayedSignatures.Contains(signatureData.filePath))
            {
                CreateDVDSignatureObject(signatureData);
                displayedSignatures.Add(signatureData.filePath);
                Debug.Log("Added new signature to DVD screensaver: " + signatureData.fileName);
            }
        }
    }
    
    /// <summary>
    /// Create a DVD signature object from SignatureManager data
    /// </summary>
    GameObject CreateDVDSignatureObject(SignatureManager.SignatureData signatureData)
    {
        GameObject signatureObj = new GameObject("DVDSignature_" + signatureData.fileName);
        SpriteRenderer renderer = signatureObj.AddComponent<SpriteRenderer>();
        renderer.sprite = signatureData.sprite;
        renderer.sortingOrder = 0;

        // Use the scale from SignatureManager
        Vector3 scale = signatureData.scale;
        
        // Calculate sprite bounds for proper placement
        float spriteWidth = signatureData.sprite.bounds.size.x * scale.x;
        float spriteHeight = signatureData.sprite.bounds.size.y * scale.y;

        // Random starting position within bounds
        float minX = quadCenter.x - quadSize.x / 2 + spriteWidth / 2 + bounceBuffer;
        float maxX = quadCenter.x + quadSize.x / 2 - spriteWidth / 2 - bounceBuffer;
        float minZ = quadCenter.z - quadSize.z / 2 + spriteHeight / 2 + bounceBuffer;
        float maxZ = quadCenter.z + quadSize.z / 2 - spriteHeight / 2 - bounceBuffer;

        Vector3 position = new Vector3(
            Random.Range(minX, maxX),
            signatureHeight,
            Random.Range(minZ, maxZ)
        );

        signatureObj.transform.position = position;
        signatureObj.transform.localScale = scale;
        signatureObj.transform.rotation = Quaternion.Euler(90, 0, 0); // Face up
        
        // Create random velocity for DVD movement
        Vector3 velocity;
        if (randomizeInitialDirection)
        {
            float speed = Random.Range(minSpeed, maxSpeed);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            velocity = new Vector3(
                Mathf.Cos(angle) * speed,
                0,
                Mathf.Sin(angle) * speed
            );
        }
        else
        {
            // Default diagonal movement like classic DVD screensaver
            float speed = Random.Range(minSpeed, maxSpeed);
            velocity = new Vector3(speed, 0, speed);
        }
        
        // Store original color for potential restoration
        originalColors.Add(renderer.color);
        
        signatureObjects.Add(signatureObj);
        velocities.Add(velocity);
        
        return signatureObj;
    }
    
    void ClearAllSignatures()
    {
        foreach (GameObject obj in signatureObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        
        signatureObjects.Clear();
        velocities.Clear();
        originalColors.Clear();
        displayedSignatures.Clear();
    }
    
    void ReloadSignatures()
    {
        // Force SignatureManager to reload if needed
        if (SignatureManager.Instance != null)
        {
            SignatureManager.Instance.ReloadAllSignatures();
        }
        
        LoadAllSignatures();
    }
    
    /// <summary>
    /// Public method to be called when new signatures are added
    /// </summary>
    public void AddNewSignatures()
    {
        CheckForNewSignatures();
    }
    
    /// <summary>
    /// Pause/Resume all signature movement
    /// </summary>
    public void TogglePause()
    {
        enabled = !enabled;
    }
    
    /// <summary>
    /// Reset all signatures to their original colors
    /// </summary>
    public void ResetColors()
    {
        for (int i = 0; i < signatureObjects.Count; i++)
        {
            if (i < originalColors.Count)
            {
                signatureObjects[i].GetComponent<SpriteRenderer>().color = originalColors[i];
            }
        }
    }
    
    /// <summary>
    /// Change the speed of all signatures
    /// </summary>
    public void SetSpeed(float speedMultiplier)
    {
        for (int i = 0; i < velocities.Count; i++)
        {
            Vector3 vel = velocities[i];
            float currentSpeed = vel.magnitude;
            Vector3 direction = vel.normalized;
            velocities[i] = direction * (currentSpeed * speedMultiplier);
        }
    }
    
    /// <summary>
    /// Add a single signature with random velocity
    /// </summary>
    public void AddSingleSignature(SignatureManager.SignatureData signatureData)
    {
        if (!displayedSignatures.Contains(signatureData.filePath))
        {
            CreateDVDSignatureObject(signatureData);
            displayedSignatures.Add(signatureData.filePath);
        }
    }
    
    /// <summary>
    /// Remove all signatures of a specific type (useful for cleanup)
    /// </summary>
    public void RemoveSignature(string fileName)
    {
        for (int i = signatureObjects.Count - 1; i >= 0; i--)
        {
            if (signatureObjects[i].name.Contains(fileName))
            {
                Destroy(signatureObjects[i]);
                signatureObjects.RemoveAt(i);
                velocities.RemoveAt(i);
                if (i < originalColors.Count)
                    originalColors.RemoveAt(i);
            }
        }
        
        // Remove from displayed set
        string pathToRemove = displayedSignatures.FirstOrDefault(path => path.Contains(fileName));
        if (pathToRemove != null)
        {
            displayedSignatures.Remove(pathToRemove);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Bounds b = renderer.bounds;
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawCube(b.center, b.size);
            
            // Draw movement area
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawWireCube(quadCenter, quadSize);
        }
    }
    
    /// <summary>
    /// Get the current number of signatures bouncing around
    /// </summary>
    public int GetSignatureCount()
    {
        return signatureObjects.Count;
    }
}