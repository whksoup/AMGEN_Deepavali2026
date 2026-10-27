using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SignatureManager : MonoBehaviour
{
    public static SignatureManager Instance { get; private set; }
    
    [Header("Signature Properties")]
    public float universalScale = 0.5f;
    public float minRotation = -15f;
    public float maxRotation = 15f;
    
    private string signaturesFolderPath;
    private Dictionary<string, SignatureData> loadedSignatures = new Dictionary<string, SignatureData>();
    private bool signaturesLoaded = false;
    
    [System.Serializable]
    public class SignatureData
    {
        public Sprite sprite;
        public string fileName;
        public string filePath;
        public Vector3 scale;
        public float rotation;
        
        public SignatureData(Sprite sprite, string fileName, string filePath, float universalScale, float rotation)
        {
            this.sprite = sprite;
            this.fileName = fileName;
            this.filePath = filePath;
            this.rotation = rotation;
            
            // Use uniform scaling to prevent stretching
            this.scale = Vector3.one * universalScale;
        }
    }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeManager()
    {
        // Setup save/load path
        signaturesFolderPath = Path.Combine(Application.persistentDataPath, "Signatures");
        if (!Directory.Exists(signaturesFolderPath))
        {
            Directory.CreateDirectory(signaturesFolderPath);
            Debug.Log("Created directory: " + signaturesFolderPath);
        }
        
        // Load all signatures on first initialization
        LoadAllSignatures();
    }
    
    void LoadAllSignatures()
    {
        if (signaturesLoaded) return;
        
        string[] signatureFiles = Directory.GetFiles(signaturesFolderPath, "*.png");
        
        foreach (string filePath in signatureFiles)
        {
            LoadSignatureFromFile(filePath);
        }
        
        signaturesLoaded = true;
        Debug.Log($"Preloaded {loadedSignatures.Count} signatures");
    }
    
    SignatureData LoadSignatureFromFile(string filePath)
    {
        if (loadedSignatures.ContainsKey(filePath)) 
            return loadedSignatures[filePath];
            
        Texture2D texture = LoadTexture(filePath);
        if (texture == null) return null;
        
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
        
        string fileName = Path.GetFileName(filePath);
        float rotation = Random.Range(minRotation, maxRotation);
        
        SignatureData signatureData = new SignatureData(sprite, fileName, filePath, universalScale, rotation);
        loadedSignatures[filePath] = signatureData;
        
        return signatureData;
    }
    
    Texture2D LoadTexture(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        
        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        return texture;
    }
    
    /// <summary>
    /// Check for new signatures and load them
    /// </summary>
    public List<SignatureData> CheckForNewSignatures()
    {
        List<SignatureData> newSignatures = new List<SignatureData>();
        string[] signatureFiles = Directory.GetFiles(signaturesFolderPath, "*.png");
        
        foreach (string filePath in signatureFiles)
        {
            if (!loadedSignatures.ContainsKey(filePath))
            {
                SignatureData newSignature = LoadSignatureFromFile(filePath);
                if (newSignature != null)
                {
                    newSignatures.Add(newSignature);
                    Debug.Log("Found new signature: " + newSignature.fileName);
                }
            }
        }
        
        return newSignatures;
    }
    
    /// <summary>
    /// Get all loaded signatures
    /// </summary>
    public List<SignatureData> GetAllSignatures()
    {
        return loadedSignatures.Values.ToList();
    }
    
    /// <summary>
    /// Get signature count
    /// </summary>
    public int GetSignatureCount()
    {
        return loadedSignatures.Count;
    }
    
    /// <summary>
    /// Check if a specific signature exists
    /// </summary>
    public bool HasSignature(string fileName)
    {
        return loadedSignatures.Values.Any(sig => sig.fileName == fileName);
    }
    
    /// <summary>
    /// Force reload all signatures (useful for testing)
    /// </summary>
    public void ReloadAllSignatures()
    {
        // Clear existing data
        foreach (var signature in loadedSignatures.Values)
        {
            if (signature.sprite != null && signature.sprite.texture != null)
            {
                Destroy(signature.sprite.texture);
                Destroy(signature.sprite);
            }
        }
        loadedSignatures.Clear();
        signaturesLoaded = false;
        
        // Reload everything
        LoadAllSignatures();
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus) // When resuming
        {
            CheckForNewSignatures();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) // When gaining focus
        {
            CheckForNewSignatures();
        }
    }
}