using UnityEngine;
using TMPro;
using Photon.Pun;

public class DebateTopicManager : MonoBehaviourPun
{
    [Header("UI Reference")]
    public TMP_Text topicText;

    private readonly string[] topics =
    {
        "Is traditional game better than modern games?",
        "Should we preserve traditional attire in daily life?",
        "Is learning local dialects still important in modern Malaysia?",
        "Should traditional festivals be made compulsory in schools?",
        "Does technology threaten Malaysian cultural heritage?"
    };

    void Start()
    {
        topicText.gameObject.SetActive(false); 
    }

    public void DisplayRandomTopic()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        string selectedTopic = topics[Random.Range(0, topics.Length)];

        // Call RPC to sync topic to all clients
        photonView.RPC("ShowTopic", RpcTarget.All, selectedTopic);
    }

    [PunRPC]
    void ShowTopic(string topic)
    {
        topicText.text = $"🗣️ The topic for this game:\n\"{topic}\"";
        topicText.gameObject.SetActive(true);
    }
}
