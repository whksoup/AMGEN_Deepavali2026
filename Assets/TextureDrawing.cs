using UnityEngine;
using System.Collections.Generic;

public class TextureDrawing : MonoBehaviour
{
    [Header("Drawing Settings")]
    public int brushSize = 15;
    public Camera drawingCamera;
    public Color brushColor = Color.black;
    [Range(0f, 1f)]
    public float brushOpacity = 1f;
    
    [Header("Stabilization Settings")]
    [Range(0f, 1f)]
    public float stabilizationStrength = 0.5f;
    [Range(3, 10)]
    public int smoothingWindowSize = 5;
    [Range(0.1f, 2f)]
    public float minimumDistance = 1f;
    public bool useAdaptiveSmoothing = true;

    [Header("Preview Line")]
    public Material previewLineMaterial;
    public float previewLineWidth = 2f;
    public Color previewLineColor = Color.red;

    [Header("UI Buttons")]
    public GameObject confirmButton;
    public GameObject stopButton;

    private Texture2D drawableTexture;
    private Renderer planeRenderer;
    private Collider planeCollider;

    // Enhanced smoothing variables
    private Queue<Vector2> inputPoints = new Queue<Vector2>();
    private List<Vector2> smoothedPath = new List<Vector2>();
    private Vector2 lastDrawPoint = Vector2.zero;
    private Vector2 currentSmoothedPoint = Vector2.zero;
    private bool isDrawing = false;

    // Preview line rendering
    private LineRenderer previewLineRenderer;
    private GameObject previewLineObject;
    private List<Vector3> previewWorldPoints = new List<Vector3>();

    // Cached arrays for performance
    private Color[] clearPixels;
    private float[,] brushMask;

    void Start()
    {
        planeRenderer = GetComponent<Renderer>();
        planeCollider = GetComponent<Collider>();

        if (drawingCamera == null)
            drawingCamera = Camera.main;

        InitializeTexture();
        CreateBrushMask();
        SetupPreviewLine();
        InitializeButtonStates();
    }

    void InitializeButtonStates()
    {
        // Set initial button states (confirm enabled, stop disabled)
        if (confirmButton != null)
            confirmButton.SetActive(true);
        
        if (stopButton != null)
            stopButton.SetActive(false);
    }

    void UpdateButtonStates(bool drawing)
    {
        // When drawing: hide confirm, show stop
        // When not drawing: show confirm, hide stop
        if (confirmButton != null)
            confirmButton.SetActive(!drawing);
        
        if (stopButton != null)
            stopButton.SetActive(drawing);
    }

    void InitializeTexture()
    {
        Texture2D originalTexture = planeRenderer.material.mainTexture as Texture2D;
        if (originalTexture != null)
        {
            drawableTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
            drawableTexture.SetPixels(originalTexture.GetPixels());
            drawableTexture.Apply();
            planeRenderer.material.mainTexture = drawableTexture;
        }
        else
        {
            drawableTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            planeRenderer.material.mainTexture = drawableTexture;
        }

        // Cache clearPixels once
        clearPixels = new Color[drawableTexture.width * drawableTexture.height];
        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = Color.white;

        // Fill initially
        drawableTexture.SetPixels(clearPixels);
        drawableTexture.Apply();
    }

    void CreateBrushMask()
    {
        int size = brushSize * 2 + 1;
        brushMask = new float[size, size];
        float center = brushSize;
        float radius = brushSize;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                
                // Create soft falloff
                if (distance <= radius)
                {
                    // Smooth falloff using smoothstep-like function
                    float normalizedDistance = distance / radius;
                    float falloff = 1f - (normalizedDistance * normalizedDistance * (3f - 2f * normalizedDistance));
                    brushMask[x, y] = Mathf.Clamp01(falloff);
                }
                else
                {
                    brushMask[x, y] = 0f;
                }
            }
        }
    }

    void SetupPreviewLine()
    {
        // Create preview line object
        previewLineObject = new GameObject("PreviewLine");
        previewLineRenderer = previewLineObject.AddComponent<LineRenderer>();
        
        // Configure line renderer
        if (previewLineMaterial != null)
            previewLineRenderer.material = previewLineMaterial;
        else
        {
            // Create a simple unlit material if none provided
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = previewLineColor;
            previewLineRenderer.material = mat;
        }

        //previewLineRenderer.color = previewLineColor;
        previewLineRenderer.startWidth = previewLineWidth;
        previewLineRenderer.endWidth = previewLineWidth;
        previewLineRenderer.useWorldSpace = true;
        previewLineRenderer.sortingOrder = 1000; // Render on top
        previewLineRenderer.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2? hitPoint = GetMouseHitPoint();
            if (hitPoint.HasValue)
            {
                StartDrawing(hitPoint.Value);
            }
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            Vector2? hitPoint = GetMouseHitPoint();
            if (hitPoint.HasValue)
            {
                ContinueDrawing(hitPoint.Value);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndDrawing();
        }

        // Update brush mask if brush size changed
        if (brushMask.GetLength(0) != brushSize * 2 + 1)
        {
            CreateBrushMask();
        }
    }

    void StartDrawing(Vector2 point)
    {
        isDrawing = true;
        inputPoints.Clear();
        smoothedPath.Clear();
        previewWorldPoints.Clear();
        
        inputPoints.Enqueue(point);
        smoothedPath.Add(point);
        
        lastDrawPoint = point;
        currentSmoothedPoint = point;
        
        // Start preview line
        previewLineRenderer.enabled = true;
        UpdatePreviewLine();
        
        // Draw initial point
        DrawPixelsWithBrush((int)point.x, (int)point.y, brushColor, brushOpacity);

        // Update button states
        UpdateButtonStates(true);
    }

    void ContinueDrawing(Vector2 newPoint)
    {
        // Add the new point to our input queue
        inputPoints.Enqueue(newPoint);
        
        // Keep only the last N points for smoothing
        while (inputPoints.Count > smoothingWindowSize)
        {
            inputPoints.Dequeue();
        }

        // Apply stabilization to get the smoothed point
        Vector2 stabilizedPoint = ApplyStabilization(newPoint);
        currentSmoothedPoint = stabilizedPoint;
        
        // Add to smoothed path for preview
        smoothedPath.Add(stabilizedPoint);
        
        // Update the preview line in real-time
        UpdatePreviewLine();
        
        // Only draw to texture if we've moved a minimum distance
        float distance = Vector2.Distance(lastDrawPoint, stabilizedPoint);
        if (distance >= minimumDistance)
        {
            DrawLineBetweenPoints(lastDrawPoint, stabilizedPoint);
            lastDrawPoint = stabilizedPoint;
        }
    }

    void EndDrawing()
    {
        if (isDrawing)
        {
            // Draw any remaining distance to the final smoothed point
            DrawLineBetweenPoints(lastDrawPoint, currentSmoothedPoint);
        }
        
        isDrawing = false;
        inputPoints.Clear();
        smoothedPath.Clear();
        previewWorldPoints.Clear();
        previewLineRenderer.enabled = false;

        // Update button states
        UpdateButtonStates(false);
    }

    void UpdatePreviewLine()
    {
        if (smoothedPath.Count < 2)
        {
            previewLineRenderer.positionCount = 0;
            return;
        }

        // Convert texture coordinates to world positions
        previewWorldPoints.Clear();
        
        for (int i = Mathf.Max(0, smoothedPath.Count - 20); i < smoothedPath.Count; i++) // Show last 20 points
        {
            Vector2 textureCoord = new Vector2(
                smoothedPath[i].x / drawableTexture.width,
                smoothedPath[i].y / drawableTexture.height
            );
            
            Vector3 worldPos = TextureCoordToWorldPos(textureCoord);
            if (worldPos != Vector3.zero)
                previewWorldPoints.Add(worldPos);
        }

        if (previewWorldPoints.Count > 1)
        {
            previewLineRenderer.positionCount = previewWorldPoints.Count;
            previewLineRenderer.SetPositions(previewWorldPoints.ToArray());
        }
    }

    Vector3 TextureCoordToWorldPos(Vector2 textureCoord)
    {
        // Convert texture coordinate back to world position on the plane
        Bounds bounds = planeRenderer.bounds;
        Vector3 worldPos = new Vector3(
            bounds.min.x + textureCoord.x * bounds.size.x,
            bounds.min.y + textureCoord.y * bounds.size.y,
            bounds.center.z - 0.01f // Slightly in front of the plane
        );
        return worldPos;
    }

    Vector2 ApplyStabilization(Vector2 currentPoint)
    {
        if (inputPoints.Count < 2)
            return currentPoint;

        Vector2 smoothedPoint = currentPoint;

        if (useAdaptiveSmoothing)
        {
            smoothedPoint = ApplyAdaptiveSmoothing(currentPoint);
        }
        else
        {
            smoothedPoint = ApplyMovingAverageSmoothing();
        }

        // Interpolate between raw input and smoothed position based on stabilization strength
        return Vector2.Lerp(currentPoint, smoothedPoint, stabilizationStrength);
    }

    Vector2 ApplyMovingAverageSmoothing()
    {
        Vector2 sum = Vector2.zero;
        int count = 0;

        foreach (Vector2 point in inputPoints)
        {
            sum += point;
            count++;
        }

        return sum / count;
    }

    Vector2 ApplyAdaptiveSmoothing(Vector2 currentPoint)
    {
        if (inputPoints.Count < 3)
            return ApplyMovingAverageSmoothing();

        // Calculate velocity to adapt smoothing strength
        Vector2[] pointArray = inputPoints.ToArray();
        float velocity = CalculateVelocity(pointArray);
        
        // Higher velocity = less smoothing, lower velocity = more smoothing
        float adaptiveStrength = Mathf.Lerp(0.8f, 0.2f, velocity / 10f);
        
        // Apply weighted average with emphasis on recent points
        Vector2 weightedSum = Vector2.zero;
        float totalWeight = 0f;
        
        for (int i = 0; i < pointArray.Length; i++)
        {
            // Give more weight to recent points
            float weight = (float)(i + 1) / pointArray.Length;
            weight *= adaptiveStrength;
            
            weightedSum += pointArray[i] * weight;
            totalWeight += weight;
        }

        return weightedSum / totalWeight;
    }

    float CalculateVelocity(Vector2[] points)
    {
        if (points.Length < 2)
            return 0f;

        float totalDistance = 0f;
        for (int i = 1; i < points.Length; i++)
        {
            totalDistance += Vector2.Distance(points[i-1], points[i]);
        }

        return totalDistance / (points.Length - 1);
    }

    Vector2? GetMouseHitPoint()
    {
        Ray ray = drawingCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && hit.collider == planeCollider)
        {
            Vector2 textureCoord = hit.textureCoord;
            int pixelX = Mathf.FloorToInt(textureCoord.x * drawableTexture.width);
            int pixelY = Mathf.FloorToInt(textureCoord.y * drawableTexture.height);
            return new Vector2(pixelX, pixelY);
        }

        return null;
    }

    void DrawLineBetweenPoints(Vector2 start, Vector2 end)
    {
        float distance = Vector2.Distance(start, end);
        if (distance < 1f)
        {
            DrawPixelsWithBrush((int)end.x, (int)end.y, brushColor, brushOpacity);
            return;
        }

        int steps = Mathf.CeilToInt(distance / (brushSize * 0.1f)); // High density for smooth lines
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 interpolatedPoint = Vector2.Lerp(start, end, t);
            DrawPixelsWithBrush((int)interpolatedPoint.x, (int)interpolatedPoint.y, brushColor, brushOpacity);
        }
    }

    void DrawPixelsWithBrush(int centerX, int centerY, Color color, float opacity)
    {
        int maskSize = brushMask.GetLength(0);
        int halfSize = maskSize / 2;

        for (int x = 0; x < maskSize; x++)
        {
            for (int y = 0; y < maskSize; y++)
            {
                int texX = centerX - halfSize + x;
                int texY = centerY - halfSize + y;

                if (texX >= 0 && texX < drawableTexture.width && texY >= 0 && texY < drawableTexture.height)
                {
                    float brushAlpha = brushMask[x, y] * opacity;
                    if (brushAlpha > 0.001f)
                    {
                        Color existingColor = drawableTexture.GetPixel(texX, texY);
                        Color newColor = Color.Lerp(existingColor, color, brushAlpha);
                        drawableTexture.SetPixel(texX, texY, newColor);
                    }
                }
            }
        }

        drawableTexture.Apply();
    }

    // Public methods for runtime adjustment
    public void SetBrushSize(int size)
    {
        brushSize = Mathf.Clamp(size, 1, 50);
        CreateBrushMask();
    }

    public void SetBrushColor(Color color)
    {
        brushColor = color;
    }

    public void SetBrushOpacity(float opacity)
    {
        brushOpacity = Mathf.Clamp01(opacity);
    }

    public void SetStabilizationStrength(float strength)
    {
        stabilizationStrength = Mathf.Clamp01(strength);
    }

    public void SetSmoothingWindowSize(int size)
    {
        smoothingWindowSize = Mathf.Clamp(size, 3, 10);
    }

    public void ToggleAdaptiveSmoothing()
    {
        useAdaptiveSmoothing = !useAdaptiveSmoothing;
    }

    // Reset function — call from UI button
    public void ClearTexture()
    {
        StartCoroutine(ClearTextureWithDelay(0.6f));
    }

    private System.Collections.IEnumerator ClearTextureWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        drawableTexture.SetPixels(clearPixels);
        drawableTexture.Apply();
    }

    // Immediate clear function without delay
    public void ForceClear()
    {
        drawableTexture.SetPixels(clearPixels);
        drawableTexture.Apply();
    }

    void OnDestroy()
    {
        if (drawableTexture != null)
            DestroyImmediate(drawableTexture);
        
        if (previewLineObject != null)
            DestroyImmediate(previewLineObject);
    }
}