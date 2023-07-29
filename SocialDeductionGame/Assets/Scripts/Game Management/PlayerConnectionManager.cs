using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using System.Linq;

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
    [SerializeField] private Dictionary<ulong, PlayerEntry> _playerDict = new();
    [System.Serializable]
    public class PlayerEntry
    {
        public string PlayerName;
        public GameObject PlayerObject;

        public PlayerEntry(GameObject playerObj)
        {
            PlayerObject = playerObj;
        }

        public void SetName(string name)
        {
            PlayerName = name.ToString();
        }
    }

    // ============== Refrences ==============
    [SerializeField] private TextMeshProUGUI _playersConnectedText;

    // ============== Setup =============
    #region Setup
    public override void OnNetworkSpawn()
    {
        Instance._netNumPlayers.OnValueChanged += UpdatePlayerConnectedText;
        PlayerData.OnChangeName += UpdateNameServerRpc;

        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
        GameManager.OnSetup += AssignRoles;
    }

    public override void OnNetworkDespawn()
    {
        Instance._netNumPlayers.OnValueChanged -= UpdatePlayerConnectedText;
        PlayerData.OnChangeName -= UpdateNameServerRpc;

        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;
        GameManager.OnSetup -= AssignRoles;
    }
    #endregion

    // ============== Client Connection ==============
    #region Client Connection
    private void UpdatePlayerConnectedText(int prev, int next)
    {
        Instance._playersConnectedText.text = "Connected Players: " + next;
    }

    private void ClientConnected(ulong clientID)
    {
        Debug.Log($"SERVER: Client {clientID} connected");
        Instance._netNumPlayers.Value++;
        Instance._playerDict.Add(clientID, new PlayerEntry(NetworkManager.SpawnManager.GetPlayerNetworkObject(clientID).gameObject));
    }

    private void ClientDisconnected(ulong clientID)
    {
        Debug.Log($"SERVER: Client {clientID} disconnected");
        Instance._netNumPlayers.Value--;
        Instance._playerDict.Remove(clientID);
    }
    #endregion

    // ============== Helpers ==============
    #region Helpers
    public static PlayerEntry FindPlayerEntry(ulong id)
    {
        if (Instance._playerDict.TryGetValue(id, out PlayerEntry entry))
            return entry;

        Debug.LogError("Unable to find player with ID: " + id);
        return null;
    }

    public static int GetNumConnectedPlayers()
    {
        Debug.Log("GetNumConnectedPlayers " + Instance._netNumPlayers.Value);
        return Instance._netNumPlayers.Value;
    }

    public static int GetNumLivingPlayers()
    {
        int numAlive = 0;

        foreach(PlayerEntry playa in Instance._playerDict.Values)
        {
            if (playa.PlayerObject.GetComponent<PlayerHealth>().IsLiving())
                numAlive++;
        }
        Debug.Log("GetNumLivingPlayers " + numAlive);
        return numAlive;
    }

    public static List<GameObject> GetLivingPlayerGameObjects()
    {
        List<GameObject> players = new();

        foreach (PlayerEntry playa in Instance._playerDict.Values)
        {
            if (playa.PlayerObject.GetComponent<PlayerHealth>().IsLiving())
                players.Add(playa.PlayerObject);
        }

        return players;
    }
    #endregion

    // ============== Player Names ==============
    #region Player Names
    [ServerRpc(RequireOwnership = false)]
    public void UpdateNameServerRpc(ulong id, string pName)
    {
        Debug.Log("<color=yellow>SERVER: </color> Setting player " + id + " name to: " + pName);
        PlayerEntry curPlayer = FindPlayerEntry(id);
        curPlayer.SetName(pName);
    }

    public static string GetPlayerNameByID(ulong id)
    {
        if (Instance._playerDict.TryGetValue(id, out PlayerEntry entry))
            return entry.PlayerName;

        return null;
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
        ulong rand = Instance._playerDict.Keys.ToArray()[(int)Random.Range(0, Instance._playerDict.Keys.Count - 1)];
        Instance._playerDict[rand].PlayerObject.GetComponent<PlayerData>().SetTeam(PlayerData.Team.Saboteurs);
    }
    #endregion
}
