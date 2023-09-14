using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private TMP_InputField _joinCode;
    [SerializeField] private Transform _lobbyContainer;
    [SerializeField] private GameObject _lobbytemplate;

    // ============== Setup ==============
    private void Start()
    {
        LobbyManager.OnLobbyListChanged += UpdateLobbyList;
    }

    private void OnDestroy()
    {
        LobbyManager.OnLobbyListChanged -= UpdateLobbyList;
    }

    // ============== Functions ==============
    public void QuickJoin()
    {
        LobbyManager.Instance.QuickJoin();
    }

    public void JoinWithCode()
    {
        LobbyManager.Instance.JoinWithCode(_joinCode.text);
    }

    public void LeaveLobby()
    {
        if(LobbyManager.Instance.IsLobbyHost())
            LobbyManager.Instance.DeleteLobby();
        else
            LobbyManager.Instance.LeaveLobby();
    }

    private void UpdateLobbyList(List<Lobby> lobbyList)
    {
        foreach(Transform child in _lobbyContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbyList)
        {
            GameObject lobbyEntry = Instantiate(_lobbytemplate, _lobbyContainer);
            lobbyEntry.GetComponent<LobbyEntryUI>().Setup(lobby);
        }
    }

    private void OnApplicationQuit()
    {
        LeaveLobby();
    }
}
