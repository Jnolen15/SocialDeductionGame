using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerConnectionManager : NetworkBehaviour
{
    // ============== Singleton pattern ==============
    #region Singleton
    public static PlayerConnectionManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;
    }
    #endregion

    // ============== Variables ==============
    [SerializeField] private NetworkVariable<int> _netNumPlayers = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private List<PlayerEntry> _playerList = new();
    public class PlayerEntry
    {
        public ulong ID;
        public GameObject PlayerObject;

        public PlayerEntry(ulong id, GameObject playerObj)
        {
            ID = id;
            PlayerObject = playerObj;
        }
    }

    // ============== Refrences ==============
    [SerializeField] private TextMeshProUGUI _playersConnected;

    // ============== Setup =============
    #region Setup
    public override void OnNetworkSpawn()
    {
        Instance._netNumPlayers.OnValueChanged += UpdatePlayerList;

        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

        GameManager.OnStateIntro += AssignRoles;
    }

    public override void OnNetworkDespawn()
    {
        Instance._netNumPlayers.OnValueChanged -= UpdatePlayerList;

        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;

        GameManager.OnStateIntro -= AssignRoles;
    }
    #endregion

    // ============== Client Connection ==============
    #region Client Connection
    private void UpdatePlayerList(int prev, int next)
    {
        Instance._playersConnected.text = "Connected Players: " + next;
    }

    private void ClientConnected(ulong clientID)
    {
        Debug.Log($"SERVER: Client {clientID} connected");
        Instance._netNumPlayers.Value++;
        Instance._playerList.Add(new PlayerEntry(clientID, NetworkManager.SpawnManager.GetPlayerNetworkObject(clientID).gameObject));
    }

    private void ClientDisconnected(ulong clientID)
    {
        Debug.Log($"SERVER: Client {clientID} disconnected");
        Instance._netNumPlayers.Value--;
        Instance._playerList.Remove(new PlayerEntry(clientID, NetworkManager.SpawnManager.GetPlayerNetworkObject(clientID).gameObject));
    }

    public static int GetNumConnectedPlayers()
    {
        Debug.Log("GetNumConnectedPlayers " + Instance._netNumPlayers.Value);
        return Instance._netNumPlayers.Value;
    }
    #endregion

    // ============== Roles ==============
    #region Roles
    private void AssignRoles()
    {
        if (!IsServer)
            return;

        AssignRolesServerRpc();
    }

    [ServerRpc]
    public void AssignRolesServerRpc(ServerRpcParams serverRpcParams = default)
    {
        Debug.Log("Assigning Roles");

        // Pick one random player and assign them to team Saboteurs
        int rand = Random.Range(0, Instance._playerList.Count);
        Instance._playerList[rand].PlayerObject.GetComponent<PlayerData>().SetTeam(PlayerData.Team.Saboteurs);
    }
    #endregion
}
