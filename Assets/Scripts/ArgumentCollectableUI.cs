using UnityEngine;
using TMPro;
using Photon.Pun;

public class ArgumentCollectableUI : MonoBehaviourPun
{
    public GameObject collectPromptPrefab;
    public static TMP_Text LocalCollectPromptText { get; private set; }

    private void Start()
    {
        if (photonView.IsMine)
        {
            GameObject uiInstance = Instantiate(collectPromptPrefab);
            uiInstance.transform.SetParent(GameObject.Find("UI").transform, false);
            uiInstance.SetActive(false);

            LocalCollectPromptText = uiInstance.GetComponentInChildren<TMP_Text>(true);
            Debug.Log($"✅ Local collect prompt text initialized: {LocalCollectPromptText.name}");
        }
    }
}