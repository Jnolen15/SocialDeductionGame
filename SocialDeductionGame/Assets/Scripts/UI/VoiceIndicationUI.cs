using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoiceIndicationUI : MonoBehaviour
{
    // ================== Variables / Refrences ==================
    #region Variables / Refrences
    [SerializeField] private Image _worldSpeakingIndicator;
    [SerializeField] private Image _saboSpeakingIndicator;
    [SerializeField] private TextMeshProUGUI _worldVoiceHint;
    [SerializeField] private Image _worldVoiceHintIcon;
    [SerializeField] private GameObject _saboVoiceHint;
    [SerializeField] private GameObject _playerSpeakingPref;
    [SerializeField] private Transform _playerSpeakingZone;
    private List<PlayerSpeakingUI> _PlayerSpeakingIndicatorList = new List<PlayerSpeakingUI>();
    #endregion

    // ================== Setup ==================
    #region Setup
    public void OnEnable()
    {
        GameManager.OnStateIntro += Setup;
        PlayerHealth.OnDeath += SwapVoiceIndicator;
        VivoxClient.OnBeginSpeaking += SpeakingIndicatorOn;
        VivoxClient.OnEndSpeaking += SpeakingIndicatorOff;
        VivoxManager.OnVoiceInputStarted += VoiceInputStarted;
        VivoxManager.OnVoiceInputEnded += VoiceInputEnded;
    }

    private void OnDisable()
    {
        GameManager.OnStateIntro -= Setup;
        PlayerHealth.OnDeath -= SwapVoiceIndicator;
        VivoxClient.OnBeginSpeaking -= SpeakingIndicatorOn;
        VivoxClient.OnEndSpeaking -= SpeakingIndicatorOff;
        VivoxManager.OnVoiceInputStarted -= VoiceInputStarted;
        VivoxManager.OnVoiceInputEnded -= VoiceInputEnded;
    }

    private void Setup()
    {
        if (PlayerConnectionManager.Instance.GetLocalPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            _saboVoiceHint.SetActive(true);
        }
    }
    #endregion

    // ================== Players Speaking ==================
    #region Players Speaking
    private void VoiceInputStarted(string displayName, VivoxManager.ChannelSeshName channelName)
    {
        // Test to see if there already is an object
        foreach (PlayerSpeakingUI pSpeaking in _PlayerSpeakingIndicatorList)
        {
            if(pSpeaking.GetAttachedName() == displayName)
            {
                pSpeaking.ShowName(channelName);
                return;
            }
        }

        // Create a new object since one does not exist
        PlayerSpeakingUI newPSpeaking = Instantiate(_playerSpeakingPref, _playerSpeakingZone).GetComponent<PlayerSpeakingUI>();
        newPSpeaking.DisplayName(displayName, channelName);
        _PlayerSpeakingIndicatorList.Add(newPSpeaking);
    }

    private void VoiceInputEnded(string displayName, VivoxManager.ChannelSeshName channelName)
    {
        foreach (PlayerSpeakingUI pSpeaking in _PlayerSpeakingIndicatorList)
        {
            if (pSpeaking.GetAttachedName() == displayName)
            {
                pSpeaking.HideName();
                break;
            }
        }
    }
    #endregion

    // ================== Indicators ==================
    #region Indicators
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
}
