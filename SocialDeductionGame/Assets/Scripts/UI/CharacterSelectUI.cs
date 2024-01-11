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
    [SerializeField] private Transform _playerEntryZone;
    [SerializeField] private GameObject _playerLobbyEntryPref;
    private bool _localPlayerReady;

    private float _updateLobbyTimer = 3f;

    // ============== Setup ==============
    private void Awake()
    {
        //PlayerConnectionManager.OnPlayerConnect += UpdatePlayerCount;
        PlayerConnectionManager.OnPlayerReady += Ready;
        PlayerConnectionManager.OnPlayerUnready += Unready;
        PlayerConnectionManager.OnAllPlayersReadyAlertClients += ShowLoad;
    }

    private void Start()
    {
        SetupLobbyInfoPannel();

        UpdatePlayerEntries();
    }

    private void OnDestroy()
    {
        //PlayerConnectionManager.OnPlayerConnect -= UpdatePlayerCount;
        PlayerConnectionManager.OnPlayerReady -= Ready;
        PlayerConnectionManager.OnPlayerUnready -= Unready;
        PlayerConnectionManager.OnAllPlayersReadyAlertClients -= ShowLoad;
    }

    // ============== Update ==============
    private void Update()
    {
        if (_updateLobbyTimer <= 0)
        {
            UpdatePlayerEntries();
            _updateLobbyTimer = 3f;
        }
        else
            _updateLobbyTimer -= Time.deltaTime;
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

    private async void UpdatePlayerEntries()
    {
        Debug.Log("Player count changed, updating entries");

        List<Player> lobbyPlayers = await LobbyManager.Instance.GetLobbyPlayerListAsync();

        foreach (Transform child in _playerEntryZone)
            Destroy(child.gameObject);

        foreach (Player player in lobbyPlayers)
        {
            PlayerLobbyEntry pEntry = Instantiate(_playerLobbyEntryPref, _playerEntryZone).GetComponent<PlayerLobbyEntry>();
            pEntry.Setup(player.Data[LobbyManager.KEY_PLAYER_NAME].Value);
        }
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
