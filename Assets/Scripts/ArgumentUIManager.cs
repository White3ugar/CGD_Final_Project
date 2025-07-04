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
    private Coroutine votingCountdownCoroutine;
    private float votingCountdownTime = 10f;

    private Dictionary<int, string> currentVotes = new Dictionary<int, string>();
    private List<string> roundWinners = new List<string>();
    private int totalVoters = 0;

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
        Debug.Log($"Total voters in room: {totalVoters}");
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
            photonView.RPC("StartNewTurn", RpcTarget.All, currentDebaterTurn, PhotonNetwork.Time);
        }
    }

    [PunRPC]
    void StartNewTurn(string debaterRole, double startTime)
    {
        currentDebaterTurn = debaterRole;

        if (activeCountdownCoroutine != null)
            StopCoroutine(activeCountdownCoroutine);

        Debug.Log($"{debaterRole} starts choosing argument...");
        activeCountdownCoroutine = StartCoroutine(StartTurnCountdown(debaterRole, startTime));
    }

    public IEnumerator StartTurnCountdown(string debaterRole, double startTime)
    {
        if (turnCounter >= maxTurns)
        {
            EndDebate();
            yield break;
        }

        double endTime = startTime + countdownTime;
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

        while (!hasTurnEnded)
        {
            double remaining = endTime - PhotonNetwork.Time;
            if (remaining <= 0)
                break;

            ChoiceCountdownText.text = $"{debaterRole} choosing argument in {Mathf.CeilToInt((float)remaining)}s...";
            yield return new WaitForSeconds(1f);
        }

        if (!hasTurnEnded)
        {
            photonView.RPC("RPC_EndTurn", RpcTarget.All);
        }
    }

    void OnArgumentSelected(string argument)
    {
        if (!isTurnActive || hasTurnEnded) return;

        isTurnActive = false;
        Debug.Log($"{currentDebaterTurn} finished choosing argument: {argument}");
        photonView.RPC("SyncArgumentDisplay", RpcTarget.All, argument);
        photonView.RPC("RPC_EndTurn", RpcTarget.All);
    }

    [PunRPC]
    void RPC_EndTurn()
    {
        if (hasTurnEnded) return;

        hasTurnEnded = true;
        isTurnActive = false;
        ChoiceCountdownText.gameObject.SetActive(false);
        argumentScrollView?.SetActive(false);

        Debug.Log($"End of {currentDebaterTurn}'s turn.");

        if (PhotonNetwork.IsMasterClient)
        {
            if (currentDebaterTurn == "Debater 1")
                photonView.RPC("StartNewTurn", RpcTarget.All, "Debater 2", PhotonNetwork.Time);
            else
                photonView.RPC("TriggerVotingPhase", RpcTarget.All, PhotonNetwork.Time);
        }
    }

   [PunRPC]
    void TriggerVotingPhase(double startTime)
    {
        CountVoters();
        currentVotes.Clear();

        if (totalVoters == 0)
        {
            photonView.RPC("NotifyNoVoters", RpcTarget.All);
            photonView.RPC("AnnounceRoundWinner", RpcTarget.All, "No Winner");

            turnCounter++;

            if (turnCounter >= maxTurns)
                photonView.RPC("EndDebate", RpcTarget.All);
            else
                photonView.RPC("StartNewTurn", RpcTarget.All, "Debater 1", PhotonNetwork.Time);

            return;
        }

        Debug.Log("Voting phase started for all players.");

        // Stop previous coroutine if any
        if (votingCountdownCoroutine != null)
            StopCoroutine(votingCountdownCoroutine);

        votingCountdownCoroutine = StartCoroutine(VotingCountdown(startTime));

        // Voters only see voting UI
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Role", out object role) && (string)role == "Voter")
        {
            votingPanel.SetActive(true);
        }
    }

    IEnumerator VotingCountdown(double startTime)
    {
        double endTime = startTime + votingCountdownTime;
        ChoiceCountdownText.gameObject.SetActive(true);

        while (true)
        {
            double remaining = endTime - PhotonNetwork.Time;
            if (remaining <= 0)
                break;

            ChoiceCountdownText.text = $"Voter voting in {Mathf.CeilToInt((float)remaining)}s...";
            yield return new WaitForSeconds(1f);
        }

        ChoiceCountdownText.gameObject.SetActive(false);

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Role", out object role) && (string)role == "Voter")
        {
            votingPanel.SetActive(false);
            Debug.Log("Voter timed out. No vote submitted.");
            photonView.RPC("ReceiveVoteResult", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, "NoVote");
        }
    }

    void CastVote(string winner)
    {
        votingPanel.SetActive(false);
        ChoiceCountdownText.gameObject.SetActive(false);
        if (votingCountdownCoroutine != null) StopCoroutine(votingCountdownCoroutine);
        photonView.RPC("ReceiveVoteResult", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, winner);
    }

    [PunRPC]
    void ReceiveVoteResult(int voterId, string winner)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (!currentVotes.ContainsKey(voterId) && winner != "NoVote")
        {
            currentVotes[voterId] = winner;
            Debug.Log($"Vote received from #{voterId}: {winner}");
        }

        if (currentVotes.Count >= totalVoters)
        {
            Debug.Log("Voting phase complete.");

            string roundWinner = TallyVotes();
            photonView.RPC("AnnounceRoundWinner", RpcTarget.All, roundWinner);

            turnCounter++;
            Debug.Log($"Round {turnCounter} complete.");

            if (turnCounter >= maxTurns)
                photonView.RPC("EndDebate", RpcTarget.All);
            else
                photonView.RPC("StartNewTurn", RpcTarget.All, "Debater 1", PhotonNetwork.Time);
        }
    }

    [PunRPC]
    void NotifyNoVoters()
    {
        ChoiceCountdownText.gameObject.SetActive(true);
        ChoiceCountdownText.text = "No voters present. Skipping voting phase.";
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
    void AnnounceRoundWinner(string winner)
    {
        Debug.Log($"Round winner: {winner}");
        roundWinners.Add(winner);
    }

    [PunRPC]
    void EndDebate()
    {
        Debug.Log("EndDebate called on " + PhotonNetwork.LocalPlayer.NickName);

        // Stop any ongoing countdowns
        if (activeCountdownCoroutine != null)
        {
            StopCoroutine(activeCountdownCoroutine);
            activeCountdownCoroutine = null;
        }
        if (votingCountdownCoroutine != null)
        {
            StopCoroutine(votingCountdownCoroutine);
            votingCountdownCoroutine = null;
        }

        // Hide all panels
        ChoiceCountdownText.gameObject.SetActive(true);
        votingPanel?.SetActive(false);
        argumentScrollView?.SetActive(false);

        // Calculate winner (only once by MasterClient)
        string winner = CalculateOverallWinner();
        ChoiceCountdownText.text = $"Debate End! Winner: {winner}";
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

    [PunRPC]
    void SyncArgumentDisplay(string argument)
    {
        argumentDisplayText.text = $"Selected Argument:\n\"{argument}\"";
    }
}
