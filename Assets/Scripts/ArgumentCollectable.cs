using UnityEngine;
using Photon.Pun;
using TMPro;

public class ArgumentCollectable : MonoBehaviourPun
{
    private string[] supportingArguments;
    private string[] refutingArguments;

    private TMP_Text collectPromptText;
    private string finalArgument;
    private string playerRole;

    private bool isInitialized = false;
    private bool isLocalSetupComplete = false;
    private bool isPlayerInside = false;

    void Awake()
    {
        if (!photonView.IsMine)
        {
            Debug.Log($"👻 Hiding non-local collectable. Owner: {photonView.Owner?.NickName}");
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log($"✅ Local player owns this collectable: {photonView.Owner?.NickName}");
        }
    }

    public void Initialize(string[] supportArgs, string[] refuteArgs)
    {
        supportingArguments = supportArgs;
        refutingArguments = refuteArgs;
        isInitialized = true;

        StartCoroutine(LocalSetup());
    }

    private System.Collections.IEnumerator LocalSetup()
    {
        while (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
            yield return null;

        while (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SelectedTopic"))
            yield return null;

        while (ArgumentCollectableUI.LocalCollectPromptText == null)
            yield return null;

        collectPromptText = ArgumentCollectableUI.LocalCollectPromptText;

        playerRole = PhotonNetwork.LocalPlayer.CustomProperties["Role"] as string;
        string topic = PhotonNetwork.CurrentRoom.CustomProperties["SelectedTopic"] as string;
        int topicIndex = TopicToIndex(topic);

        finalArgument = (playerRole == "Debater 1")
            ? supportingArguments[topicIndex]
            : refutingArguments[topicIndex];

        isLocalSetupComplete = true;
    }

    private int TopicToIndex(string topic)
    {
        switch (topic)
        {
            case "Does technology threaten Malaysian cultural heritage?": return 0;
            case "Should traditional festivals be made compulsory in schools?": return 1;
            case "Is learning local dialects still important in modern Malaysia?": return 2;
            case "Should we preserve traditional attire in daily life?": return 3;
            case "Is traditional game better than modern games?": return 4;
            default: return -1;
        }
    }

    private void Update()
    {
        if (!isInitialized || !isLocalSetupComplete || !photonView.IsMine) return;

        if (isPlayerInside && Input.GetKeyDown(KeyCode.F) && !string.IsNullOrEmpty(finalArgument))
        {
            PlayerArgumentManager.Instance?.CollectArgument(finalArgument);
            collectPromptText?.gameObject.SetActive(false);
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized || !isLocalSetupComplete) return;

        if (other.CompareTag("Player"))
        {
            PhotonView playerView = other.GetComponent<PhotonView>();
            if (playerView != null && playerView.IsMine)
            {
                Debug.Log($"🧍 Local player collided: {other.name} (Actor #{playerView.OwnerActorNr})");

                isPlayerInside = true;
                collectPromptText.text = $"\"{finalArgument}\"\n\n<Press F to collect>";
                collectPromptText.gameObject.SetActive(true);
            }
            else
            {
                Debug.Log($"👤 Remote player collided: {other.name} (Actor #{playerView?.OwnerActorNr})");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isInitialized || !isLocalSetupComplete || !photonView.IsMine) return;
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            collectPromptText?.gameObject.SetActive(false);
        }
    }
}