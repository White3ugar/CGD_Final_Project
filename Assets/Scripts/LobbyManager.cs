using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon; // For Photon.Hashtable

// [System.Serializable]
// public class ArgumentSet
// {
//     public string[] supportingArguments = new string[5];
//     public string[] refutingArguments = new string[5];
// }

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject playerListPanel;
    public GameObject nameTextPrefab;
    public Transform contentPanel;
    public TMP_Text countdownText;

    // [SerializeField] private List<ArgumentSet> argumentSets = new List<ArgumentSet>();


    private Dictionary<string, GameObject> playerTexts = new Dictionary<string, GameObject>();
    private bool countdownStarted = false;

    void Start()
    {
        if (playerListPanel == null || contentPanel == null || countdownText == null)
        {
            Debug.LogError("❌ UI references are missing!");
            return;
        }

        countdownText.gameObject.SetActive(false);
        contentPanel.gameObject.SetActive(false);
    }

    public override void OnJoinedRoom()
    {
        contentPanel.gameObject.SetActive(true);

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            AddOrUpdatePlayerText(player);
        }

        CheckStartCondition();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AddOrUpdatePlayerText(newPlayer);
        CheckStartCondition();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RemovePlayerText(otherPlayer);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Role"))
        {
            AddOrUpdatePlayerText(targetPlayer);
        }
    }

    private void AddOrUpdatePlayerText(Player player)
    {
        string displayName = player.NickName;

        if (player.CustomProperties.TryGetValue("Role", out object roleValue))
        {
            displayName += $" - {roleValue}";
        }
        else
        {
            displayName += " - waiting...";
        }

        if (playerTexts.TryGetValue(player.ActorNumber.ToString(), out GameObject existingObj))
        {
            TMP_Text txt = existingObj.GetComponent<TMP_Text>();
            if (txt != null) txt.text = displayName;
        }
        else
        {
            GameObject textObj = Instantiate(nameTextPrefab, contentPanel);
            textObj.GetComponent<TMP_Text>().text = displayName;
            playerTexts[player.ActorNumber.ToString()] = textObj;
        }
    }

    private void RemovePlayerText(Player player)
    {
        if (playerTexts.TryGetValue(player.ActorNumber.ToString(), out GameObject textObj))
        {
            Destroy(textObj);
            playerTexts.Remove(player.ActorNumber.ToString());
        }
    }

    private void CheckStartCondition()
    {
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

        if (PhotonNetwork.IsMasterClient && !countdownStarted)
        {
            // Testing mode for 1 or 2 players, for testing123
            if (playerCount == 1) // start countdown with 2 players for built version, change to 1 for testing in editor
            {
                countdownStarted = true;
                Debug.LogWarning("Only 1/2 player in room. Starting countdown for testing purposes...");
                photonView.RPC(nameof(StartCountdown), RpcTarget.All);
            }
            // Real mode: only trigger when 5 players
            else if (playerCount == 5)
            {
                countdownStarted = true;
                Debug.Log("5 players ready. Starting game...");
                photonView.RPC(nameof(StartCountdown), RpcTarget.All);
            }
            // All other cases
            else
            {
                // Do not start countdown
                return;
            }
        }
    }

    [PunRPC]
    private void StartCountdown()
    {
        StartCoroutine(CountdownToGame());
    }

    private IEnumerator CountdownToGame()
    {
        countdownText.gameObject.SetActive(true);

        int seconds = 5;
        while (seconds > 0)
        {
            countdownText.text = $"Go to library in {seconds} seconds...";
            yield return new WaitForSeconds(1f);
            seconds--;
        }

        countdownText.text = "Teleporting...";
        yield return new WaitForSeconds(1f);

        // Check role before spawning
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Role", out object roleObj))
        {
            string role = roleObj as string;

            /*
            if (role == "Debater 1" || role == "Debater 2")
            {
                ArgumentSpawner spawner = FindObjectOfType<ArgumentSpawner>();
                if (spawner != null)
                {
                    Debug.Log($"🔁 {PhotonNetwork.LocalPlayer.NickName} ({role}) is spawning their own collectables...");
                    spawner.SpawnPlayerCollectables(argumentSets);
                }
                else
                {
                    Debug.LogWarning("❌ ArgumentSpawner not found in scene!");
                }
            }
            else
            {
                Debug.Log($"{PhotonNetwork.LocalPlayer.NickName} is a {role}, no collectables will be spawned.");
            }
            */
        }
        else
        {
            Debug.LogWarning("Player role not found in custom properties.");
        }

        FindObjectOfType<SpawnManager>()?.TeleportToGameRoom();
    }
}
