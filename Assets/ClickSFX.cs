using UnityEngine;

public class ClickSFX : MonoBehaviour
{
    [Header("Sound Settings")]
    [Tooltip("Minimum time between sound plays to prevent duplicates")]
    public float soundCooldown = 0.2f;
    
    private float lastSoundTime = -999f;
    private bool canPlaySound => Time.time - lastSoundTime >= soundCooldown;
    
    public void ClickSound()
    {
        if (!canPlaySound) return;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Click 1");
            lastSoundTime = Time.time;
        }
        else
        {
            Debug.LogWarning("[ClickSFX] AudioManager not found - sound effect skipped");
        }
    }
}