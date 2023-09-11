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
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;

        DontDestroyOnLoad(gameObject);
    }
    #endregion

    // ============== Variables ==============
    #region Variables and Refrences
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private NetworkVariable<int> _netNumPlayers = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netNumLivingPlayers = new(writePerm: NetworkVariableWritePermission.Server);
    private Dictionary<ulong, PlayerEntry> _playerDict = new();
    public class PlayerEntry : INetworkSerializable
    {
        public string PlayerName;
        public GameObject PlayerObject;
        public PlayerData.Team PlayerTeam;
        public int PlayerStyleIndex = 0;
        public int PlayerMaterialIndex = 0;

        // INetworkSerializable
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerName);
        }

        // Constructors
        public PlayerEntry()
        {
            PlayerName = string.Empty;
            PlayerObject = null;
        }

        public PlayerEntry(string playerName, GameObject playerObj = null)
        {
            PlayerName = playerName;
            PlayerObject = playerObj;
        }


        // Functions
        public void SetName(string name)
        {
            PlayerName = name.ToString();
        }

        public void SetObject(GameObject playerObj)
        {
            PlayerObject = playerObj;
        }

        public void SetTeam(PlayerData.Team team)
        {
            PlayerTeam = team;
            PlayerObject.GetComponent<PlayerData>().SetTeam(PlayerData.Team.Saboteurs);
        }

        public void SetVisual(int style, int mat)
        {
            PlayerStyleIndex = style;
            PlayerMaterialIndex = mat;
        }

        public override string ToString()
        {
            string outStr = PlayerName;
            outStr += ", On team " + PlayerTeam.ToString();
            outStr += ", With style" + PlayerStyleIndex + " and material " + PlayerMaterialIndex;
            if (PlayerObject)
                outStr += " object is not null";
            else
                outStr += " object is null";
            return outStr;
        }
    }

    // Ready stuff
    [SerializeField] private NetworkVariable<int> _netPlayersReadied = new(writePerm: NetworkVariableWritePermission.Server);
    private Dictionary<ulong, bool> _playerReadyDictionary = new();

    public delegate void PlayerReadyAction();
    public static event PlayerReadyAction OnPlayerReady;
    public static event PlayerReadyAction OnPlayerUnready;
    public static event PlayerReadyAction OnAllPlayersReady;
    public static event PlayerReadyAction OnPlayerSetupComplete;
    #endregion

    // ============== Setup =============
    #region Setup
    private void Awake()
    {
        InitializeSingleton();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log("<color=yellow>SERVER: </color> Doing Server setup");

            NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SpawnPlayerPrefabs;

            GameManager.OnSetup += AssignRoles;
            GameManager.OnStateMorning += SyncClientPlayerDictServerRpc;
            GameManager.OnStateChange += UpdateNumLivingPlayersServerRpc;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SpawnPlayerPrefabs;

            GameManager.OnSetup -= AssignRoles;
            GameManager.OnStateMorning -= SyncClientPlayerDictServerRpc;
            GameManager.OnStateChange -= UpdateNumLivingPlayersServerRpc;
        }

    }
    #endregion

    // ============== Client Connection ==============
    #region Client Connection
    private void ClientConnected(ulong clientID)
    {
        Debug.Log($"<color=yellow>SERVER: </color> Client {clientID} connected");
        _netNumPlayers.Value++;
        _playerDict.Add(clientID, new PlayerEntry("Player " + clientID));
    }

    private void ClientDisconnected(ulong clientID)
    {
        Debug.Log($"<color=yellow>SERVER: </color> Client {clientID} disconnected");
        _netNumPlayers.Value--;
        _playerDict.Remove(clientID);
    }
    #endregion

    // ============== Client Sync ==============
    #region Client Syncs
    // Updates local client dictionaries to (mostly) match server one
    // Currently only updates with IDs and Names
    // Each morning it is synced. On clients the dict is cleared and then re-added
    [ServerRpc]
    private void SyncClientPlayerDictServerRpc()
    {
        SyncClientPlayerDictClientRpc(_playerDict.Keys.ToArray(), _playerDict.Values.ToArray());
    }

    [ClientRpc]
    private void SyncClientPlayerDictClientRpc(ulong[] iDArry, PlayerEntry[] playerEntyArry)
    {
        if (IsServer)
            return;

        _playerDict.Clear();

        for (int i = 0; i < iDArry.Length; i++)
        {
            Debug.Log("<color=blue>CLIENT: </color> Recieved Id: " + iDArry[i] + " Name: " + playerEntyArry[i].PlayerName);
            _playerDict.Add(iDArry[i], new PlayerEntry(playerEntyArry[i].PlayerName, null));
        }
    }
    #endregion

    // ============== Player Setup ==============
    #region Player Setup
    private void SpawnPlayerPrefabs(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer)
            return;

        // Make sure in game scene
        if (!SceneLoader.IsInScene(SceneLoader.Scene.IslandGameScene))
            return;

        Debug.Log("<color=yellow>SERVER: </color> In Island Game Scene, spawning player prefabs", gameObject);

        // Spawn a player prefab for each connected player
        foreach(ulong clientID in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameObject playerObj = Instantiate(_playerPrefab);
            playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
        }

        PlayerObjectSetup();

        // Invoke setup complete event
        // WARNING: For some god forsaken reason, invoking an event here will cause this function immidiately run a second time. I have no idea why
        //OnPlayerSetupComplete?.Invoke();
    }

    private void PlayerObjectSetup()
    {
        Debug.Log("<color=yellow>SERVER: </color> Adding player objects to dictionary");

        foreach (ulong clientID in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!_playerDict.ContainsKey(clientID))
            {
                Debug.LogError("<color=yellow>SERVER: </color> Player ID not found in dictionary");
                return;
            }

            if (!NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientID).gameObject)
            {
                Debug.LogError("<color=yellow>SERVER: </color> Player ID does not have object");
                return;
            }

            //Debug.Log("BEFORE SETUP " + _playerDict[clientID]);

            PlayerEntry entry = _playerDict[clientID];

            // Add Player objects to dictionary
            entry.SetObject(NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientID).gameObject);

            // Update players name
            entry.PlayerObject.GetComponent<PlayerData>().UpdatePlayerNameServerRPC(entry.PlayerName);

            // Update Players Visuals
            entry.PlayerObject.GetComponentInChildren<PlayerObj>().UpdateCharacterModelClientRPC(entry.PlayerStyleIndex, entry.PlayerMaterialIndex);

            //Debug.Log("AFTER SETUP " + _playerDict[clientID]);
        }

        // Finish Setup Event
        OnPlayerSetupComplete?.Invoke();
    }
    #endregion

    // ============== Player Customization ==============
    #region Player Customization
    public void UpdatePlayerName(ulong id, string pName)
    {
        if (pName == "")
            return;

        UpdatePlayerNameServerRpc(id, pName);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerNameServerRpc(ulong id, string pName)
    {
        Debug.Log("<color=yellow>SERVER: </color> Setting player " + id + " name to: " + pName);
        PlayerEntry curPlayer = FindPlayerEntry(id);
        curPlayer.SetName(pName);
    }

    public void UpdatePlayerVisuals(ulong id, int style, int mat)
    {
        UpdatePlayerVisualsServerRpc(id, style, mat);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerVisualsServerRpc(ulong id, int style, int mat)
    {
        Debug.Log("<color=yellow>SERVER: </color> Setting player " + id + " style to: " + style + " mat to: " + mat);
        PlayerEntry curPlayer = FindPlayerEntry(id);
        curPlayer.SetVisual(style, mat);
    }
    #endregion

    // ============== Player Readying ==============
    #region Player Readying
    public void ReadyPlayer()
    {
        ReadyPlayerServerRpc();
    }

    public void UnreadyPlayer()
    {
        UnreadyPlayerServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReadyPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientID = serverRpcParams.Receive.SenderClientId;

        // Check if player is already Readied
        if (_playerReadyDictionary.ContainsKey(clientID) && _playerReadyDictionary[clientID] == true)
            return;

        // Get client data
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientID }
            }
        };

        // Record ready player on server
        _netPlayersReadied.Value++;
        _playerReadyDictionary[clientID] = true;
        ReadyPlayerClientRpc(clientRpcParams);

        // Check if all players ready
        if (_netPlayersReadied.Value >= GetNumConnectedPlayers())
        {
            ProgressState();
        }
    }

    [ClientRpc]
    private void ReadyPlayerClientRpc(ClientRpcParams clientRpcParams = default)
    {
        OnPlayerReady?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UnreadyPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientID = serverRpcParams.Receive.SenderClientId;

        // Return if player is not Readied
        if (!_playerReadyDictionary.ContainsKey(clientID) || _playerReadyDictionary[clientID] == false)
        {
            Debug.Log("<color=yellow>SERVER: </color> Can't unready, player not ready");
            return;
        }

        // Get client data
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientID }
            }
        };

        // Unready
        _netPlayersReadied.Value--;
        _playerReadyDictionary[clientID] = false;

        UnreadyPlayerClientRpc(clientRpcParams);
    }

    [ClientRpc]
    private void UnreadyPlayerClientRpc(ClientRpcParams clientRpcParams = default)
    {
        OnPlayerUnready?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UnreadyAllPlayersServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Unready
        _netPlayersReadied.Value = 0;
        foreach (ulong key in _playerReadyDictionary.Keys.ToList())
        {
            _playerReadyDictionary[key] = false;
        }
        UnreadyPlayerClientRpc();
    }

    private void ProgressState()
    {
        if (!IsServer) return;

        Debug.Log("<color=yellow>SERVER: </color> All players ready");

        // Unready all players
        UnreadyAllPlayersServerRpc();

        // Send event
        OnAllPlayersReady?.Invoke();
    }

    public int GetNumReadyPlayers()
    {
        return _netPlayersReadied.Value;
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
        ulong rand = _playerDict.Keys.ToArray()[(int)Random.Range(0, _playerDict.Keys.Count)];
        _playerDict[rand].SetTeam(PlayerData.Team.Saboteurs);
    }
    #endregion

    // ============== Helpers ==============
    #region Helpers
    public PlayerEntry FindPlayerEntry(ulong id)
    {
        if (_playerDict.TryGetValue(id, out PlayerEntry entry))
            return entry;

        Debug.LogError("Unable to find player with ID: " + id);
        return null;
    }

    public int GetNumConnectedPlayers()
    {
        Debug.Log("GetNumConnectedPlayers " + _netNumPlayers.Value);
        return _netNumPlayers.Value;
    }

    // Returns the network variable, which only updates at the begenning of each state
    public int GetNumLivingPlayers()
    {
        Debug.Log("GetNumLivingPlayers " + _netNumLivingPlayers.Value);
        return _netNumLivingPlayers.Value;
    }

    // Calculates and returns number of living players
    public int CheckNumLivingPlayers()
    {
        int numAlive = 0;

        foreach (PlayerEntry playa in _playerDict.Values)
        {
            if (playa.PlayerObject.GetComponent<PlayerHealth>().IsLiving())
                numAlive++;
        }

        return numAlive;
    }

    [ServerRpc]
    public void UpdateNumLivingPlayersServerRpc()
    {
        int numAlive = 0;

        foreach (PlayerEntry playa in _playerDict.Values)
        {
            if (playa.PlayerObject.GetComponent<PlayerHealth>().IsLiving())
                numAlive++;
        }

        _netNumLivingPlayers.Value = numAlive;
    }

    public int GetNumLivingOnTeam(PlayerData.Team team)
    {
        int numAlive = 0;

        foreach (PlayerEntry playa in _playerDict.Values)
        {
            if (playa.PlayerObject.GetComponent<PlayerHealth>().IsLiving() && playa.PlayerTeam == team)
                numAlive++;
        }
        Debug.Log(team.ToString() + numAlive);
        return numAlive;
    }

    public List<GameObject> GetLivingPlayerGameObjects()
    {
        List<GameObject> players = new();

        foreach (PlayerEntry playa in _playerDict.Values)
        {
            if (playa.PlayerObject.GetComponent<PlayerHealth>().IsLiving())
                players.Add(playa.PlayerObject);
        }

        return players;
    }

    public string GetPlayerNameByID(ulong id)
    {
        if (_playerDict.TryGetValue(id, out PlayerEntry entry))
            return entry.PlayerName;

        return null;
    }

    public ulong GetThisPlayersID()
    {
        return NetworkManager.Singleton.LocalClientId;
    }

    public GameObject GetPlayerObject(ulong playerID)
    {
        if (!IsServer)
            return null;

        return _playerDict[playerID].PlayerObject;
    }
    #endregion
}
