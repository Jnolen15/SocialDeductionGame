using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using TMPro;

public class CharacterSelectUI : MonoBehaviour
{
    // ============== Refrences ==============
    [Header("UI Refrences")]
    [SerializeField] private Color _readyColor;
    [SerializeField] private Color _unreadyColor;
    [SerializeField] private Image _readyButtonSprite;
    [SerializeField] private TextMeshProUGUI _readyButtonText;
    [SerializeField] private TextMeshProUGUI _lobbyName;
    [SerializeField] private TextMeshProUGUI _lobbyCode;
    [SerializeField] private GameObject _gameSettingsButton;
    [SerializeField] private GameObject _gameSettingsMenu;
    [SerializeField] private GameObject _customizeMenu;
    [SerializeField] private GameObject _voiceSettingsMenu;
    [SerializeField] private GameObject _loadScreen;
    [SerializeField] private Transform _playerEntryZone;
    [SerializeField] private GameObject _playerLobbyEntryPref;

    [Header("Game Rules")]
    [SerializeField] private TextMeshProUGUI _numSabosText;
    [SerializeField] private TextMeshProUGUI _numDaysText;
    [SerializeField] private TextMeshProUGUI _timerLengthsText;
    private GameRules _gameRules = new();

    private bool _localPlayerReady;

    private float _updateLobbyTimer = 3f;

    // ============== Setup ==============
    #region Setup
    private void Awake()
    {
        //PlayerConnectionManager.OnPlayerConnect += UpdatePlayerCount;
        PlayerConnectionManager.OnPlayerReady += Ready;
        PlayerConnectionManager.OnPlayerUnready += Unready;
        PlayerConnectionManager.OnAllPlayersReadyAlertClients += ShowLoad;
        PlayerLobbyEntry.OnKickPlayer += KickPlayer;
    }

    private void Start()
    {
        SetupLobbyInfoPannel();

        UpdatePlayerEntries();

        if (!LobbyManager.Instance.IsLobbyHost())
        {
            _gameSettingsButton.SetActive(false);
            ToggleCustomize();
        }
        else
        {
            PlayerConnectionManager.Instance.SetGameSettings(_gameRules);
        }
    }

    private void OnDestroy()
    {
        //PlayerConnectionManager.OnPlayerConnect -= UpdatePlayerCount;
        PlayerConnectionManager.OnPlayerReady -= Ready;
        PlayerConnectionManager.OnPlayerUnready -= Unready;
        PlayerConnectionManager.OnAllPlayersReadyAlertClients -= ShowLoad;
        PlayerLobbyEntry.OnKickPlayer -= KickPlayer;
    }
    #endregion

    // ============== Update ==============
    #region Update
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
    #endregion

    // ============== UI Function ==============
    #region UI Function
    public void ToggleGameSettings()
    {
        _gameSettingsMenu.SetActive(true);
        _customizeMenu.SetActive(false);
        _voiceSettingsMenu.SetActive(false);
    }

    public void ToggleCustomize()
    {
        _gameSettingsMenu.SetActive(false);
        _customizeMenu.SetActive(true);
        _voiceSettingsMenu.SetActive(false);
    }

    public void ToggleVoiceSettings()
    {
        _gameSettingsMenu.SetActive(false);
        _customizeMenu.SetActive(false);
        _voiceSettingsMenu.SetActive(true);
    }

    private void Ready()
    {
        _readyButtonSprite.color = _readyColor;
        _readyButtonText.text = "Readied!";
        _localPlayerReady = true;
    }

    private void Unready()
    {
        _readyButtonSprite.color = _unreadyColor;
        _readyButtonText.text = "Ready?";
        _localPlayerReady = false;
    }

    private async void UpdatePlayerEntries()
    {
        Debug.Log("Updating player entries");

        List<Player> lobbyPlayers = await LobbyManager.Instance.GetLobbyPlayerListAsync();

        if (lobbyPlayers == null)
            return;

        foreach (Transform child in _playerEntryZone)
            Destroy(child.gameObject);

        foreach (Player player in lobbyPlayers)
        {
            PlayerLobbyEntry pEntry = Instantiate(_playerLobbyEntryPref, _playerEntryZone).GetComponent<PlayerLobbyEntry>();

            // If this is the host dont set a kick button
            if (player.Id == LobbyManager.Instance.GetHostID())
                pEntry.Setup(player, false);
            // I the player is a host, add kick button
            else
                pEntry.Setup(player, LobbyManager.Instance.IsLobbyHost());
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

    #endregion

    // ============== Other Functions ==============
    #region Other Functions
    public void ToggleReadyPlayer()
    {
        if (!_localPlayerReady)
            PlayerConnectionManager.Instance.ReadyPlayer();
        else
            PlayerConnectionManager.Instance.UnreadyPlayer();
    }

    private void KickPlayer(string playerID)
    {
        if(LobbyManager.Instance.IsLobbyHost())
            LobbyManager.Instance.KickPlayerFromLobby(playerID);
    }
    #endregion

    // ============== Game Settings ==============
    #region Game Settings
    private void UpdateGameSettings()
    {
        PlayerConnectionManager.Instance.SetGameSettings(_gameRules);
    }

    public void IncrementNumSabos()
    {
        _gameRules.NumSaboteurs = 2;
        _numSabosText.text = _gameRules.NumSaboteurs.ToString();

        UpdateGameSettings();
    }

    public void DecrementNumSabos()
    {
        _gameRules.NumSaboteurs = 1;
        _numSabosText.text = _gameRules.NumSaboteurs.ToString();

        UpdateGameSettings();
    }

    public void IncrementDaysToWin()
    {
        if (_gameRules.NumDaysToWin < 9)
            _gameRules.NumDaysToWin++;

        _numDaysText.text = _gameRules.NumDaysToWin.ToString();

        UpdateGameSettings();
    }

    public void DecrementDaysToWin()
    {
        if (_gameRules.NumDaysToWin > 7)
            _gameRules.NumDaysToWin--;

        _numDaysText.text = _gameRules.NumDaysToWin.ToString();

        UpdateGameSettings();
    }

    public void IncrementTimerLengths()
    {
        if (_gameRules.TimerLength != GameRules.TimerLengths.Longer)
            _gameRules.TimerLength++;

        _timerLengthsText.text = _gameRules.TimerLength.ToString();

        UpdateGameSettings();
    }

    public void DecrementTimerLengths()
    {
        if (_gameRules.TimerLength != GameRules.TimerLengths.Shorter)
            _gameRules.TimerLength--;

        _timerLengthsText.text = _gameRules.TimerLength.ToString();

        UpdateGameSettings();
    }
    #endregion
}
