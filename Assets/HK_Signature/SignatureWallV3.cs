using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class SignatureWallV3 : MonoBehaviour
{
    public Camera orthoCam;

    [Header("Signature Display Properties")]
    public float signatureHeight = -45f;
    public float minSpacing = 0.5f;
    public Vector3 zoomScaleFactor = new Vector3(1f, 1f, 1f);
    
    [Header("Animation Properties")]
    public GameObject animationStartObject;
    public float animationDuration = 2f;
    public float initialScale;
    public float animationHeight = 1f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); 

    // Tracking
    private List<Vector3> placedPositions = new List<Vector3>();
    private List<GameObject> signatureObjects = new List<GameObject>();
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
            Debug.LogError("SignatureWall requires a MeshRenderer (Quad)!");
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
        
        // Display all preloaded signatures
        DisplayAllSignatures();
        canCheckSignatures = true;
    }
    
    private void UpdateCameraBounds()
    {
        if (orthoCam == null) return;

        Vector3 bl = orthoCam.ViewportToWorldPoint(new Vector3(0.05f, 0.05f, orthoCam.nearClipPlane));
        Vector3 tl = orthoCam.ViewportToWorldPoint(new Vector3(0.05f, 0.95f, orthoCam.nearClipPlane));
        Vector3 tr = orthoCam.ViewportToWorldPoint(new Vector3(0.95f, 0.95f, orthoCam.nearClipPlane));
        Vector3 br = orthoCam.ViewportToWorldPoint(new Vector3(0.95f, 0.05f, orthoCam.nearClipPlane));

        quadCenter = (bl + tr) / 2f;
        quadSize = new Vector3(
            Vector3.Distance(bl, br),
            0,
            Vector3.Distance(bl, tl)
        );

        quadBounds = new Bounds(quadCenter, quadSize);
    }
    
    void Update()
    {
        UpdateCameraBounds();
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            RefreshSignatureDisplay();
        }

        // Check for new signatures periodically
        if (Time.frameCount % 60 == 0 && canCheckSignatures)
        {
            CheckForNewSignatures();
        }
    }
    
    /// <summary>
    /// Display all signatures from the preloaded manager
    /// </summary>
    void DisplayAllSignatures()
    {
        if (SignatureManager.Instance == null)
        {
            Debug.LogWarning("SignatureManager not found!");
            return;
        }
        
        List<SignatureManager.SignatureData> allSignatures = SignatureManager.Instance.GetAllSignatures();
        
        foreach (var signatureData in allSignatures)
        {
            if (!displayedSignatures.Contains(signatureData.filePath))
            {
                CreateSignatureObject(signatureData, false); // false = no animation for initial load
            }
        }
        
        Debug.Log($"Displayed {displayedSignatures.Count} signatures on wall");
    }
    
    /// <summary>
    /// Check for new signatures and animate them in
    /// </summary>
    void CheckForNewSignatures()
    {
        if (SignatureManager.Instance == null) return;
        
        List<SignatureManager.SignatureData> newSignatures = SignatureManager.Instance.CheckForNewSignatures();
        
        foreach (var signatureData in newSignatures)
        {
            if (!displayedSignatures.Contains(signatureData.filePath))
            {
                CreateSignatureObject(signatureData, true); // true = with animation
            }
        }
    }
    
    /// <summary>
    /// Create a signature object from preloaded data
    /// </summary>
    GameObject CreateSignatureObject(SignatureManager.SignatureData signatureData, bool animate)
    {
        GameObject signatureObj = new GameObject(signatureData.fileName);
        SpriteRenderer renderer = signatureObj.AddComponent<SpriteRenderer>();
        renderer.sprite = signatureData.sprite;
        renderer.sortingOrder = 0;

        // Find a position for the signature
        Vector3 position;
        bool positionFound = FindAvailablePosition(signatureData.scale, out position);
        
        if (!positionFound)
        {
            Debug.LogWarning("Couldn't find position for signature: " + signatureData.fileName);
            Destroy(signatureObj);
            return null;
        }

        signatureObj.transform.position = position;
        signatureObj.transform.localScale = signatureData.scale;
        signatureObj.transform.rotation = Quaternion.Euler(90, 0, signatureData.rotation);

        placedPositions.Add(position);
        signatureObjects.Add(signatureObj);
        displayedSignatures.Add(signatureData.filePath);
        
        // Animate if requested and animation object exists
        if (animate && animationStartObject != null)
        {
            AnimateSignature(signatureData.sprite, signatureObj);
        }
        
        return signatureObj;
    }
    
    /// <summary>
    /// Find an available position for a signature
    /// </summary>
    bool FindAvailablePosition(Vector3 signatureScale, out Vector3 position)
    {
        position = Vector3.zero;
        int attempts = 0;
        const int maxAttempts = 100;

        do
        {
            position = new Vector3(
                Random.Range(quadCenter.x - quadSize.x / 2 + signatureScale.x / 2, 
                           quadCenter.x + quadSize.x / 2 - signatureScale.x / 2),
                signatureHeight,
                Random.Range(quadCenter.z - quadSize.z / 2 + signatureScale.y / 2, 
                           quadCenter.z + quadSize.z / 2 - signatureScale.y / 2)
            );

            bool overlapping = false;
            foreach (Vector3 placedPos in placedPositions)
            {
                if (Vector3.Distance(new Vector3(position.x, 0, position.z),
                                   new Vector3(placedPos.x, 0, placedPos.z)) < minSpacing)
                {
                    overlapping = true;
                    break;
                }
            }

            if (!overlapping) return true;
            attempts++;
        } while (attempts < maxAttempts);

        return false;
    }
    
    /// <summary>
    /// Animation for new signatures
    /// </summary>
    void AnimateSignature(Sprite sprite, GameObject targetSignature)
    {
        GameObject animatedCopy = new GameObject("AnimatedSignature_" + targetSignature.name);
        SpriteRenderer copyRenderer = animatedCopy.AddComponent<SpriteRenderer>();
        copyRenderer.sprite = sprite;
        copyRenderer.sortingOrder = 10;

        Vector3 startPos = animationStartObject.transform.position;
        startPos.y = animationHeight;
        animatedCopy.transform.position = startPos;
        
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            float cameraScale = mainCam.orthographicSize;
            animatedCopy.transform.localScale = new Vector3(
                cameraScale * zoomScaleFactor.x, 
                cameraScale * zoomScaleFactor.y, 
                cameraScale * zoomScaleFactor.z
            );
        }
        else
        {
            animatedCopy.transform.localScale = animationStartObject.transform.localScale;
        }
        
        animatedCopy.transform.rotation = animationStartObject.transform.rotation;
        targetSignature.GetComponent<SpriteRenderer>().enabled = false;
        
        StartCoroutine(AnimateSignatureCoroutine(animatedCopy, targetSignature));
    }
    
    System.Collections.IEnumerator AnimateSignatureCoroutine(GameObject animatedCopy, GameObject targetSignature)
    {
        Vector3 startPos = animatedCopy.transform.position;
        Vector3 endPos = targetSignature.transform.position;
        Vector3 startScale = animatedCopy.transform.localScale;
        Vector3 endScale = targetSignature.transform.localScale;
        Quaternion startRot = animatedCopy.transform.rotation;
        Quaternion endRot = targetSignature.transform.rotation;

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(t);

            Vector3 currentPos = animatedCopy.transform.position;
            currentPos.x = Mathf.Lerp(startPos.x, endPos.x, curveValue);
            currentPos.z = Mathf.Lerp(startPos.z, endPos.z, curveValue);
            currentPos.y = animationHeight;
            animatedCopy.transform.position = currentPos;

            animatedCopy.transform.localScale = Vector3.Lerp(startScale, endScale, curveValue);
            animatedCopy.transform.rotation = Quaternion.Lerp(startRot, endRot, curveValue);

            yield return null;
        }

        animatedCopy.transform.position = endPos;
        animatedCopy.transform.localScale = endScale;
        animatedCopy.transform.rotation = endRot;

        targetSignature.GetComponent<SpriteRenderer>().enabled = true;
        Destroy(animatedCopy);
    }
    
    /// <summary>
    /// Clear and refresh the display (useful for testing)
    /// </summary>
    public void RefreshSignatureDisplay()
    {
        // Clear current display
        foreach (GameObject obj in signatureObjects)
        {
            Destroy(obj);
        }
        signatureObjects.Clear();
        placedPositions.Clear();
        displayedSignatures.Clear();
        
        // Force manager to reload
        if (SignatureManager.Instance != null)
        {
            SignatureManager.Instance.ReloadAllSignatures();
        }
        
        // Display all signatures again
        DisplayAllSignatures();
    }
    
    /// <summary>
    /// Public methods for external access
    /// </summary>
    public void AddNewSignature()
    {
        CheckForNewSignatures();
    }
    
    public int GetSignatureCount()
    {
        return signatureObjects.Count;
    }
    
    public bool HasSignature(string fileName)
    {
        return SignatureManager.Instance?.HasSignature(fileName) ?? false;
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