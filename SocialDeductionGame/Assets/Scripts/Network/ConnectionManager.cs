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

public class ConnectionManager : NetworkBehaviour
{
    // ============== Singleton pattern ==============
    #region Singleton
    public static ConnectionManager Instance { get; private set; }
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        DontDestroyOnLoad(gameObject);
    }
    #endregion

    // ============== Refrences ==============
    [SerializeField] private GameObject _buttons;

    // ============== Variables ==============
    private UnityTransport _transport;

    public delegate void ConnectingAction();
    public static event ConnectingAction OnTryingToJoinGame;
    public static event ConnectingAction OnFailedToJoinGame;

    private void Awake()
    {
        InitializeSingleton();
    }

    // ============== Connections ==============
    public void CreateGame()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApproval;
        NetworkManager.Singleton.StartHost();

        LobbyManager.Instance.CreateLobbyData();
        //LobbyManager.Instance.SubscribeToLobbyEvents();
    }

    public void JoinGame()
    {
        OnTryingToJoinGame?.Invoke();

        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnect;
        NetworkManager.Singleton.StartClient();

        LobbyManager.Instance.CreateLobbyData();
        //LobbyManager.Instance.SubscribeToLobbyEvents();
    }

    private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        // NOTE: If connection approval is denied while the lobby is active the player's information will still be in the lobby
        // Here is doesnt matter as if the game has already started the lobby shouldn't exist anymore anyway

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

    private void ClientDisconnect(ulong playerID)
    {
        OnFailedToJoinGame?.Invoke();
    }

    public void Shutdown()
    {
        Debug.Log("COMMENING SHUTDOWN");

        NetworkManager.Singleton.Shutdown();
    }
}
