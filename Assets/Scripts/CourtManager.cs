using System.Collections;
using UnityEngine;
using TMPro;

public class CourtManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text GameroomCountdownText;

    private ArgumentUIManager argumentUIManager;

    void Start()
    {
        // Find the ArgumentUIManager in the scene
        argumentUIManager = FindObjectOfType<ArgumentUIManager>();
    }

    public void DisableGameroomCountdownText()
    {
        if (GameroomCountdownText != null)
        {
            GameroomCountdownText.gameObject.SetActive(false);
            Debug.Log("🛑 Gameroom countdown text has been disabled.");

            // Start the debate with sync countdown
            if (argumentUIManager != null)
            {
                argumentUIManager.TriggerDebateStart();
                Debug.Log("⏱️ Triggered synced debate start.");
            }
            else
            {
                Debug.LogWarning("⚠ ArgumentUIManager reference is missing!");
            }
        }
        else
        {
            Debug.LogWarning("⚠ GameroomCountdownText reference is missing!");
        }
    }

    // private IEnumerator ShowArgumentUIAfterDelay(float delay)
    // {
    //     yield return new WaitForSeconds(delay);

    //     if (argumentUIManager != null)
    //     {
    //         argumentUIManager.GenerateArgumentButtons();  // Populate buttons
    //         Debug.Log("🧠 Argument buttons shown after delay.");
    //     }
    //     else
    //     {
    //         Debug.LogWarning("⚠ ArgumentUIManager not found in the scene.");
    //     }
    // }
}
