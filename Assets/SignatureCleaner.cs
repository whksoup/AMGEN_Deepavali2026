using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections;

public class SignatureCleaner : MonoBehaviour
{
    [Tooltip("Maximum number of signatures allowed in the folder.")]
    public int maxSignatures = 1000;

    [Tooltip("How often to check (in seconds). Default is every hour.")]
    public float checkInterval = 3600f;

    private string folderPath;

    void Start()
    {
        folderPath = Path.Combine(Application.persistentDataPath, "Signatures");
        StartCoroutine(CheckFolderRoutine());
    }

    IEnumerator CheckFolderRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            CheckAndCleanSignatures();
        }
    }

    public void CheckAndCleanSignatures()
    {
        if (!Directory.Exists(folderPath)) return;

        string[] files = Directory.GetFiles(folderPath, "Signature_*.png");

        if (files.Length > maxSignatures)
        {
            // Target count is 90% of max, rounded up
            int targetCount = Mathf.CeilToInt(maxSignatures * 0.9f);
            int excess = files.Length - targetCount;

            if (excess > 0)
            {
                // Sort by filename (timestamp in name ensures chronological order)
                var orderedFiles = files.OrderBy(f => f).ToList();

                for (int i = 0; i < excess; i++)
                {
                    try
                    {
                        File.Delete(orderedFiles[i]);
                        Debug.Log($"Deleted old signature: {orderedFiles[i]}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to delete {orderedFiles[i]}: {e.Message}");
                    }
                }
                Debug.Log($"Pruned {excess} signatures. Now {files.Length - excess} remain.");
            }
        }
    }
}
