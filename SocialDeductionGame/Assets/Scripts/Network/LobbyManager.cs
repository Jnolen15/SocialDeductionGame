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
            Destroy(this);
        else
            Instance = this;

        DontDestroyOnLoad(gameObject);
    }
    #endregion

    // ============== Variables ==============
    #region Variables
    private const string KEY_RELAY_JOIN_CODE = "KeyRelayJoinCode";

    [SerializeField] private bool _localTestMode;
    private Lobby _joinedLobby;
    private float _hearthbeatTimer;
    private float _listRefreshTimer;

    public delegate void LobbyAction();
    public static event LobbyAction OnStartCreateLobby;
    public static event LobbyAction OnFailCreateLobby;
    public static event LobbyAction OnStartQuickJoin;
    public static event LobbyAction OnFailQuickJoin;
    public static event LobbyAction OnStartCodeJoin;
    public static event LobbyAction OnFailCodeJoin;

    public delegate void LobbyListAction(List<Lobby> lobbyList);
    public static event LobbyListAction OnLobbyListChanged;
    #endregion

    // ============== Setup =============
    #region Setup
    private void Awake()
    {
        _localTestMode = LogViewer.Instance.GetLocalTestMode();

        InitializeSingleton();

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
        } else
        {
            Debug.Log("Unity Services already initialized!");
        }
    }
    #endregion

    // ============== Relay =============
    #region Relay
    private async Task<Allocation> AllocateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(_joinedLobby.MaxPlayers - 1);

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
        if (!SceneLoader.IsInScene(SceneLoader.Scene.IslandGameScene))
        {
            if (_joinedLobby == null && AuthenticationService.Instance.IsSignedIn)
            {
                _listRefreshTimer -= Time.deltaTime;
                if (_listRefreshTimer <= 0f)
                {
                    Debug.Log("Refreshing Lobby List");

                    _listRefreshTimer = 5f;
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

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        OnStartCreateLobby?.Invoke();

        try
        {
            _joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 8, new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
            });

            Allocation allocation = await AllocateRelay();
            string relayJoinCode = await GetRelayJoinCode(allocation);

            await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            });

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));

            ConnectionManager.Instance.CreateGame();
            SceneLoader.LoadNetwork(SceneLoader.Scene.CharacterSelectScene);
        }
        catch (LobbyServiceException e)
        {
            OnFailCreateLobby();
            Debug.LogError(e);
        }
    }

    public async void QuickJoin()
    {
        OnStartQuickJoin?.Invoke();

        try
        {
            _joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            string relayJoinCode = _joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            ConnectionManager.Instance.JoinGame();
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
            _joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            string relayJoinCode = _joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            ConnectionManager.Instance.JoinGame();
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
            _joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID);

            string relayJoinCode = _joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            ConnectionManager.Instance.JoinGame();
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

            await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, playerID);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
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
    #endregion
}
