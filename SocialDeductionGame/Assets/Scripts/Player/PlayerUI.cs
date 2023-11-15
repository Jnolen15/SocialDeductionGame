using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PlayerUI : MonoBehaviour
{
    // ================== Variables / Refrences ==================
    #region Variables / Refrences
    private PlayerData _playerData;
    private PlayerHealth _playerHealth;

    [Header("Other")]
    [SerializeField] private GameObject _readyButton;
    [SerializeField] private GameObject _readyButtonIcon;
    [SerializeField] private Sprite _readyNormal;
    [SerializeField] private Sprite _readySpeedUp;
    [SerializeField] private GameObject _locationMenu;
    [SerializeField] private GameObject _craftingMenu;
    [SerializeField] private GameObject _introRole;
    [SerializeField] private TextMeshProUGUI _movementText;
    [SerializeField] private GameObject _speakingIndicator;
    [SerializeField] private TextMeshProUGUI _speakingIndicatorText;
    [SerializeField] private GameObject _mutedIndicator;
    #endregion

    // ================== Setup ==================
    #region Setup
    public void OnEnable()
    {
        _playerData = this.GetComponentInParent<PlayerData>();
        _playerHealth = this.GetComponentInParent<PlayerHealth>();

        GameManager.OnStateChange += EnableReadyButton;
        GameManager.OnStateChange += StateChangeEvent;
        PlayerConnectionManager.OnPlayerReady += Ready;
        PlayerConnectionManager.OnPlayerUnready += Unready;
        GameManager.OnStateIntro += DisplayRole;
        VivoxClient.OnBeginSpeaking += SpeakingIndicatorOn;
        VivoxClient.OnEndSpeaking += SpeakingIndicatorOff;
    }

    private void Start()
    {
        // These menus have to start active so setup scripts propperly run on them
        _craftingMenu.SetActive(false);
        _locationMenu.SetActive(false);
    }

    private void OnDisable()
    {
        GameManager.OnStateChange -= EnableReadyButton;
        GameManager.OnStateChange -= StateChangeEvent;
        PlayerConnectionManager.OnPlayerReady -= Ready;
        PlayerConnectionManager.OnPlayerUnready -= Unready;
        GameManager.OnStateIntro -= DisplayRole;
        VivoxClient.OnBeginSpeaking -= SpeakingIndicatorOn;
        VivoxClient.OnEndSpeaking -= SpeakingIndicatorOff;
    }
    #endregion

    // ================== Misc UI ==================
    #region Misc UI
    private void OnDeath()
    {
        DisableReadyButton();

        //MutedIndicatorOn();

        // Close Menus if player died
        _locationMenu.SetActive(false);
        _craftingMenu.SetActive(false);
    }

    public void StateChangeEvent(GameManager.GameState prev, GameManager.GameState current)
    {
        if(_introRole != null && _introRole.activeInHierarchy)
            _introRole.SetActive(false);

        // Close Menus on a state change
        _locationMenu.SetActive(false);
        _craftingMenu.SetActive(false);
    }

    private void EnableReadyButton(GameManager.GameState prev, GameManager.GameState current)
    {
        if (!_playerHealth.IsLiving())
            return;

        _readyButtonIcon.SetActive(true);
    }

    public void DisableReadyButton()
    {
        _readyButton.SetActive(false);
    }

    public void Ready()
    {
        _readyButtonIcon.GetComponent<Image>().sprite = _readySpeedUp;
    }

    public void Unready()
    {
        _readyButtonIcon.GetComponent<Image>().sprite = _readyNormal;
    }

    private void DisplayRole()
    {
        _introRole.SetActive(true);
        TextMeshProUGUI roleText = _introRole.GetComponentInChildren<TextMeshProUGUI>();

        if (_playerData.GetPlayerTeam() == PlayerData.Team.Survivors)
        {
            roleText.text = "Survivors";
            roleText.color = Color.green;
        }
        else if (_playerData.GetPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            roleText.text = "Saboteurs";
            roleText.color = Color.red;
        }
    }

    public void ToggleCraft()
    {
        if (!_playerHealth.IsLiving())
            return;

        _craftingMenu.SetActive(!_craftingMenu.activeSelf);
    }

    public void ToggleMap()
    {
        if (GameManager.Instance.GetCurrentGameState() != GameManager.GameState.Morning)
            return;

        _locationMenu.SetActive(!_locationMenu.activeSelf);
    }

    public void UpdateMovement(int prev, int current)
    {
        _movementText.text = "Movement: " + current;
    }

    public void SpeakingIndicatorOn(VivoxManager.ChannelSeshName channel)
    {
        _speakingIndicator.SetActive(true);

        if (channel == VivoxManager.ChannelSeshName.World)
        {
            _speakingIndicatorText.text = "World";
            _speakingIndicatorText.color = Color.white;
        }
        else if(channel == VivoxManager.ChannelSeshName.Death)
        {
            _speakingIndicatorText.text = "Death";
            _speakingIndicatorText.color = Color.blue;
        }
        else
        {
            _speakingIndicatorText.text = channel.ToString();
            _speakingIndicatorText.color = Color.white;
        }
    }

    public void SpeakingIndicatorOff(VivoxManager.ChannelSeshName channel)
    {
        _speakingIndicator.SetActive(false);
    }
    
    public void MutedIndicatorOn()
    {
        _mutedIndicator.SetActive(true);
        _speakingIndicator.SetActive(false);
    }

    public void MutedIndicatorOff()
    {
        _mutedIndicator.SetActive(false);
    }

    #endregion
}
