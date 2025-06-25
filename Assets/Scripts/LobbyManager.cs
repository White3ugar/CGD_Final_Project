using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon; // For Photon.Hashtable

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject playerListPanel;
    public GameObject nameTextPrefab;
    public Transform contentPanel;
    public TMP_Text countdownText;

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
            if (playerCount == 3) // For testing
            {
                countdownStarted = true;
                Debug.LogWarning("Testing mode (2 players). Starting synced countdown...");
                double startTime = PhotonNetwork.Time + 5; // 5s buffer
                photonView.RPC(nameof(StartCountdown), RpcTarget.All, startTime);
            }
            else if (playerCount == 5) // Real game condition
            {
                countdownStarted = true;
                Debug.Log("5 players ready. Starting synced countdown...");
                double startTime = PhotonNetwork.Time + 5;
                photonView.RPC(nameof(StartCountdown), RpcTarget.All, startTime);
            }
        }
    }

    [PunRPC]
    private void StartCountdown(double networkStartTime)
    {
        StartCoroutine(CountdownToGame(networkStartTime));
    }

    private IEnumerator CountdownToGame(double startTime)
    {
        countdownText.gameObject.SetActive(true);

        while (PhotonNetwork.Time < startTime)
        {
            int secondsLeft = Mathf.CeilToInt((float)(startTime - PhotonNetwork.Time));
            countdownText.text = $"Go to library in {secondsLeft} seconds...";
            yield return null;
        }

        countdownText.text = "Teleporting...";
        yield return new WaitForSeconds(1f);

        FindObjectOfType<SpawnManager>()?.TeleportToGameRoom();
    }
}