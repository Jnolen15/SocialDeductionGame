using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Vivox;
using VivoxUnity;

public class LobbyManager : MonoBehaviour
{
    // Much of this code was written following Code Monkey's multiplayer game tutorial
    // https://www.youtube.com/watch?v=7glCsF9fv3s&t=13474s

    // ============== Singleton pattern ==============
    #region Singleton
    public static LobbyManager Instance { get; private set; }
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
    #region Variables
    private const string KEY_RELAY_JOIN_CODE = "KeyRelayJoinCode";
    private const string KEY_PLAYER_NAME = "KeyPlayerName";

    [SerializeField] private bool _localTestMode;
    private Lobby _joinedLobby;
    private float _hearthbeatTimer;
    private float _listRefreshTimer;
    private LobbyData _joinedLobbyData;

    public delegate void LobbyAction();
    public static event LobbyAction OnStartCreateLobby;
    public static event LobbyAction OnFailCreateLobby;
    public static event LobbyAction OnStartQuickJoin;
    public static event LobbyAction OnFailQuickJoin;
    public static event LobbyAction OnStartCodeJoin;
    public static event LobbyAction OnFailCodeJoin;
    public static event LobbyAction OnLoginComplete;
    public static event LobbyAction OnAlreadyLoggedIn;

    public delegate void LobbyListAction(List<Lobby> lobbyList);
    public static event LobbyListAction OnLobbyListChanged;

    public delegate void LobbyDataAction(LobbyData data);
    public static event LobbyDataAction OnLobbySendData;
    #endregion

    // ============== Setup =============
    #region Setup
    private void Awake()
    {
        _localTestMode = LogViewer.Instance.GetLocalTestMode();

        InitializeSingleton();
    }

    private void Start()
    {
        InitializeUnityAuthentication();
    }

    private async void InitializeUnityAuthentication()
    {
        if(UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new();

            if (_localTestMode)
            {
                Debug.Log("TEST MODE ENABLED: Random profile assignemnet");
                initializationOptions.SetProfile(Random.Range(0, 1000000).ToString());
            }

            await UnityServices.InitializeAsync(initializationOptions);

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            VivoxManager.Instance.VivoxLogin();

            OnLoginComplete?.Invoke();
        } else
        {
            OnAlreadyLoggedIn?.Invoke();
            Debug.Log("Unity Services already initialized!");
        }
    }
    #endregion

    // ============== Relay =============
    #region Relay
    private async Task<Allocation> AllocateRelay(int maxplayers)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxplayers);

            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);

            return default;
        }
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            return relayJoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);

            return default;
        }
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            return joinAllocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);

            return default;
        }
    }
    #endregion

    // ============== Update =============
    #region Update
    private void Update()
    {
        HandleHeartbeat();

        // Auto-Refresh lobby list
        if (SceneLoader.IsInScene(SceneLoader.Scene.LobbyScene))
        {
            if (_joinedLobby == null && AuthenticationService.Instance.IsSignedIn)
            {
                _listRefreshTimer -= Time.deltaTime;
                if (_listRefreshTimer <= 0f)
                {
                    Debug.Log("Refreshing Lobby List");

                    _listRefreshTimer = 3f;
                    ListLobbies();
                }
            }
        }
    }

    public void HandleHeartbeat()
    {
        if (IsLobbyHost())
        {
            _hearthbeatTimer -= Time.deltaTime;
            if(_hearthbeatTimer <= 0f)
            {
                _hearthbeatTimer = 15f;

                LobbyService.Instance.SendHeartbeatPingAsync(_joinedLobby.Id);
            }
        }
    }
    #endregion

    // ============== Lobby =============
    #region Lobby
    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            },
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            OnLobbyListChanged?.Invoke(queryResponse.Results);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }


    }

    public async void CreateLobby(LobbyData lobbyData)
    {
        OnStartCreateLobby?.Invoke();

        try
        {
            Allocation allocation = await AllocateRelay(lobbyData.MaxPlayers);
            string relayJoinCode = await GetRelayJoinCode(allocation);

            var options = new CreateLobbyOptions
            {
                IsPrivate = lobbyData.IsPrivate,
                Data = new Dictionary<string, DataObject> {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) },
                },
                Player = GetNewPlayer(),
            };

            _joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyData.Name, lobbyData.MaxPlayers, options);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));

            ConnectionManager.Instance.CreateGame();
            SceneLoader.LoadNetwork(SceneLoader.Scene.CharacterSelectScene);

            PrintPlayers(_joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            OnFailCreateLobby?.Invoke();
            Debug.LogError(e);
        }
    }

    public async void QuickJoin()
    {
        OnStartQuickJoin?.Invoke();

        try
        {
            var options = new QuickJoinLobbyOptions
            {
                Player = GetNewPlayer(),
            };

            _joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

            string relayJoinCode = _joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            ConnectionManager.Instance.JoinGame();

            PrintPlayers(_joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            OnFailQuickJoin();
            Debug.LogError(e);
        }
    }

    public async void JoinWithCode(string lobbyCode)
    {
        if (lobbyCode == "")
            return;

        OnStartCodeJoin?.Invoke();

        try
        {
            var options = new JoinLobbyByCodeOptions
            {
                Player = GetNewPlayer(),
            };

            _joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);

            string relayJoinCode = _joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            ConnectionManager.Instance.JoinGame();

            PrintPlayers(_joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            OnFailCodeJoin?.Invoke();
            Debug.LogError(e);
        }
    }

    public async void JoinWithID(string lobbyID)
    {
        if (lobbyID == "")
            return;

        OnStartCodeJoin?.Invoke();

        try
        {
            var options = new JoinLobbyByIdOptions
            {
                Player = GetNewPlayer(),
            };

            _joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID, options);

            string relayJoinCode = _joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            ConnectionManager.Instance.JoinGame();

            PrintPlayers(_joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            OnFailCodeJoin?.Invoke();
            Debug.LogError(e);
        }
    }

    public void DisconnectFromLobby()
    {
        if (IsLobbyHost())
            DeleteLobby();
        else
            LeaveLobby();
    }

    public async void DeleteLobby()
    {
        if (_joinedLobby == null)
            return;

        try
        {
            Debug.Log("<color=yellow>SERVER: </color>Deleting lobby");

            // Disconnect from vivox channel
            VivoxManager.Instance.LeaveAll();

            await LobbyService.Instance.DeleteLobbyAsync(_joinedLobby.Id);

            _joinedLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void LeaveLobby()
    {
        if (_joinedLobby == null)
            return;

        try
        {
            Debug.Log("<color=purple>CONNECTION: </color>leaving lobby");

            // Disconnect from vivox channel
            VivoxManager.Instance.LeaveAll();

            await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);

            _joinedLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void KickPlayerFromLobby(string playerID)
    {
        if (!IsLobbyHost())
            return;

        try
        {
            Debug.Log("<color=yellow>SERVER: </color>Kicking player: " + playerID);

            // Remove that player from lobby voice?

            await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, playerID);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    #endregion

    #region Lobby Events
    /*private async void SubscribeToLobbyEvents()
    {
        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += OnLobbyChanged;
        //callbacks.KickedFromLobby += OnKickedFromLobby;
        //callbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
        try
        {
            _lobbyEvents = await Lobbies.Instance.SubscribeToLobbyEventsAsync(_joinedLobby.Id, callbacks);
        }
        catch (LobbyServiceException ex)
        {
            switch (ex.Reason)
            {
                case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{_joinedLobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                default: throw;
            }
        }
    }

    private void OnLobbyChanged(ILobbyChanges changes)
    {
        if (changes.LobbyDeleted)
        {
            // Handle lobby being deleted
            // Calling changes.ApplyToLobby will log a warning and do nothing
        }
        else
        {
            changes.ApplyToLobby(_joinedLobby);
        }
        // Refresh the UI in some way
    }*/
    #endregion

    // ============== Vivox =============
    #region Vivox
    // Join in game positional and lobby channels
    public void JoinLobbyVivoxChannel()
    {
        // Positional first (documention says always positional first)
        VivoxManager.Instance.JoinWorldChannel(_joinedLobby.Id);

        // Lobby channel
        VivoxManager.Instance.JoinLobbyChannel(_joinedLobby.Id);
        
        // Death channel
        //VivoxManager.Instance.JoinDeathChannel(_joinedLobby.Id);
    }
    #endregion

    // ============== Helpers =============
    #region Helpers
    public bool IsLobbyHost()
    {
        return (_joinedLobby != null && _joinedLobby.HostId == AuthenticationService.Instance.PlayerId);
    }

    public Lobby GetLobby()
    {
        return _joinedLobby;
    }

    public void CreateLobbyData()
    {
        if (_joinedLobby == null)
        {
            Debug.LogError("_joinedLobby is null, cant send data!");
            return;
        }

        Debug.Log("Creating Lobby Data");

        _joinedLobbyData = new LobbyData
        {
            Name = _joinedLobby.Name,
            IsPrivate = _joinedLobby.IsPrivate,
            MaxPlayers = _joinedLobby.MaxPlayers,
        };

        SendLobbyData();
    }

    public void SendLobbyData()
    {
        OnLobbySendData?.Invoke(_joinedLobbyData);
    }

    private Player GetNewPlayer()
    {
        string playerName = PlayerPrefs.GetString(PlayerNamer.KEY_PLAYERNAME);

        if (LogViewer.Instance.GetRandomNames())
            playerName = "Gamer " + Random.Range(0, 1000);

        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject> {
                { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName )},
            },
        };
    }

    private void PrintPlayers(Lobby printLobby)
    {
        foreach(Player playa in printLobby.Players)
        {
            Debug.Log($"ID: {playa.Id}, Name: {playa.Data[KEY_PLAYER_NAME].Value}");
        }
    }
    #endregion
}
