using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class ExileManager : NetworkBehaviour
{
    // ================== Refrences / Variables ==================
    #region Refrences and Variables
    [SerializeField] private GameObject _exileButton;
    [SerializeField] private GameObject _exileUI;
    [SerializeField] private GameObject _closeUIButton;
    [SerializeField] private Transform _voteArea;
    [SerializeField] private GameObject _exileVotePrefab;

    private List<ExileVoteEntry> _voteList = new();
    [SerializeField] private NetworkVariable<int> _netPlayersVoted = new();
    private Dictionary<ulong, bool> _playerVotedDictionary = new();
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
        }
    }
    #endregion

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
        _exileButton.SetActive(true);
    }

    private void DisableExileButton()
    {
        _exileButton.SetActive(false);
    }

    // Called by button
    public void StartExile()
    {
        StartExileServerRpc();
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

        // Show UI
        ShowExileUIClientRpc();
    }

    [ClientRpc]
    public void ShowExileUIClientRpc()
    {
        _exileButton.SetActive(false);

        // Dont let dead players vote
        //if (!_playerHealth.IsLiving())
        //    return;

        // Reset vote objects
        foreach (Transform exilevote in _voteArea)
        {
            ExileVote vote = exilevote.GetComponent<ExileVote>();
            if(vote.GetVotePlayerID() != 999)
                vote.ResetVote(PlayerConnectionManager.Instance.GetPlayerLivingByID(vote.GetVotePlayerID()));
            else
                vote.ResetVote(true);
        }

        _closeUIButton.SetActive(false);
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

        // Increment number of players voted
        _netPlayersVoted.Value++;

        // Submit vote
        ExileVoteEntry voteEntry = FindVoteEntry(VotedID);
        voteEntry.NumVotes++;

        // Track player voted
        _playerVotedDictionary[playerID] = true;

        Debug.Log("<color=yellow>SERVER: </color>" + playerID + "voted for " + VotedID);

        // Test if all players have voted
        if (_netPlayersVoted.Value >= PlayerConnectionManager.Instance.GetNumLivingPlayers())
        {
            Debug.Log("<color=yellow>SERVER: </color> All players have voted");

            // Results list for clients
            int[] results = new int[_voteList.Count];
            int i = 0;

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
            else if(curHeighest.PlayerID == 999)
                Debug.Log("<color=yellow>SERVER: </color> Nobody voted highest, no punishement");
            // kill highest voted player
            else
            {
                Debug.Log("<color=yellow>SERVER: </color> Killing " + curHeighest.PlayerName);
                GameObject playerToExecute = PlayerConnectionManager.Instance.GetPlayerObjectByID(curHeighest.PlayerID);
                playerToExecute.GetComponent<PlayerHealth>().ModifyHealth(-99);
            }

            // Show Results
            ShowResultsClientRpc(results);
        }
    }

    [ClientRpc]
    public void ShowResultsClientRpc(int[] results)
    {
        _closeUIButton.SetActive(true);

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
