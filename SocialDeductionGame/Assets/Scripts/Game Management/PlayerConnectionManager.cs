using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using System.Linq;
using Unity.Services.Lobbies.Models;

public class PlayerConnectionManager : NetworkBehaviour
{
    // ============== Singleton pattern ==============
    #region Singleton
    public static PlayerConnectionManager Instance { get; private set; }
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        DontDestroyOnLoad(gameObject);
    }
    #endregion

    // ============== Variables ==============
    #region Variables and Refrences
    // Game rules
    private GameRules _gameRules;
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

        public void SetPlayerTeam(PlayerData.Team team)
        {
            PlayerTeam = team;
            PlayerObject.GetComponent<PlayerData>().SetTeam(team);
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

        public PlayerData.Team GetPlayerTeam()
        {
            return PlayerTeam;
        }

        public override string ToString()
        {
            string outStr = PlayerName;
            outStr += ", On team " + PlayerTeam.ToString();
            outStr += ", Is living " + GetPlayerLiving();
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

    public delegate void PlayerConnectionAction();
    public static event PlayerConnectionAction OnPlayerConnect;
    public static event PlayerConnectionAction OnPlayerDisconnect;

    public delegate void PlayerReadyAction();
    public static event PlayerReadyAction OnPlayerReady;
    public static event PlayerReadyAction OnPlayerUnready;
    public static event PlayerReadyAction OnAllPlayersReady;
    public static event PlayerReadyAction OnAllPlayersReadyAlertClients;
    public static event PlayerReadyAction OnPlayerSetupComplete;

    public delegate void PlayerChangeAction();
    public static event PlayerReadyAction OnPlayerDied;
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
            Debug.Log("<color=yellow>SERVER: </color> Subscribing to callbacks");

            NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SpawnPlayerPrefabs;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            Debug.Log("<color=yellow>SERVER: </color> Unsubscribing from callbacks");

            NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SpawnPlayerPrefabs;
        }
        base.OnNetworkDespawn();
    }
    #endregion

    // ============== Client Connection ==============
    #region Client Connection
    private void ClientConnected(ulong clientID)
    {
        Debug.Log($"<color=yellow>SERVER: </color> Client {clientID} connected");
        // Update server side dictionary
        _netNumPlayers.Value++;
        PlayerEntry newPlayer = new PlayerEntry("Player " + clientID);
        _playerDict.Add(clientID, newPlayer);

        // sync newly joined clients player dictionary
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { clientID };
        InitialPlayerDictionarySycnClientRpc(_playerDict.Keys.ToArray(), _playerDict.Values.ToArray(), clientRpcParams);

        // Update client dictionaries
        AddPlayerToDictionaryClientRpc(clientID, newPlayer);

        PlayerConnectedClientRpc();
    }

    private void ClientDisconnected(ulong clientID)
    {
        // When someone leaves count them as dead
        if(SceneLoader.IsInScene(SceneLoader.Scene.IslandGameScene))
            RecordPlayerDeathServerRpc(clientID);

        /*// Test for ready again (In case all but player who left were ready)
        // If player who left is ready, unready them, otherwise they will be counted when they should not
        if (GetPlayerReadyByID(clientID))
            UnreadyPlayerByID(clientID);
        TestAllPlayersReady();*/

        Debug.Log($"<color=yellow>SERVER: </color> Client {clientID} disconnected");
        _netNumPlayers.Value--;

        // Update server and client dictionaries
        _playerDict.Remove(clientID);
        RemovePlayerFromDictionaryClientRpc(clientID);

        PlayerDisconnectedClientRpc();
    }

    [ClientRpc]
    private void PlayerConnectedClientRpc()
    {
        OnPlayerConnect?.Invoke();
    }

    [ClientRpc]
    private void PlayerDisconnectedClientRpc()
    {
        OnPlayerDisconnect?.Invoke();
    }
    #endregion

    // ============== Player / Game Setup ==============
    #region Player / Game Setup
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
            Debug.Log("<color=yellow>SERVER: </color> Spawning prefab for client " + clientID, gameObject);
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

        //Assign player teams
        AssignRoles();

        // Finish Setup Event
        OnPlayerSetupComplete?.Invoke();
    }

    public void SetGameSettings(GameRules gameRules)
    {
        if (!IsServer)
            return;

        Debug.Log("<color=yellow>SERVER: </color>Updating game settings");
        _gameRules = gameRules;
    }

    public GameRules GetGameRules()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only the server has access to game rules!");
            return null;
        }

        return _gameRules;
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
        UpdatePlayerNameClientRpc(id, pName);
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

        TestAllPlayersReady();
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

    public void UnreadyAllPlayers()
    {
        if (!IsServer) return;

        UnreadyAllPlayersServerRpc();
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

    private void TestAllPlayersReady()
    {
        if (!IsServer) return;

        // Test in game
        if (SceneLoader.IsInScene(SceneLoader.Scene.IslandGameScene))
        {
            // If in game scene check against number of living players
            if (_netPlayersReadied.Value >= GetNumLivingPlayers())
                AllPlayersReady();
        }
        // Test out of game, AKA character select
        else
        {
            // Make sure at least 4 players connected
            if(GetNumConnectedPlayers() < 4 && !LogViewer.Instance.GetStartWithAny())
            {
                Debug.Log("<color=yellow>SERVER: </color>Can't start game, Not enough players");
                return;
            }

            // Otherwise check against total connected players
            if (_netPlayersReadied.Value >= GetNumConnectedPlayers())
            {
                AllPlayersReady();
                AlertClientsAllPlayersReadyClientRpc();
            }
        }
    }

    private void AllPlayersReady()
    {
        if (!IsServer) return;

        Debug.Log("<color=yellow>SERVER: </color> All players ready");

        // Unready all players
        UnreadyAllPlayersServerRpc();

        // Send event
        OnAllPlayersReady?.Invoke();
    }

    [ClientRpc]
    private void AlertClientsAllPlayersReadyClientRpc()
    {
        OnAllPlayersReadyAlertClients?.Invoke();
    }

    public int GetNumReadyPlayers()
    {
        return _netPlayersReadied.Value;
    }

    private bool GetPlayerReadyByID(ulong clientID)
    {
        return _playerReadyDictionary[clientID];
    }

    private void UnreadyPlayerByID(ulong clientID)
    {
        if (!IsServer)
            return;

        _netPlayersReadied.Value--;
        _playerReadyDictionary[clientID] = false;
    }
    #endregion

    // ============== Roles ==============
    #region Roles
    private void AssignRoles()
    {
        if (!IsServer)
            return;

        if (_gameRules.NumSaboteurs != 1 && _gameRules.NumSaboteurs != 2)
        {
            _gameRules.NumSaboteurs = 1;
        }

        Debug.Log("<color=yellow>SERVER: </color>Assigning player roles. Sabos: " + _gameRules.NumSaboteurs);

        // Pick X random players to be saboteurs
        List<ulong> playerIDs = new(_playerDict.Keys);
        List<ulong> saboPlayers = new();
        for(int i = 0; i < _gameRules.NumSaboteurs; i++)
        {
            ulong rand = 99;

            if (playerIDs.Count > 0)
                rand = playerIDs[(int)Random.Range(0, playerIDs.Count)];

            if (rand == 99)
            {
                Debug.Log("<color=yellow>SERVER: </color>Less players than number of sabos");
                break;
            }

            Debug.Log($"<color=yellow>SERVER: </color>Player {rand} will be on team Saboteurs");
            saboPlayers.Add(rand);
            playerIDs.Remove(rand);
        }

        // Assign roles
        foreach (ulong playerID in _playerDict.Keys)
        {
            if (saboPlayers.Contains(playerID))
            {
                Debug.Log($"<color=yellow>SERVER: </color>Assinging player {playerID} to team Saboteurs");
                _playerDict[playerID].SetPlayerTeam(PlayerData.Team.Saboteurs);

                // Update that client
                ClientRpcParams clientRpcParams = default;
                clientRpcParams.Send.TargetClientIds = new ulong[] { playerID };
                UpdatePlayerTeamClientRpc(playerID, PlayerData.Team.Saboteurs, clientRpcParams);
            }
            else
            {
                Debug.Log($"<color=yellow>SERVER: </color>Assinging player {playerID} to team Survivors");
                _playerDict[playerID].SetPlayerTeam(PlayerData.Team.Survivors);

                // Update that client
                ClientRpcParams clientRpcParams = default;
                clientRpcParams.Send.TargetClientIds = new ulong[] { playerID };
                UpdatePlayerTeamClientRpc(playerID, PlayerData.Team.Survivors, clientRpcParams);
            }
        }
    }
    #endregion

    // ============== Player Dictionary Client Sync ==============
    #region PlayerDictionary Client Sync
    /* NOTICE
     * Since I sync all PlayerEntries, all clients now have access to all that info
     * So for example clients could access what team a player is on and also (untested) their game object
     * In the future, may want to change it so that the client does not reiceive that info
     * As a cheat could be made to instantly see player teams
     * 
     * Acctualy no teams are never updated, and they start unassigned. So other players should see all teams unassinged
     * Escept for their own team which should be set
    */

    [ClientRpc]
    private void InitialPlayerDictionarySycnClientRpc(ulong[] playerIDs, PlayerEntry[] playerEntries, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
            return;

        Debug.Log("<color=blue>CLIENT: </color> Performing initial client dictionary sync");

        for (int i = 0; i < playerIDs.Length; i++)
        {
            _playerDict.Add(playerIDs[i], playerEntries[i]);

            Debug.Log($"<color=blue>CLIENT: </color> Client {playerIDs[i]} added to client dictionary");
        }
    }

    [ClientRpc]
    private void AddPlayerToDictionaryClientRpc(ulong playerID, PlayerEntry playerEntry)
    {
        if (IsServer)
            return;

        if (_playerDict.ContainsKey(playerID))
            return;

        _playerDict.Add(playerID, playerEntry);

        Debug.Log($"<color=blue>CLIENT: </color> Client {playerID} added to client dictionary");
    }

    [ClientRpc]
    private void RemovePlayerFromDictionaryClientRpc(ulong playerID)
    {
        if (IsServer)
            return;

        _playerDict.Remove(playerID);

        Debug.Log($"<color=blue>CLIENT: </color> Client {playerID} removed from client dictionary");
    }

    [ClientRpc]
    private void UpdatePlayerNameClientRpc(ulong playerID, string playerName)
    {
        if (IsServer)
            return;

        PlayerEntry entry = FindPlayerEntry(playerID);
        entry.SetName(playerName);

        Debug.Log($"<color=blue>CLIENT: </color> Updated player {playerID}'s name to {playerName}");
    }

    [ClientRpc]
    private void UpdatePlayerLivingClientRpc(ulong playerID, bool playerLiving)
    {
        if (IsServer)
            return;

        PlayerEntry entry = FindPlayerEntry(playerID);
        entry.SetPlayerLiving(playerLiving);

        Debug.Log($"<color=blue>CLIENT: </color> Updated player {playerID}'s living status to {playerLiving}");
    }

    [ClientRpc]
    private void UpdatePlayerTeamClientRpc(ulong playerID, PlayerData.Team playerTeam, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
            return;

        PlayerEntry entry = FindPlayerEntry(playerID);
        entry.PlayerTeam = playerTeam;

        Debug.Log($"<color=blue>CLIENT: </color> Updated player {playerID}'s team status to {playerTeam}");
    }
    #endregion

    // ============== Helpers ==============
    #region Helpers
    // ~~~~~~~~~ Return Data From Player Dictionary ~~~~~~~~~
    // Server (all info) or Client (Id, name, living status)
    public PlayerEntry FindPlayerEntry(ulong id)
    {
        if (_playerDict.TryGetValue(id, out PlayerEntry entry))
            return entry;

        Debug.LogError("<color=yellow>SERVER: </color>Unable to find player with ID: " + id);
        return null;
    }

    // Server or Client
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

    // Server only
    public List<ulong> GetPlayerIDs()
    {
        if (!IsServer)
        {
            Debug.LogError("Server only function not called by server");
            return null;
        }

        return _playerDict.Keys.ToList<ulong>();
    }

    // Server only
    public List<ulong> GetLivingPlayerIDs()
    {
        if (!IsServer)
        {
            Debug.LogError("Server only function not called by server");
            return null;
        }

        List<ulong> playerIDs = new();

        foreach (ulong id in _playerDict.Keys)
        {
            if (FindPlayerEntry(id).GetPlayerLiving())
                playerIDs.Add(id);
        }

        return playerIDs;
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


    // ~~~~~~~~~ Get Local Player Info ~~~~~~~~~
    // Server or Client
    public ulong GetLocalPlayersID()
    {
        return NetworkManager.Singleton.LocalClientId;
    }

    public bool GetLocalPlayerLiving()
    {
        return FindPlayerEntry(NetworkManager.Singleton.LocalClientId).GetPlayerLiving();
    }

    public PlayerData.Team GetLocalPlayerTeam()
    {
        return FindPlayerEntry(NetworkManager.Singleton.LocalClientId).GetPlayerTeam();
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
        OnPlayerDied?.Invoke();
        Debug.Log("<color=yellow>SERVER: </color> Player death " + id + ": " + FindPlayerEntry(id).PlayerName + " recorded");
        UpdatePlayerLivingClientRpc(id, false);
    }

    // Server or Client
    public bool GetPlayerLivingByID(ulong id)
    {
        if (FindPlayerEntry(id) != null)
            return FindPlayerEntry(id).GetPlayerLiving();
        else return false;
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
            if (playa.GetPlayerLiving() && playa.GetPlayerTeam() == team)
                numAlive++;
        }
        Debug.Log("<color=yellow>SERVER: </color> Living members of team " + team.ToString() + " = " + numAlive);
        return numAlive;
    }


    // ~~~~~~~~~ Team Stuffs ~~~~~~~~~
    // Server only
    public int GetNumSaboteurs()
    {
        if (!IsServer)
        {
            Debug.LogError("Server only function not called by server");
            return -1;
        }

        return _gameRules.NumSaboteurs;
    }
    #endregion
}
