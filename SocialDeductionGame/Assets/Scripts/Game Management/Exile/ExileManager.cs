using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ExileManager : NetworkBehaviour
{
    // ================== Refrences / Variables ==================
    [SerializeField] private GameObject _nobodyVotePrefab;
    [SerializeField] private GameObject _votePrefab;
    [SerializeField] private Transform _voteArea;
    [SerializeField] private GameObject _exileButton;
    [SerializeField] private GameObject _exileUI;
    [SerializeField] private ExileVote _nobodyVote;
    [SerializeField] private GameObject _closeUIButton;

    [SerializeField] private List<ExileVoteEntry> _voteList = new();

    [SerializeField] private NetworkVariable<int> _netPlayersVoted = new();

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

    // ================== Setup ==================
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

    // ================== Exile ==================
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
        ulong[] playerIDs = new ulong[PlayerConnectionManager.GetNumLivingPlayers()+1];
        int i = 1;

        // Add nobody vote
        _voteList.Add(new ExileVoteEntry(999, "Nobody", null));
        playerIDs[0] = 999;

        // Add entry for each player
        foreach (GameObject playa in PlayerConnectionManager.GetLivingPlayerGameObjects())
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
        // NOTE: MAYBE IN FUTURE MOVE THIS TO A PLAYER UI, that way its in a place where I dont have to get component
        // Dont let dead players vote
        if (!GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHealth>().IsLiving())
            return;

        // Clear old stuff
        foreach (Transform child in _voteArea)
            Destroy(child.gameObject);

        _exileButton.SetActive(false);
        _closeUIButton.SetActive(false);
        _exileUI.SetActive(true);

        // Add nobody vote
        ExileVote nobodyVote = Instantiate(_nobodyVotePrefab, _voteArea).GetComponent<ExileVote>();
        nobodyVote.Setup(999, "Nobody", this);

        // Add entry for each player
        foreach (ulong id in playerIDList)
        {
            if(id != 999)
            {
                // Instantiate a vote box
                var curVote = Instantiate(_votePrefab, _voteArea).GetComponent<ExileVote>();

                // Setup Vote
                ulong pID = id;
                string pName = "Player " + pID.ToString();
                curVote.Setup(pID, pName, this);
            }
        }
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
        if (_netPlayersVoted.Value >= PlayerConnectionManager.GetNumLivingPlayers())
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
        _closeUIButton.SetActive(true);

        int i = 0;

        foreach (Transform child in _voteArea)
        {
            child.GetComponent<ExileVote>().DisplayResults(results[i]);
            i++;
        }
    }
}
