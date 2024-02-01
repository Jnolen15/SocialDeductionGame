using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using TMPro;

public class ExileManager : NetworkBehaviour
{
    // ================== Refrences / Variables ==================
    #region Refrences and Variables
    [SerializeField] private ExileVoteUI _exileUI;
    [SerializeField] private TrialVoteUI _trialUI;
    private GameManager _gameManager;

    private NetworkVariable<int> _netPlayersVoted = new();
    private Dictionary<ulong, bool> _playerExileVoteDictionary = new();
    private Dictionary<ulong, string> _playerTrialVoteDictionary = new();

    [Header("Phase 1: Exile Vote")]
    private NetworkVariable<bool> _netExileVoteActive = new();
    private List<ExileVoteEntry> _voteList = new();
    private bool _exileVoteStarted;

    [SerializeField] private float _exileVoteTimerMax;
    [SerializeField] private NetworkVariable<float> _netExileVoteTimer = new(writePerm: NetworkVariableWritePermission.Server);

    [Header("Phase 2: Trial Vote")]
    private NetworkVariable<bool> _netTrialActive = new();
    private NetworkVariable<ulong> _netOnTrialPlayerID = new();
    private NetworkVariable<int> _netExileVotes = new();
    private NetworkVariable<int> _netSpareVotes = new();

    [SerializeField] private float _trialVoteTimerMax;
    [SerializeField] private NetworkVariable<float> _netTrialVoteTimer = new(writePerm: NetworkVariableWritePermission.Server);

    public delegate void VoteEvent();
    public static event VoteEvent OnExileVoteComplete;
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
        TabButtonUI.OnExilePressed += ExileButtonPressed;

        if (IsServer)
        {
            GameManager.OnStateIntro += InitializeExileVotes;
            GameManager.OnStateNight += StopVoting;
        }
    }

    private void Start()
    {
        _gameManager = this.GetComponent<GameManager>();
    }

    public override void OnNetworkDespawn()
    {
        TabButtonUI.OnExilePressed -= ExileButtonPressed;

        if (IsServer)
        {
            GameManager.OnStateIntro -= InitializeExileVotes;
            GameManager.OnStateNight -= StopVoting;
        }
    }
    #endregion

    // ================== Update ==================
    #region Update
    private void Update()
    {
        if (!IsServer)
            return;

        // Exile timer
        if(_netExileVoteActive.Value && _netExileVoteTimer.Value >= 0)
        {
            _netExileVoteTimer.Value -= Time.deltaTime;
            if (_netExileVoteTimer.Value <= 0)
            {
                Debug.Log($"<color=yellow>SERVER: </color> {_netExileVoteTimer} Timer up, Vote complete");
                RunVoteCompletetion();
            }
        }

        // Trial timer
        if (_netTrialActive.Value && _netTrialVoteTimer.Value >= 0)
        {
            _netTrialVoteTimer.Value -= Time.deltaTime;
            if (_netTrialVoteTimer.Value <= 0)
            {
                Debug.Log($"<color=yellow>SERVER: </color> {_netTrialVoteTimer} Phase two timer up, Vote complete");
                RunTiralVoteCompleteion();
            }
        }
    }

    public float CalculateExileTimerFill()
    {
        return 1 - (_netExileVoteTimer.Value / _exileVoteTimerMax);
    }

    public float CalculateTrialTimerFill()
    {
        return 1 - (_netTrialVoteTimer.Value / _trialVoteTimerMax);
    }
    #endregion

    // ================== Helpers ==================
    #region Helpers
    public bool ExileStarted()
    {
        return _netExileVoteActive.Value;
    }

    public bool TrialStarted()
    {
        return _netTrialActive.Value;
    }

    // Called by server to interupt and stop voting on end of night
    private void StopVoting()
    {
        if (!IsServer)
            return;

        _netExileVoteActive.Value = false;
        _netTrialActive.Value = false;
        _exileVoteStarted = false;
    }
    #endregion

    // ================== Exile ==================
    #region Exile
    // ~~~~~~ Vote initilization ~~~~~~
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
        _exileUI.InitializeVotePrefabs(playerIDs);
    }

    // Called by button event
    public void ExileButtonPressed()
    {
        if (_netExileVoteActive.Value)
        {
            _exileUI.Show();
        }
        else
        {
            StartExileServerRpc(PlayerConnectionManager.Instance.GetLocalPlayersID());
        }
    }

    // ~~~~~~ Exile Function ~~~~~~
    [ServerRpc(RequireOwnership = false)]
    public void StartExileServerRpc(ulong calledByID, ServerRpcParams serverRpcParams = default)
    {
        if (_exileVoteStarted)
        {
            Debug.Log("<color=yellow>SERVER: </color> Exile vote already complete");
            return;
        }

        Debug.Log("<color=yellow>SERVER: </color> Starting Exile Vote");

        // Clear old stuff
        _netPlayersVoted.Value = 0;
        foreach (ExileVoteEntry v in _voteList)
        {
            v.NumVotes = 0;
        }
        foreach (ulong playerID in _playerExileVoteDictionary.Keys.ToList())
        {
            _playerExileVoteDictionary[playerID] = false;
        }

        _exileVoteStarted = true;
        _netExileVoteActive.Value = true;
        _netExileVoteTimer.Value = _exileVoteTimerMax;

        // Show UI
        ShowExileUIClientRpc(calledByID);
    }

    [ClientRpc]
    public void ShowExileUIClientRpc(ulong calledByID)
    {
        // Dont let dead players vote
        if (!PlayerConnectionManager.Instance.GetPlayerLivingByID(PlayerConnectionManager.Instance.GetLocalPlayersID()))
        {
            Debug.Log("<color=blue>CLIENT: </color>Player is dead, and cannot vote");
            return;
        }

        _exileUI.UpdateExileUI(PlayerConnectionManager.Instance.GetPlayerNameByID(calledByID));
    }

    public void SubmitVote(ulong playerID, ulong VotedID)
    {
        SubmitPlayerVoteServerRpc(playerID, VotedID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitPlayerVoteServerRpc(ulong playerID, ulong VotedID)
    {
        // Check if player hasn't already voted
        if (_playerExileVoteDictionary.ContainsKey(playerID) && _playerExileVoteDictionary[playerID] == true)
        {
            Debug.Log("<color=yellow>SERVER: </color> Player " + playerID + " already voted!");
            return;
        }

        // Check to make sure vote is still going on
        if (!_netExileVoteActive.Value)
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
        _playerExileVoteDictionary[playerID] = true;

        Debug.Log("<color=yellow>SERVER: </color>" + playerID + "voted for " + VotedID);

        // Test if all players have voted
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
        _netExileVoteActive.Value = false;

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
        // Go to phase two with highest voted player
        else
        {
            StartTrialVote(curHeighest.PlayerID);
        }

        // Show Results
        ShowResultsClientRpc(results);
    }

    [ClientRpc]
    public void ShowResultsClientRpc(int[] results)
    {
        _exileUI.ShowResults(results);

        OnExileVoteComplete?.Invoke();
    }
    #endregion

    // ================== Trial ==================
    #region Trial
    private void StartTrialVote(ulong playerID)
    {
        if (!IsServer)
            return;

        // Clear old stuff
        _netExileVotes.Value = 0;
        _netSpareVotes.Value = 0;
        foreach (ulong pID in _playerTrialVoteDictionary.Keys.ToList())
        {
            _playerTrialVoteDictionary[pID] = "none";
        }

        // Add time
        if (_gameManager != null)
            _gameManager.PauseCurrentTimer(_trialVoteTimerMax);
        else
            Debug.LogWarning("Exile Manager does not have Game Manager Refrence!");

        // Start
        _netOnTrialPlayerID.Value = playerID;
        _netTrialActive.Value = true;
        _netTrialVoteTimer.Value = _trialVoteTimerMax;

        SetupPhaseTwoClientRpc(playerID);
    }

    [ClientRpc]
    private void SetupPhaseTwoClientRpc(ulong playerID)
    {
        // Dont let dead players vote
        if (!PlayerConnectionManager.Instance.GetPlayerLivingByID(PlayerConnectionManager.Instance.GetLocalPlayersID()))
        {
            _trialUI.Setup(playerID, false);
        }
        else
        {
            _trialUI.Setup(playerID, true);
        }
    }

    public void SubmitExileVote()
    {
        SubmitTrialVoteServerRpc(PlayerConnectionManager.Instance.GetLocalPlayersID(), true);
    }

    public void SubmitSpareVote()
    {
        SubmitTrialVoteServerRpc(PlayerConnectionManager.Instance.GetLocalPlayersID(), false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitTrialVoteServerRpc(ulong playerID, bool vote)
    {
        // Make sure player voting isn't player on trial
        if (playerID == _netOnTrialPlayerID.Value)
        {
            Debug.Log("<color=yellow>SERVER: </color> Player " + playerID + " is on trial and cant vote!");
            return;
        }

        // If player already voted, remove previous vote if new one is different
        if (_playerTrialVoteDictionary.ContainsKey(playerID))
        {
            if (_playerTrialVoteDictionary[playerID] == "exile" && !vote) // Previously voted exile but now vote spare
            {
                _netExileVotes.Value--;
                Debug.Log("<color=yellow>SERVER: </color> Player " + playerID + " already voted exile, removing that vote");
            }
            else if (_playerTrialVoteDictionary[playerID] == "exile" && vote) // Prev is exile and new is exile
                return;
            else if (_playerTrialVoteDictionary[playerID] == "spare" && vote) // Previously voted spare but now vote exile
            {
                _netSpareVotes.Value--;
                Debug.Log("<color=yellow>SERVER: </color> Player " + playerID + " already voted spare, removing that vote");
            }
            else if (_playerTrialVoteDictionary[playerID] == "spare" && !vote) // Prev is spare and new is spare
                return;
        }

        if (vote)
        {
            _netExileVotes.Value++;
            _playerTrialVoteDictionary[playerID] = "exile";
            Debug.Log("<color=yellow>SERVER: </color> Player " + playerID + " voted exile");
        }
        else
        {
            _netSpareVotes.Value++;
            _playerTrialVoteDictionary[playerID] = "spare";
            Debug.Log("<color=yellow>SERVER: </color> Player " + playerID + " voted spare");
        }

        UpdateTrialResultsClientRpc(_netExileVotes.Value, _netSpareVotes.Value);
    }

    private void RunTiralVoteCompleteion()
    {
        if (!IsServer)
            return;

        Debug.Log("<color=yellow>SERVER: </color> All players have voted, or time ran out");
        _netTrialActive.Value = false;

        // majority exile
        if (_netExileVotes.Value > _netSpareVotes.Value)
        {
            Debug.Log("<color=yellow>SERVER: </color> Killing " + _netOnTrialPlayerID.Value);
            if (PlayerConnectionManager.Instance.FindPlayerEntry(_netOnTrialPlayerID.Value) != null)
            {
                GameObject playerToExecute = PlayerConnectionManager.Instance.GetPlayerObjectByID(_netOnTrialPlayerID.Value);

                // Award Suffering if they were a survivor
                if (playerToExecute.GetComponent<PlayerData>().GetPlayerTeam() == PlayerData.Team.Survivors)
                    SufferingManager.Instance.ModifySuffering(3, 104, true);

                // Kill Plauer
                playerToExecute.GetComponent<PlayerHealth>().ModifyHealth(-99, "Exile");
            }
            else
            {
                Debug.Log("<color=yellow>SERVER: </color> TOP VOTED PLAYER NOT FOUND!");
            }
        }
        // Majority spare
        else if(_netExileVotes.Value < _netSpareVotes.Value)
        {
            Debug.Log("<color=yellow>SERVER: </color> Majority voted spare, no punishement");
        }
        // Tie
        else
        {
            Debug.Log("<color=yellow>SERVER: </color> Tie for highest vote, no punishement");
        }

        TrialVoteEndedClientRpc();
    }

    [ClientRpc]
    private void UpdateTrialResultsClientRpc(int exileVotes, int SpareVotes)
    {
        _trialUI.UpdateTrialResults(exileVotes, SpareVotes);
    }
    
    [ClientRpc]
    private void TrialVoteEndedClientRpc()
    {
        _trialUI.VoteEnded();
    }

    #endregion
}
