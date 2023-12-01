using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using TMPro;

public class CharacterSelectUI : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private Image _readyButtonSprite;
    [SerializeField] private TextMeshProUGUI _lobbyName;
    [SerializeField] private TextMeshProUGUI _lobbyCode;
    [SerializeField] private TextMeshProUGUI _joinedPlayers;
    [SerializeField] private GameObject _loadScreen;
    private bool _localPlayerReady;

    // ============== Setup ==============
    private void Awake()
    {
        PlayerConnectionManager.OnPlayerConnect += UpdatePlayerCount;
        PlayerConnectionManager.OnPlayerReady += Ready;
        PlayerConnectionManager.OnPlayerUnready += Unready;
        PlayerConnectionManager.OnAllPlayersReadyAlertClients += ShowLoad;
    }

    private void Start()
    {
        SetupLobbyInfoPannel();
    }

    private void OnDestroy()
    {
        PlayerConnectionManager.OnPlayerConnect -= UpdatePlayerCount;
        PlayerConnectionManager.OnPlayerReady -= Ready;
        PlayerConnectionManager.OnPlayerUnready -= Unready;
        PlayerConnectionManager.OnAllPlayersReadyAlertClients -= ShowLoad;
    }

    // ============== Functions ==============
    public void ReturnToMainMenu()
    {
        LobbyManager.Instance.DisconnectFromLobby();
        VivoxManager.Instance.LeaveAll();

        SceneLoader.Load(SceneLoader.Scene.MainMenu);
    }

    public void ToggleReadyPlayer()
    {
        if(!_localPlayerReady)
            PlayerConnectionManager.Instance.ReadyPlayer();
        else
            PlayerConnectionManager.Instance.UnreadyPlayer();
    }

    private void Ready()
    {
        _readyButtonSprite.color = Color.green;
        _localPlayerReady = true;
    }

    private void Unready()
    {
        _readyButtonSprite.color = Color.red;
        _localPlayerReady = false;
    }

    private void UpdatePlayerCount(ulong id)
    {
        Debug.Log("PLAYER CONNECTED UPDATING COUNT");
        int numPlayers = PlayerConnectionManager.Instance.GetNumConnectedPlayers();
        _joinedPlayers.text = "Connected Players: " + numPlayers;
    }

    private void SetupLobbyInfoPannel()
    {
        Lobby joinedLobby = LobbyManager.Instance.GetLobby();

        _lobbyName.text = joinedLobby.Name;
        _lobbyCode.text = joinedLobby.LobbyCode;
    }

    public void CopyCodeText()
    {
        TextEditor texteditor = new();
        texteditor.text = _lobbyCode.text;
        texteditor.SelectAll();
        texteditor.Copy();
    }

    private void ShowLoad()
    {
        _loadScreen.SetActive(true);
    }
}
