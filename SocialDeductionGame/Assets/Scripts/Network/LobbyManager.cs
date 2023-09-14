using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

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

    // ============== Refrences ==============

    // ============== Variables ==============
    [SerializeField] private bool _enableTestMode;
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

    // ============== Setup =============
    #region Setup
    private void Awake()
    {
        InitializeSingleton();

        InitializeUnityAuthentication();
    }

    private async void InitializeUnityAuthentication()
    {
        if(UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new();

            if (_enableTestMode)
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

    // ============== Lobby =============
    private void Update()
    {
        HandleHeartbeat();

        // Auto-Refresh lobby list
        if(_joinedLobby == null && AuthenticationService.Instance.IsSignedIn)
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

    public bool IsLobbyHost()
    {
        return (_joinedLobby != null && _joinedLobby.HostId == AuthenticationService.Instance.PlayerId);
    }

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

            ConnectionManager.Instance.CreateGameTest();
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

            ConnectionManager.Instance.JoinGameTest();
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

            ConnectionManager.Instance.JoinGameTest();
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

            ConnectionManager.Instance.JoinGameTest();
        }
        catch (LobbyServiceException e)
        {
            OnFailCodeJoin?.Invoke();
            Debug.LogError(e);
        }
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

    public Lobby GetLobby()
    {
        return _joinedLobby;
    }
}
