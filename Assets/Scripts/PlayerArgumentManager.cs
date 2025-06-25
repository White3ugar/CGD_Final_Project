using System.Collections.Generic;
using UnityEngine;

public class PlayerArgumentManager : MonoBehaviour
{
    public static PlayerArgumentManager Instance;

    // Private list of collected arguments
    private List<string> collectedArguments = new List<string>();

    // Read-only public access (safe exposure)
    public IReadOnlyList<string> CollectedArguments => collectedArguments;

    private void Awake()
    {
        // Ensure singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CollectArgument(string argument)
    {
        collectedArguments.Add(argument);
        Debug.Log("✅ Collected Argument: " + argument);
    }
}
