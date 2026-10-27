using UnityEngine;

public class CameraPriorityController : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera textureRenderCamera;
    
    [Header("Settings")]
    [SerializeField] private int playerCameraPriority = 0;
    [SerializeField] private int textureRenderCameraPriority = -10;
    
    void Start()
    {
        SetupCameras();
    }
    
    void SetupCameras()
    {
        // Ensure we have camera references
        if (playerCamera == null)
        {
            Debug.LogError("Player Camera not assigned!");
            return;
        }
        
        if (textureRenderCamera == null)
        {
            Debug.LogError("Texture Render Camera not assigned!");
            return;
        }
        
        // Set camera priorities (higher priority renders last/on top)
        playerCamera.depth = playerCameraPriority;
        textureRenderCamera.depth = textureRenderCameraPriority;
        
        // Ensure player camera is enabled and active
        playerCamera.enabled = true;
        
        // Keep texture camera enabled so it can render to texture
        textureRenderCamera.enabled = true;
        
        // Ensure only player camera has Audio Listener
        SetupAudioListener();
        
        // Ensure only player camera has MainCamera tag
        SetupMainCameraTag();
        
        // Make sure texture camera doesn't render to screen
        SetupTextureCamera();
        
        Debug.Log($"Camera setup complete. Player camera priority: {playerCamera.depth}, Texture camera priority: {textureRenderCamera.depth}");
    }
    
    void SetupAudioListener()
    {
        // Remove Audio Listener from texture camera
        AudioListener textureAudioListener = textureRenderCamera.GetComponent<AudioListener>();
        if (textureAudioListener != null)
        {
            DestroyImmediate(textureAudioListener);
        }
        
        // Ensure player camera has Audio Listener
        if (playerCamera.GetComponent<AudioListener>() == null)
        {
            playerCamera.gameObject.AddComponent<AudioListener>();
        }
    }
    
    void SetupMainCameraTag()
    {
        // Set player camera as MainCamera
        if (playerCamera.tag != "MainCamera")
        {
            playerCamera.tag = "MainCamera";
        }
        
        // Remove MainCamera tag from texture camera
        if (textureRenderCamera.tag == "MainCamera")
        {
            textureRenderCamera.tag = "Untagged";
        }
    }
    
    void SetupTextureCamera()
    {
        // If texture camera doesn't have a target texture, it will render to screen
        // This ensures it renders to a texture instead
        if (textureRenderCamera.targetTexture == null)
        {
            Debug.LogWarning("Texture Render Camera doesn't have a Target Texture assigned. It may still render to screen.");
        }
        
        // Optional: Set different clear flags if needed
        // textureRenderCamera.clearFlags = CameraClearFlags.SolidColor;
    }
    
    // Public methods to switch cameras if needed during runtime
    public void EnablePlayerCamera()
    {
        playerCamera.enabled = true;
        playerCamera.depth = playerCameraPriority;
    }
    
    public void DisablePlayerCamera()
    {
        playerCamera.enabled = false;
    }
    
    public void SetPlayerCameraPriority(int priority)
    {
        playerCameraPriority = priority;
        playerCamera.depth = priority;
    }
    
    // Method to reassign cameras during runtime if needed
    public void ReassignCameras(Camera newPlayerCamera, Camera newTextureCamera)
    {
        playerCamera = newPlayerCamera;
        textureRenderCamera = newTextureCamera;
        SetupCameras();
    }
}