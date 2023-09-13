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
    private Lobby joinedLobby;

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
    // Delete later if not needed
    public void TestCreateLobby()
    {
        CreateLobby("Lobby name", false);
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 8, new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
            });

            ConnectionManager.Instance.CreateGameTest();
            SceneLoader.LoadNetwork(SceneLoader.Scene.CharacterSelectScene);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void QuickJoin()
    {
        try
        {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            ConnectionManager.Instance.JoinGameTest();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void JoinWithCode(string lobbyCode)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            ConnectionManager.Instance.JoinGameTest();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public Lobby GetLobby()
    {
        return joinedLobby;
    }
}
