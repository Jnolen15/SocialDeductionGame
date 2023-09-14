using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using TMPro;

public class LobbyEntryUI : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private TextMeshProUGUI _lobbyName;
    [SerializeField] private TextMeshProUGUI _lobbyNumPlayers;

    // ============== Vairables ==============
    private Lobby _thisLobby;

    // ============== Functions ==============
    public void Setup(Lobby lobby)
    {
        _thisLobby = lobby;

        _lobbyName.text = lobby.Name;
        _lobbyNumPlayers.text = lobby.Players.Count + "/8";
    }

    public void JoinLobby()
    {
        LobbyManager.Instance.JoinWithID(_thisLobby.Id);
    }
}
