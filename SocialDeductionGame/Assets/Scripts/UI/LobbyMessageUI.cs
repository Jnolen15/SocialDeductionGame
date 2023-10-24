using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class LobbyMessageUI : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private TextMeshProUGUI _reasonText;

    // ============== Setup ==============
    void Start()
    {
        ConnectionManager.OnTryingToJoinGame += TryingToJoinGameMessage;
        ConnectionManager.OnFailedToJoinGame += FailJoinGameMessage;
        LobbyManager.OnStartCreateLobby += StartCreateLobbyMessage;
        LobbyManager.OnFailCreateLobby += FailCreateLobbyMessage;
        LobbyManager.OnStartQuickJoin += StartQuickJoinMessage;
        LobbyManager.OnFailQuickJoin += FailQuickJoinMessage;
        LobbyManager.OnStartCodeJoin += StartCodeJoinMessage;
        LobbyManager.OnFailCodeJoin += FailCodeJoinMessage;
        VivoxManager.OnLoginFailure += VivoxLoginfail;
        Hide();
    }

    private void OnDestroy()
    {
        ConnectionManager.OnTryingToJoinGame -= TryingToJoinGameMessage;
        ConnectionManager.OnFailedToJoinGame -= FailJoinGameMessage;
        LobbyManager.OnStartCreateLobby -= StartCreateLobbyMessage;
        LobbyManager.OnFailCreateLobby -= FailCreateLobbyMessage;
        LobbyManager.OnStartQuickJoin -= StartQuickJoinMessage;
        LobbyManager.OnFailQuickJoin -= FailQuickJoinMessage;
        LobbyManager.OnStartCodeJoin -= StartCodeJoinMessage;
        LobbyManager.OnFailCodeJoin -= FailCodeJoinMessage;
        VivoxManager.OnLoginFailure -= VivoxLoginfail;
    }

    // ============== UI Functions ==============
    private void DisplayMessage(string message)
    {
        _reasonText.text = message;
        Show();
    }

    private void TryingToJoinGameMessage()
    {
        DisplayMessage("Connecting...");
    }

    private void FailJoinGameMessage()
    {
        _reasonText.text = NetworkManager.Singleton.DisconnectReason;

        if (NetworkManager.Singleton.DisconnectReason == "")
            DisplayMessage("Failed to connect");
        else
            DisplayMessage(NetworkManager.Singleton.DisconnectReason);
    }

    public void StartCreateLobbyMessage()
    {
        DisplayMessage("Creating lobby...");
    }

    public void FailCreateLobbyMessage()
    {
        DisplayMessage("Failed to create lobby!");
    }

    public void StartQuickJoinMessage()
    {
        DisplayMessage("Finding a lobby...");
    }

    public void FailQuickJoinMessage()
    {
        DisplayMessage("Failed to find a lobby!");
    }

    public void StartCodeJoinMessage()
    {
        DisplayMessage("Joining lobby...");
    }

    public void FailCodeJoinMessage()
    {
        DisplayMessage("Failed to join lobby!");
    }

    public void VivoxLoginfail()
    {
        DisplayMessage("Vivox login failure! Voice funtion will not work, restart game.");
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
