using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

// This code is by TaroDev, from this tutorial: https://www.youtube.com/watch?v=fdkvm21Y0xE

public class RelayTest : MonoBehaviour
{
    [SerializeField] private TMP_Text _joinCodeText;
    [SerializeField] private TMP_InputField _joinInput;
    [SerializeField] private GameObject _buttons;

    private UnityTransport _transport;
    private const int MaxPlayers = 5;

    public delegate void ConnectingAction();
    public static event ConnectingAction OnTryingToJoinGame;
    public static event ConnectingAction OnFailedToJoinGame;

    private async void Awake()
    {
        _transport = FindObjectOfType<UnityTransport>();

        _buttons.SetActive(false);

        await SignInCachedUserAsync();

        _buttons.SetActive(true);
    }

    private static async Task Authenticate()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    async static Task SignInCachedUserAsync()
    {
        await UnityServices.InitializeAsync();

        // Check if a cached player already exists by checking if the session token exists
        /*if (!AuthenticationService.Instance.SessionTokenExists)
        {
            Debug.Log("Cached Player re-join");
            return;
        }*/

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
    }

    public async void CreateGame()
    {
        //_buttons.SetActive(false);

        Allocation a = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
        _joinCodeText.text = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);

        _transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);

        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApproval;
        NetworkManager.Singleton.StartHost();

        SceneLoader.LoadNetwork(SceneLoader.Scene.CharacterSelectScene);
    }

    private void NetworkManager_ConnectionApproval(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        //Debug.Log("<color=purple>CONNECTION: </color> In NetworkManager_ConnectionApproval");

        if (!SceneLoader.IsInScene(SceneLoader.Scene.CharacterSelectScene))
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game already started";
            Debug.Log("<color=purple>CONNECTION: </color> Connection denied, Game already started");
            return;
        }

        connectionApprovalResponse.Approved = true;
    }

    public void CreateGameTest()
    {
        //_buttons.SetActive(false);

        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApproval;
        NetworkManager.Singleton.StartHost();

        SceneLoader.LoadNetwork(SceneLoader.Scene.CharacterSelectScene);
    }

    public void JoinGameTest()
    {
        //_buttons.SetActive(false);
        OnTryingToJoinGame?.Invoke();

        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_ClientDisconnect;
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_ClientDisconnect(ulong playerID)
    {
        OnFailedToJoinGame?.Invoke();
    }

    public async void JoinGame()
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
    }

    public void CopyCodeText()
    {
        TextEditor texteditor = new();
        texteditor.text = _joinCodeText.text;
        texteditor.SelectAll();
        texteditor.Copy();
    }
}
