using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using UnityEngine.UI;

public class ExileManager : NetworkBehaviour
{
    // ================== Refrences / Variables ==================
    #region Refrences and Variables
    [SerializeField] private GameObject _exileButton;
    [SerializeField] private GameObject _exileUI;
    [SerializeField] private Transform _voteArea;
    [SerializeField] private GameObject _exileVotePrefab;
    [SerializeField] private Image _voteTimerFill;

    [SerializeField] private NetworkVariable<bool> _netExileVoteStarted = new();
    private List<ExileVoteEntry> _voteList = new();
    [SerializeField] private NetworkVariable<int> _netPlayersVoted = new();
    private Dictionary<ulong, bool> _playerVotedDictionary = new();

    [SerializeField] private float _voteTimerMax;
    [SerializeField] private NetworkVariable<float> _netVoteTimer = new(writePerm: NetworkVariableWritePermission.Server);
    #endregion

    #region ExileVoteEntry
    public class ExileVoteEntry
    {
        public ulong PlayerID;
        public string PlayerName;
        public int NumVotes;

        public ExileVoteEntry(ulong playerID, string playerName)
        {
            PlayerID = playerID;
            PlayerName = playerName;
        }
    }

    private ExileVoteEntry FindVoteEntry(ulong playerID)
    {
        foreach (ExileVoteEntry v in _voteList)
        {
            if (v.PlayerID == playerID)
                return v;
        }

        return null;
    }
    #endregion

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateEvening += EnableExileButton;
        GameManager.OnStateNight += DisableExileButton;
        GameManager.OnStateNight += CloseExileVote;

        if (IsServer)
        {
            GameManager.OnStateIntro += InitializeExileVotes;
            PlayerConnectionManager.OnPlayerDisconnect += TestForCompletionOnClientDisconnect;
        }
    }

    public override void OnNetworkDespawn()
    {
        GameManager.OnStateEvening -= EnableExileButton;
        GameManager.OnStateNight -= DisableExileButton;
        GameManager.OnStateNight -= CloseExileVote;

        if (IsServer)
        {
            GameManager.OnStateIntro -= InitializeExileVotes;
            PlayerConnectionManager.OnPlayerDisconnect -= TestForCompletionOnClientDisconnect;
        }
    }
    #endregion

    // ================== Update ==================
    private void Update()
    {
        // Update timer
        if(_netExileVoteStarted.Value)
            _voteTimerFill.fillAmount = 1 - (_netVoteTimer.Value / _voteTimerMax);

        if (!IsServer)
            return;

        if(_netExileVoteStarted.Value && _netVoteTimer.Value >= 0)
            RunTimer(_netVoteTimer);
    }

    private void RunTimer(NetworkVariable<float> timer)
    {
        timer.Value -= Time.deltaTime;
        if (timer.Value <= 0)
        {
            Debug.Log($"<color=yellow>SERVER: </color> {timer} Timer up, Vote complete");
            RunVoteCompletetion();
        }
    }

    // ================== Exile ==================
    #region Exile
    private void InitializeExileVotes()
    {
        if (!IsServer)
        {
            Debug.LogError("Server only function not called by server");
            return;
        }

        Debug.Log("<color=yellow>SERVER: </color>Setting up exile votes");

        // ID list for clients
        ulong[] playerIDs = new ulong[PlayerConnectionManager.Instance.GetNumConnectedPlayers()+1];

        // Add nobody vote
        _voteList.Add(new ExileVoteEntry(999, "Nobody"));
        playerIDs[0] = 999;

        // Add entry for each player
        int i = 1;
        foreach (ulong playerID in PlayerConnectionManager.Instance.GetPlayerIDs())
        {
            string pName = PlayerConnectionManager.Instance.GetPlayerNameByID(playerID);

            // Add vote to list
            _voteList.Add(new ExileVoteEntry(playerID, pName));

            playerIDs[i] = playerID;

            i++;
        }

        InitializeVotePrefabsClientRpc(playerIDs);
    }

    [ClientRpc]
    private void InitializeVotePrefabsClientRpc(ulong[] playerIDs)
    {
        // Initialize vote prefabs
        for(int i = 0; i < playerIDs.Length; i++)
        {
            GameObject vote = Instantiate(_exileVotePrefab, _voteArea);
            //vote.transform.SetParent(_voteArea, false);
            vote.GetComponent<ExileVote>().Setup(playerIDs[i], PlayerConnectionManager.Instance.GetPlayerNameByID(playerIDs[i]));
            Debug.Log("<color=blue>CLIENT: </color>Spawned an exile vote", vote);
        }
    }

    private void EnableExileButton()
    {
        if (!PlayerConnectionManager.Instance.GetPlayerLivingByID(PlayerConnectionManager.Instance.GetLocalPlayersID()))
        {
            Debug.Log("<color=blue>CLIENT: </color>Player is dead, and cannot vote");
            return;
        }

        _exileButton.SetActive(true);
    }

    private void DisableExileButton()
    {
        _exileButton.SetActive(false);
    }

    // Called by button
    public void ExileButtonPressed()
    {
        if (_netExileVoteStarted.Value)
        {
            _exileUI.SetActive(true);
        }
        else
        {
            StartExileServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartExileServerRpc(ServerRpcParams serverRpcParams = default)
    {
        Debug.Log("<color=yellow>SERVER: </color> Starting Exile Vote");

        // Clear old stuff
        _netPlayersVoted.Value = 0;
        foreach (ExileVoteEntry v in _voteList)
        {
            v.NumVotes = 0;
        }
        foreach (ulong playerID in _playerVotedDictionary.Keys.ToList())
        {
            _playerVotedDictionary[playerID] = false;
        }

        _netExileVoteStarted.Value = true;
        _netVoteTimer.Value = _voteTimerMax;

        // Show UI
        ShowExileUIClientRpc();
    }

    [ClientRpc]
    public void ShowExileUIClientRpc()
    {
        // Dont let dead players vote
        if (!PlayerConnectionManager.Instance.GetPlayerLivingByID(PlayerConnectionManager.Instance.GetLocalPlayersID()))
        {
            Debug.Log("<color=blue>CLIENT: </color>Player is dead, and cannot vote");
            return;
        }

        // Reset vote objects
        foreach (Transform exilevote in _voteArea)
        {
            ExileVote vote = exilevote.GetComponent<ExileVote>();
            // Reset player votes, show if the player is dead or living (or dead if diconnected)
            if(vote.GetVotePlayerID() != 999)
            {
                vote.ResetVote(PlayerConnectionManager.Instance.GetPlayerLivingByID(vote.GetVotePlayerID()));
            }
            else // Nobody vote
                vote.ResetVote(true);
        }

        _exileUI.SetActive(true);
    }

    public void SubmitVote(ulong playerID, ulong VotedID)
    {
        SubmitPlayerVoteServerRpc(playerID, VotedID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitPlayerVoteServerRpc(ulong playerID, ulong VotedID)
    {
        // Check if player hasn't already voted
        if (_playerVotedDictionary.ContainsKey(playerID) && _playerVotedDictionary[playerID] == true)
        {
            Debug.Log("<color=yellow>SERVER: </color> Player " + playerID + " already voted!");
            return;
        }

        // Check to make sure vote is still going on
        if (!_netExileVoteStarted.Value)
        {
            Debug.Log("<color=yellow>SERVER: </color> Player " + playerID + " voted too late");
            return;
        }

        // Increment number of players voted
        _netPlayersVoted.Value++;

        // Submit vote
        ExileVoteEntry voteEntry = FindVoteEntry(VotedID);
        voteEntry.NumVotes++;

        // Track player voted
        _playerVotedDictionary[playerID] = true;

        Debug.Log("<color=yellow>SERVER: </color>" + playerID + "voted for " + VotedID);

        // Test if all players have voted
        TestForVoteCompletetion();
    }

    private void TestForCompletionOnClientDisconnect(ulong playerID)
    {
        Debug.Log("<color=yellow>SERVER: </color> Client disconnected, testing for vote completion");
        TestForVoteCompletetion();
    }

    private void TestForVoteCompletetion()
    {
        if (!IsServer)
            return;

        if (_netPlayersVoted.Value >= PlayerConnectionManager.Instance.GetNumLivingPlayers())
        {
            RunVoteCompletetion();
        }
    }

    private void RunVoteCompletetion()
    {
        if (!IsServer)
            return;

        Debug.Log("<color=yellow>SERVER: </color> All players have voted, or time ran out");
        _netExileVoteStarted.Value = false;

        // Results list for clients
        int[] results = new int[_voteList.Count];
        int i = 0;

        // Find highest voted
        ExileVoteEntry dummy = new ExileVoteEntry(888, "dummy");
        ExileVoteEntry curHeighest = dummy;
        ExileVoteEntry prevHeighest = dummy;

        foreach (ExileVoteEntry v in _voteList)
        {
            results[i] = v.NumVotes;
            i++;

            // Get highest voted player
            if (v.NumVotes >= curHeighest.NumVotes)
            {
                Debug.Log("<color=yellow>SERVER: </color> New highest found: " + v.NumVotes + " from " + v.PlayerName);
                prevHeighest = curHeighest;
                curHeighest = v;
            }
        }

        // If there is a tie, no punishement
        if (curHeighest.NumVotes == prevHeighest.NumVotes)
            Debug.Log("<color=yellow>SERVER: </color> Tie for highest vote, no punishement");
        // If nobody highest, no punishement
        else if (curHeighest.PlayerID == 999)
            Debug.Log("<color=yellow>SERVER: </color> Nobody voted highest, no punishement");
        // kill highest voted player
        else
        {
            Debug.Log("<color=yellow>SERVER: </color> Killing " + curHeighest.PlayerName);
            if (PlayerConnectionManager.Instance.FindPlayerEntry(curHeighest.PlayerID) != null)
            {
                GameObject playerToExecute = PlayerConnectionManager.Instance.GetPlayerObjectByID(curHeighest.PlayerID);
                playerToExecute.GetComponent<PlayerHealth>().ModifyHealth(-99);
            }
            else
            {
                Debug.Log("<color=yellow>SERVER: </color> TOP VOTED PLAYER NOT FOUND!");
            }
        }

        // Show Results
        ShowResultsClientRpc(results);
    }

    [ClientRpc]
    public void ShowResultsClientRpc(int[] results)
    {
        _exileUI.SetActive(true);

        int i = 0;
        foreach (Transform child in _voteArea)
        {
            child.GetComponent<ExileVote>().DisplayResults(results[i]);
            i++;
        }
    }

    // Called when the state transitions.
    // This matters if the timer ends but players are not done voting.
    private void CloseExileVote()
    {
        _exileUI.SetActive(false);
    }
    #endregion
}
