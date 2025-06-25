using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;

public class ArgumentUIManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject argumentButtonPrefab;
    public Transform argumentButtonPanel;
    public TMP_Text argumentDisplayText;
    public GameObject argumentScrollView;

    [Header("Turn Control")]
    public TMP_Text ChoiceCountdownText;
    private string currentDebaterTurn = "Debater 1";
    private float countdownTime = 10f;
    private bool isTurnActive = false;
    private bool hasTurnEnded = false;
    private Coroutine activeCountdownCoroutine;
    private int turnCounter = 0;
    private int maxTurns = 2;

    [Header("Voting UI")]
    public GameObject votingPanel;
    public Button voteDebater1Button;
    public Button voteDebater2Button;
    private Dictionary<int, string> currentVotes = new Dictionary<int, string>();
    private List<string> roundWinners = new List<string>();

    private int totalVoters = 0;

    [PunRPC]
    void AnnounceRoundWinner(string winner)
    {
        Debug.Log($"Round winner: {winner}");
        roundWinners.Add(winner);
    }

    void Start()
    {
        argumentScrollView?.SetActive(false);
        ChoiceCountdownText?.gameObject.SetActive(false);
        votingPanel?.SetActive(false);

        voteDebater1Button.onClick.AddListener(() => CastVote("Debater 1"));
        voteDebater2Button.onClick.AddListener(() => CastVote("Debater 2"));
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        CountVoters();
    }

    void CountVoters()
    {
        totalVoters = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("Role", out object role) && (string)role == "Voter")
                totalVoters++;
        }
    }

    public void TriggerDebateStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("BeginDebatePhase", RpcTarget.All);
        }
    }

    [PunRPC]
    void BeginDebatePhase()
    {
        StartCoroutine(ShowStartCountdownAndBeginDebate());
    }

    private IEnumerator ShowStartCountdownAndBeginDebate()
    {
        ChoiceCountdownText.gameObject.SetActive(true);
        float preStartCountdown = 5f;
        while (preStartCountdown > 0f)
        {
            ChoiceCountdownText.text = $"Debate starts in {Mathf.Ceil(preStartCountdown)}s...";
            yield return new WaitForSeconds(1f);
            preStartCountdown--;
        }
        ChoiceCountdownText.gameObject.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("StartNewTurn", RpcTarget.All, currentDebaterTurn);
        }
    }

    [PunRPC]
    void StartNewTurn(string debaterRole)
    {
        currentDebaterTurn = debaterRole;

        if (activeCountdownCoroutine != null)
            StopCoroutine(activeCountdownCoroutine);

        activeCountdownCoroutine = StartCoroutine(StartTurnCountdown(debaterRole));
    }

    public IEnumerator StartTurnCountdown(string debaterRole)
    {
        if (turnCounter >= maxTurns)
        {
            EndDebate();
            yield break;
        }

        hasTurnEnded = false;
        isTurnActive = false;
        ChoiceCountdownText.gameObject.SetActive(true);

        string myRole = PhotonNetwork.LocalPlayer.CustomProperties["Role"] as string;

        if (myRole == debaterRole)
        {
            GenerateArgumentButtons();
        }
        else
        {
            argumentScrollView?.SetActive(false);
        }

        float remaining = countdownTime;
        while (remaining > 0 && !hasTurnEnded)
        {
            ChoiceCountdownText.text = $"{debaterRole} choosing argument in {Mathf.Ceil(remaining)}s...";
            yield return new WaitForSeconds(1f);
            remaining--;
        }

        ChoiceCountdownText.gameObject.SetActive(false);

        if (!hasTurnEnded)
        {
            EndTurn();
        }
    }

    void EndTurn()
    {
        if (hasTurnEnded) return;

        hasTurnEnded = true;
        isTurnActive = false;
        argumentScrollView?.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            if (currentDebaterTurn == "Debater 1")
                photonView.RPC("StartNewTurn", RpcTarget.All, "Debater 2");
            else
                photonView.RPC("TriggerVotingPhase", RpcTarget.All);
        }
    }

    [PunRPC]
    void TriggerVotingPhase()
    {
        currentVotes.Clear();

        string myRole = PhotonNetwork.LocalPlayer.CustomProperties["Role"] as string;
        if (myRole == "Voter")
        {
            votingPanel.SetActive(true);
        }
    }

    void CastVote(string winner)
    {
        votingPanel.SetActive(false);
        photonView.RPC("ReceiveVoteResult", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, winner);
    }

    [PunRPC]
    void ReceiveVoteResult(int voterId, string winner)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (!currentVotes.ContainsKey(voterId))
        {
            currentVotes[voterId] = winner;
            Debug.Log($"✅ Vote from actor #{voterId}: {winner}");
        }

        if (currentVotes.Count >= totalVoters)
        {
            string roundWinner = TallyVotes();
            photonView.RPC("AnnounceRoundWinner", RpcTarget.All, roundWinner);

            turnCounter++;

            if (turnCounter >= maxTurns)
            {
                photonView.RPC("EndDebate", RpcTarget.All);
            }
            else
            {
                photonView.RPC("StartNewTurn", RpcTarget.All, "Debater 1");
            }
        }
    }

    string TallyVotes()
    {
        int d1 = 0, d2 = 0;
        foreach (string vote in currentVotes.Values)
        {
            if (vote == "Debater 1") d1++;
            else if (vote == "Debater 2") d2++;
        }
        if (d1 == d2) return "Tie";
        return d1 > d2 ? "Debater 1" : "Debater 2";
    }

    [PunRPC]
    void EndDebate()
    {
        ChoiceCountdownText.gameObject.SetActive(true);
        string winner = CalculateOverallWinner();
        ChoiceCountdownText.text = $"Debate End! Winner: {winner}";
        argumentScrollView?.SetActive(false);
    }

    string CalculateOverallWinner()
    {
        int d1 = 0, d2 = 0;
        foreach (var r in roundWinners)
        {
            if (r == "Debater 1") d1++;
            else if (r == "Debater 2") d2++;
        }
        return d1 == d2 ? "Tie" : (d1 > d2 ? "Debater 1" : "Debater 2");
    }

    public void GenerateArgumentButtons()
    {
        foreach (Transform child in argumentButtonPanel)
        {
            Destroy(child.gameObject);
        }

        var collected = PlayerArgumentManager.Instance.CollectedArguments;

        if (collected.Count == 0) return;

        argumentScrollView?.SetActive(true);

        foreach (string argument in collected)
        {
            GameObject buttonObj = Instantiate(argumentButtonPrefab, argumentButtonPanel);
            TMP_Text btnText = buttonObj.GetComponentInChildren<TMP_Text>();
            btnText.text = argument;
            Button btn = buttonObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnArgumentSelected(argument));
        }

        isTurnActive = true;
    }

    void OnArgumentSelected(string argument)
    {
        if (!isTurnActive || hasTurnEnded) return;

        isTurnActive = false;
        photonView.RPC("SyncArgumentDisplay", RpcTarget.All, argument);
        EndTurn();
    }

    [PunRPC]
    void SyncArgumentDisplay(string argument)
    {
        argumentDisplayText.text = $"Selected Argument:\n\"{argument}\"";
    }
}
