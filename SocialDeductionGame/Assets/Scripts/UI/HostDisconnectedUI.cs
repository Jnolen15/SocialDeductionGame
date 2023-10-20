using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HostDisconnectedUI : MonoBehaviour
{
    // =================== Refrences ===================
    [SerializeField] private GameObject _panel;

    // =================== Setup ===================
    private void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnectCallback;
    }

    private void OnDisable()
    {
        if(NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnectCallback;
    }

    // =================== UI Functions ===================
    private void OnDisconnectCallback(ulong clientID)
    {
        // Server host disconnected
        if (clientID == NetworkManager.ServerClientId)
            Show();
    }

    public void ReturnToMainMenu()
    {
        // Note, not entirely sure if this is right.
        // The lobby stops existing when entered a game so only try to disconnect from it in character select scene

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

    private void Show()
    {
        _panel.SetActive(true);
    }

    private void Hide()
    {
        _panel.SetActive(false);
    }
}
