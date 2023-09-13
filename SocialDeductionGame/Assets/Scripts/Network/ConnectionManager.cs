using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

// Code written following Code Monkey: https://www.youtube.com/watch?v=7glCsF9fv3s&t=13474s

public class ConnectionManager : MonoBehaviour
{
    // ============== Singleton pattern ==============
    #region Singleton
    public static ConnectionManager Instance { get; private set; }
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
    [SerializeField] private GameObject _buttons;

    // ============== Variables ==============
    private UnityTransport _transport;
    private const int MaxPlayers = 5;

    public delegate void ConnectingAction();
    public static event ConnectingAction OnTryingToJoinGame;
    public static event ConnectingAction OnFailedToJoinGame;

    private void Awake()
    {
        InitializeSingleton();

        /*_transport = FindObjectOfType<UnityTransport>();

        _buttons.SetActive(false);

        await SignInCachedUserAsync();

        _buttons.SetActive(true);*/
    }

    // ============== Connections ==============
    public void CreateGameTest()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApproval;
        NetworkManager.Singleton.StartHost();
    }

    public void JoinGameTest()
    {
        OnTryingToJoinGame?.Invoke();

        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_ClientDisconnect;
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_ConnectionApproval(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        // If not in character select scene deny connection
        if (!SceneLoader.IsInScene(SceneLoader.Scene.CharacterSelectScene))
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game already started";
            Debug.Log("<color=purple>CONNECTION: </color> Connection denied, Game already started");
            return;
        }

        // Approve connection if nothing above has fired
        connectionApprovalResponse.Approved = true;
    }

    private void NetworkManager_ClientDisconnect(ulong playerID)
    {
        OnFailedToJoinGame?.Invoke();
    }


    // ============== OLD ==============
    /*private static async Task Authenticate()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }*/

    /*async static Task SignInCachedUserAsync()
    {
        await UnityServices.InitializeAsync();

        // Check if a cached player already exists by checking if the session token exists
        if (!AuthenticationService.Instance.SessionTokenExists)
        {
            Debug.Log("Cached Player re-join");
            return;
        }

        // Sign in Anonymously
        // This call will sign in the cached player.
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");

            // Shows how to get the playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }*/

    /*public async void CreateGame()
    {
        //_buttons.SetActive(false);

        Allocation a = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
        _joinCodeText.text = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);

        _transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);

        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApproval;
        NetworkManager.Singleton.StartHost();

        SceneLoader.LoadNetwork(SceneLoader.Scene.CharacterSelectScene);
    }*/



    /*public async void JoinGame()
    {
        //_buttons.SetActive(false);

        try
        {
            JoinAllocation a = await RelayService.Instance.JoinAllocationAsync(_joinInput.text);

            _transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);

            NetworkManager.Singleton.StartClient();
        }
        catch
        {
            //_buttons.SetActive(true);
            Debug.LogError("Room with provided join code not found!");
            return;
        }
    }*/
}
