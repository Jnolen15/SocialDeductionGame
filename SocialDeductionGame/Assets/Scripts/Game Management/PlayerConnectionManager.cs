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
        private int PlayerStyleIndex = 0;
        private int PlayerMaterialIndex = 0;
        private bool PlayerLiving = true;

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
        public void SetPlayerLiving(bool isLiving)
        {
            PlayerLiving = isLiving;
        }


        public int GetPlayerStyle()
        {
            return PlayerStyleIndex;
        }

        public int GetPlayerMaterial()
        {
            return PlayerMaterialIndex;
        }

        public bool GetPlayerLiving()
        {
            return PlayerLiving;
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

            PlayerEntry entry = _playerDict[clientID];

            // Add Player objects to dictionary
            entry.SetObject(NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientID).gameObject);

            // Update players name in game
            entry.PlayerObject.GetComponent<PlayerData>().UpdatePlayerNameServerRPC(entry.PlayerName);

            // Update Players Visuals
            entry.PlayerObject.GetComponentInChildren<PlayerObj>().UpdateCharacterModelClientRPC(entry.GetPlayerStyle(), entry.GetPlayerMaterial());

            // Update player living tracker
            _netNumLivingPlayers.Value++;
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
        if (SceneLoader.IsInScene(SceneLoader.Scene.IslandGameScene))
        {
            // If in game scene check against number of living players
            if (_netPlayersReadied.Value >= GetNumLivingPlayers())
                ProgressState();
        } else
        {
            // Otherwise check against total connected players
            if (_netPlayersReadied.Value >= GetNumConnectedPlayers())
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

        Debug.Log("<color=yellow>SERVER: </color>Assigning player roles");

        // Pick one random player and assign them to team Saboteurs
        ulong rand = _playerDict.Keys.ToArray()[(int)Random.Range(0, _playerDict.Keys.Count)];
        _playerDict[rand].SetTeam(PlayerData.Team.Saboteurs);
    }
    #endregion

    // ============== Helpers ==============
    #region Helpers
    // ~~~~~~~~~ Return Data From Player Dictionary ~~~~~~~~~
    // Server only
    public PlayerEntry FindPlayerEntry(ulong id)
    {
        if (!IsServer)
        {
            Debug.LogError("Server only function not called by server");
            return null;
        }

        if (_playerDict.TryGetValue(id, out PlayerEntry entry))
            return entry;

        Debug.LogError("<color=yellow>SERVER: </color>Unable to find player with ID: " + id);
        return null;
    }

    // TODO: REWORK THIS? ALSO REWORK EXILE VOTING or look at it at least
    public string GetPlayerNameByID(ulong id)
    {
        if (_playerDict.TryGetValue(id, out PlayerEntry entry))
            return entry.PlayerName;

        return null;
    }

    // Server only
    public GameObject GetPlayerObjectByID(ulong playerID)
    {
        if (!IsServer)
        {
            Debug.LogError("Server only function not called by server");
            return null;
        }

        return _playerDict[playerID].PlayerObject;
    }

    // ~~~~~~~~~ Return Network Vairables ~~~~~~~~~
    // Server or Client
    public int GetNumConnectedPlayers()
    {
        return _netNumPlayers.Value;
    }

    // Server or Client
    public int GetNumLivingPlayers()
    {
        return _netNumLivingPlayers.Value;
    }


    // ~~~~~~~~~ Return NetworkManager Data ~~~~~~~~~
    // Server or Client
    public ulong GetLocalPlayersID()
    {
        return NetworkManager.Singleton.LocalClientId;
    }

    // ~~~~~~~~~ Player Living Stuffs ~~~~~~~~~
    // Server or Client
    public void RecordPlayerDeath(ulong id)
    {
        Debug.Log("<color=yellow>SERVER: </color> Record player death called by ID " + id);
        RecordPlayerDeathServerRpc(id);
    }

    // Server only
    [ServerRpc(RequireOwnership = false)]
    public void RecordPlayerDeathServerRpc(ulong id)
    {
        FindPlayerEntry(id).SetPlayerLiving(false);
        _netNumLivingPlayers.Value--;
        Debug.Log("<color=yellow>SERVER: </color> Player death " + id + ": " + FindPlayerEntry(id).PlayerName + " recorded");
    }

    // Server only
    public int GetNumLivingOnTeam(PlayerData.Team team)
    {
        if (!IsServer)
        {
            Debug.LogError("Server only function not called by server");
            return -1;
        }

        int numAlive = 0;

        foreach (PlayerEntry playa in _playerDict.Values)
        {
            if (playa.GetPlayerLiving() && playa.PlayerTeam == team)
                numAlive++;
        }
        Debug.Log("<color=yellow>SERVER: </color> Living members of team " + team.ToString() + " = " + numAlive);
        return numAlive;
    }

    // Get rid of when re-doing exile manager?
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
    #endregion
}
