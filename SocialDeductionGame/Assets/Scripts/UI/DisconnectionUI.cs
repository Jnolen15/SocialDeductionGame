using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DisconnectionUI : MonoBehaviour
{
    // =================== Refrences ===================
    [SerializeField] private GameObject _hostDisconnectPanel;
    [SerializeField] private GameObject _playerDisconnectPanel;

    // =================== Setup ===================
    private void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnectCallback;
    }

    private void OnDisable()
    {
        if(NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnectCallback;
        }
    }

    // =================== UI Functions ===================
    private void OnTransportFailCallback()
    {
        ShowPlayerDisconnect();
    }

    private void OnDisconnectCallback(ulong clientID)
    {
        // Server host disconnected
        if (clientID == NetworkManager.ServerClientId)
            ShowHostDisconnect();
    }

    public void ReturnToMainMenu()
    {
        // Note, not entirely sure if this is right.
        // The lobby stops existing when entered a game so only try to disconnect from it in character select scene

        VivoxManager.Instance.LeaveAll();

        if (SceneLoader.IsInScene(SceneLoader.Scene.CharacterSelectScene))
        {
            LobbyManager.Instance.DisconnectFromLobby();
        }
        else
        {
            ConnectionManager.Instance.Shutdown();
        }

        SceneLoader.Load(SceneLoader.Scene.MainMenu);
    }

    private void ShowPlayerDisconnect()
    {
        _playerDisconnectPanel.SetActive(true);
    }

    private void ShowHostDisconnect()
    {
        _hostDisconnectPanel.SetActive(true);
    }

    private void HideHostDisconnect()
    {
        _hostDisconnectPanel.SetActive(false);
    }
}
