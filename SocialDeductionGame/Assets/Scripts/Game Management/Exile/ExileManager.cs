using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ExileManager : NetworkBehaviour
{
    // ================== Refrences / Variables ==================
    [SerializeField] private GameObject _exileButton;

    private PlayerUI _playerUI;

    private List<ExileVoteEntry> _voteList = new();
    [SerializeField] private NetworkVariable<int> _netPlayersVoted = new();

    #region ExileVoteEntry
    public class ExileVoteEntry
    {
        public ulong PlayerID;
        public string PlayerName;
        public int numVotes;
        public PlayerHealth PlayerHealth;

        public ExileVoteEntry(ulong playerID, string playerName, PlayerHealth playerHealth)
        {
            PlayerID = playerID;
            PlayerName = playerName;
            PlayerHealth = playerHealth;
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
    private void OnEnable()
    {
        GameManager.OnStateEvening += EnableExileButton;
        GameManager.OnStateNight += DisableExileButton;
    }

    private void OnDisable()
    {
        GameManager.OnStateEvening -= EnableExileButton;
        GameManager.OnStateNight -= DisableExileButton;
    }
    #endregion

    // ================== Exile ==================
    #region Exile
    private void EnableExileButton()
    {
        _exileButton.SetActive(true);
    }

    private void DisableExileButton()
    {
        _exileButton.SetActive(false);
    }

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
        _voteList.Clear();

        // Player ID list for clients
        ulong[] playerIDs = new ulong[PlayerConnectionManager.Instance.GetNumLivingPlayers()+1];
        int i = 1;

        // Add nobody vote
        _voteList.Add(new ExileVoteEntry(999, "Nobody", null));
        playerIDs[0] = 999;

        // Add entry for each player
        foreach (GameObject playa in PlayerConnectionManager.Instance.GetLivingPlayerGameObjects())
        {
            // Add vote to list
            ulong pID = playa.GetComponent<PlayerData>().GetPlayerID();
            string pName = "Player " + pID.ToString();

            _voteList.Add(new ExileVoteEntry(pID, pName, playa.GetComponent<PlayerHealth>()));

            // Add to client list
            playerIDs[i] = pID;
            i++;
        }

        // Call to clients
        StartExileClientRpc(playerIDs);
    }

    [ClientRpc]
    public void StartExileClientRpc(ulong[] playerIDList)
    {
        _exileButton.SetActive(false);

        if (_playerUI == null)
            _playerUI = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<PlayerUI>();

        _playerUI.StartExile(playerIDList, this);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitPlayerVoteServerRpc(ulong playerID)
    {
        Debug.Log("<color=yellow>SERVER: </color> Vote for " + playerID + " submitted");

        // Increment number of players voted
        _netPlayersVoted.Value++;

        // Submit vote
        ExileVoteEntry voteEntry = FindVoteEntry(playerID);
        voteEntry.numVotes++;

        // Test if all players have voted
        if (_netPlayersVoted.Value >= PlayerConnectionManager.Instance.GetNumLivingPlayers())
        {
            Debug.Log("<color=yellow>SERVER: </color> All players have voted");

            // Results list for clients
            int[] results = new int[_voteList.Count];
            int i = 0;

            ExileVoteEntry dummy = new ExileVoteEntry(888, "dummy", null);
            ExileVoteEntry curHeighest = dummy;
            ExileVoteEntry prevHeighest = dummy;

            foreach (ExileVoteEntry v in _voteList)
            {
                results[i] = v.numVotes;
                i++;

                // Get highest voted player
                if (v.numVotes >= curHeighest.numVotes)
                {
                    Debug.Log("<color=yellow>SERVER: </color> New highest found: " + v.numVotes + " from " + v.PlayerName);
                    prevHeighest = curHeighest;
                    curHeighest = v;
                }
            }

            // If there is a tie, no punishement
            if (curHeighest.numVotes == prevHeighest.numVotes)
                Debug.Log("<color=yellow>SERVER: </color> Tie for highest vote, no punishement");
            // If nobody highest, no punishement
            else if(curHeighest.PlayerID == 999)
                Debug.Log("<color=yellow>SERVER: </color> Nobody voted highest, no punishement");
            // kill highest voted player
            else
            {
                Debug.Log("<color=yellow>SERVER: </color> Killing " + curHeighest.PlayerName);
                curHeighest.PlayerHealth.ModifyHealth(-99);
            }

            // Show Results
            ShowResultsClientRpc(results);
        }
    }

    [ClientRpc]
    public void ShowResultsClientRpc(int[] results)
    {
        if (_playerUI == null)
            _playerUI = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<PlayerUI>();

        _playerUI.ShowResults(results);
    }
    #endregion
}
