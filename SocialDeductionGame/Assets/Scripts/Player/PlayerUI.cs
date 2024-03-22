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
    [SerializeField] private GameObject _craftingMenu;
    [SerializeField] private CanvasGroup _introRole;
    [SerializeField] private GameObject _introRoleSaboIcon;
    [SerializeField] private GameObject _introRoleSurvivorIcon;
    [SerializeField] private Image _worldSpeakingIndicator;
    [SerializeField] private Image _saboSpeakingIndicator;
    [SerializeField] private TextMeshProUGUI _worldVoiceHint;
    [SerializeField] private Image _worldVoiceHintIcon;
    [SerializeField] private GameObject _saboVoiceHint;
    //[SerializeField] private GameObject _mutedIndicator;
    #endregion

    // ================== Setup ==================
    #region Setup
    public void OnEnable()
    {
        _playerData = this.GetComponentInParent<PlayerData>();
        _playerHealth = this.GetComponentInParent<PlayerHealth>();

        GameManager.OnStateChange += StateChangeEvent;
        GameManager.OnStateIntro += DisplayRole;
        PlayerHealth.OnDeath += SwapVoiceIndicator;
        PlayerHealth.OnDeath += OnDeath;
        VivoxClient.OnBeginSpeaking += SpeakingIndicatorOn;
        VivoxClient.OnEndSpeaking += SpeakingIndicatorOff;

        TabButtonUI.OnCraftingPressed += ToggleCraft;
    }

    private void Start()
    {
        // These menus have to start active so setup scripts propperly run on them
        _craftingMenu.SetActive(false);
    }

    private void OnDisable()
    {
        GameManager.OnStateChange -= StateChangeEvent;
        GameManager.OnStateIntro -= DisplayRole;
        PlayerHealth.OnDeath -= SwapVoiceIndicator;
        PlayerHealth.OnDeath -= OnDeath;
        VivoxClient.OnBeginSpeaking -= SpeakingIndicatorOn;
        VivoxClient.OnEndSpeaking -= SpeakingIndicatorOff;

        TabButtonUI.OnCraftingPressed -= ToggleCraft;
    }
    #endregion

    // ================== Speaking ==================
    #region Speaking
    public void SpeakingIndicatorOn(VivoxManager.ChannelSeshName channel)
    {
        _worldSpeakingIndicator.color = Color.gray;
        _saboSpeakingIndicator.color = Color.gray;

        if (channel == VivoxManager.ChannelSeshName.World)
        {
            _worldSpeakingIndicator.color = Color.white;
        }
        else if (channel == VivoxManager.ChannelSeshName.Death)
        {
            _worldSpeakingIndicator.color = Color.cyan;
        }
        else if (channel == VivoxManager.ChannelSeshName.Sabo)
        {
            _saboSpeakingIndicator.color = Color.red;
        }
        else
        {
            _worldVoiceHint.text = channel.ToString();
            _worldVoiceHint.color = Color.white;
            _worldSpeakingIndicator.color = Color.white;
        }
    }

    public void SpeakingIndicatorOff(VivoxManager.ChannelSeshName channel)
    {
        _worldSpeakingIndicator.color = Color.gray;
        _saboSpeakingIndicator.color = Color.gray;
    }

    // Sawp Indicator to show death channel when die
    private void SwapVoiceIndicator()
    {
        _saboVoiceHint.gameObject.SetActive(false);
        _worldVoiceHint.text = "Death [C]";
        _worldVoiceHint.color = Color.cyan;
        _worldVoiceHintIcon.color = Color.cyan;
    }
    #endregion

    // ================== Misc UI ==================
    #region Misc UI
    private void OnDeath()
    {
        // Close Menus if player died
        _craftingMenu.SetActive(false);
    }

    public void StateChangeEvent(GameManager.GameState prev, GameManager.GameState current)
    {
        // Close Menus on a state change
        _craftingMenu.SetActive(false);
    }

    // Display Role Intro
    private void DisplayRole()
    {
        _introRole.gameObject.SetActive(true);
        TextMeshProUGUI roleText = _introRole.GetComponentInChildren<TextMeshProUGUI>();

        if (_playerData.GetPlayerTeam() == PlayerData.Team.Survivors)
        {
            roleText.text = "Survivors";
            roleText.color = Color.green;
            _introRoleSurvivorIcon.SetActive(true);
        }
        else if (_playerData.GetPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            roleText.text = "Saboteurs";
            roleText.color = Color.red;
            _introRoleSaboIcon.SetActive(true);

            // Show sabo Voice channel
            _saboVoiceHint.SetActive(true);
        }

        Sequence IntroRoleSequence = DOTween.Sequence();
        IntroRoleSequence.Append(_introRole.DOFade(1, 0.5f))
          .AppendInterval(3)
          .Append(_introRole.DOFade(0, 0.5f))
          .AppendCallback(() => _introRole.gameObject.SetActive(false));
    }

    // Crafting Open/Close
    public void ToggleCraft()
    {
        if (!_playerHealth.IsLiving())
            return;

        _craftingMenu.SetActive(!_craftingMenu.activeSelf);
    }
    #endregion
}
