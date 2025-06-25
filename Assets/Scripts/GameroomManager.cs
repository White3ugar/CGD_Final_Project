using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;

[System.Serializable]
public class ArgumentSet
{
    public string[] supportingArguments = new string[5];
    public string[] refutingArguments = new string[5];
}

public class GameroomManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_Text gameroomcountdownText;
    public TMP_Text lobbycountdownText;
    public TMP_Text topicText;
    public TMP_Text roleText;

    [Header("Debate Topics")]
    public string[] topics;

    [SerializeField]
    private List<ArgumentSet> argumentSets = new List<ArgumentSet>();

    private static readonly string[] Roles = { "Debater 1", "Debater 2", "Voter", "Voter", "Voter" };
    private PhotonView photonView;

    private const string TopicKey = "SelectedTopic";

    void Start()
    {
        photonView = GetComponent<PhotonView>();

        if (photonView == null)
            Debug.LogError("❌ PhotonView is missing on GameroomManager!");

        roleText?.gameObject.SetActive(false);
        topicText?.gameObject.SetActive(false);
        gameroomcountdownText?.gameObject.SetActive(false);
    }

    public void TopicSelection()
    {
        if (lobbycountdownText != null)
            lobbycountdownText.gameObject.SetActive(false);
        else
            Debug.LogError("❌ CountdownText reference is missing!");

        if (PhotonNetwork.IsMasterClient)
        {
            SelectAndSetRandomTopic(); // only MasterClient selects topic
        }
    }

    private void SelectAndSetRandomTopic()
    {
        if (topics == null || topics.Length == 0)
        {
            Debug.LogError("❌ No topics assigned!");
            return;
        }

        string selectedTopic = topics[Random.Range(0, topics.Length)];

        // Save topic in room properties
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { TopicKey, selectedTopic } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(TopicKey))
        {
            string topic = PhotonNetwork.CurrentRoom.CustomProperties[TopicKey] as string;
            DisplayTopicToAll(topic);
            StartCoroutine(AssignRolesAfterDelay());
        }
    }

    private void DisplayTopicToAll(string topic)
    {
        if (topicText != null)
        {
            topicText.text = $"The topic for this game:\n{topic}";
            topicText.gameObject.SetActive(true);
        }
    }

    private IEnumerator AssignRolesAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);

        if (PhotonNetwork.IsMasterClient)
            AssignRolesToPlayers();
    }

    private void AssignRolesToPlayers()
    {
        List<Player> players = new List<Player>(PhotonNetwork.PlayerList);
        List<string> shuffledRoles = new List<string>(Roles);

        // 🎲 Shuffle roles (original logic - commented out for testing123)
        /*
        for (int i = 0; i < shuffledRoles.Count; i++)
        {
            int rnd = Random.Range(i, shuffledRoles.Count);
            (shuffledRoles[i], shuffledRoles[rnd]) = (shuffledRoles[rnd], shuffledRoles[i]);
        }
        */

        for (int i = 0; i < players.Count; i++)
        {
            string role = "";

            // 🧪 Test setup: first player is Debater 1, second is Debater 2
            if (i == 0)
                role = "Debater 1";
            else if (i == 1)
                role = "Debater 2";
            else
                role = $"Voter"; // Optional: assign spectator or fallback role

            // ✅ Original role assignment (disabled for testing123)
            // string role = shuffledRoles[i];

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "Role", role } };
            players[i].SetCustomProperties(props);

            if (players[i] == PhotonNetwork.LocalPlayer)
            {
                // 👑 Set own role immediately
                ReceiveRole(role);
            }
            else
            {
                // 📡 Send to other player
                photonView.RPC(nameof(ReceiveRole), players[i], role);
            }
        }

        // Call courtroom countdown for all clients
        double startTime = PhotonNetwork.Time + 60; // 60 seconds countdown
        photonView.RPC(nameof(StartCourtroomCountdown), RpcTarget.All, startTime);
    }

    [PunRPC]
    private void ReceiveRole(string role)
    {
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { "Role", role }
        });

        StartCoroutine(WaitRoleSetAndSpawnCol(role));
    }

    private IEnumerator WaitRoleSetAndSpawnCol(string role)
    {
        while (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
            yield return null;

        string confirmedRole = PhotonNetwork.LocalPlayer.CustomProperties["Role"] as string;
        Debug.Log($"Eventually confirmed role: {confirmedRole}");

        // Spawn collectables only for debaters
        if (confirmedRole == "Debater 1" || confirmedRole == "Debater 2")
        {
            ArgumentSpawner spawner = FindObjectOfType<ArgumentSpawner>();
            if (spawner != null)
            {
                Debug.Log($"🧠 {PhotonNetwork.LocalPlayer.NickName} ({confirmedRole}) is spawning collectables...");
                spawner.SpawnPlayerCollectables(argumentSets);
            }
            else
            {
                Debug.LogWarning("❌ ArgumentSpawner not found in scene!");
            }
        }

        if (roleText != null)
        {
            roleText.text = $"Your role: {confirmedRole}";
            roleText.gameObject.SetActive(true);
        }
    }

    [PunRPC]
    private void StartCourtroomCountdown(double startTime)
    {
        StartCoroutine(CountdownToCourtRoom(startTime));
    }

    private IEnumerator CountdownToCourtRoom(double startTime)
    {
        if (gameroomcountdownText != null)
            gameroomcountdownText.gameObject.SetActive(true);

        while (PhotonNetwork.Time < startTime)
        {
            float remaining = (float)(startTime - PhotonNetwork.Time);
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.CeilToInt(remaining % 60f);

            gameroomcountdownText.text = $"Go to court in {minutes:D2}:{seconds:D2}";
            yield return null;
        }

        gameroomcountdownText.text = "Teleporting...";
        yield return new WaitForSeconds(1f);

        FindObjectOfType<SpawnManager>()?.TeleportToCourtRoom();
    }
}
