using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSpeakingUI : MonoBehaviour
{
    // =========== Variables / Refrences ===========
    [SerializeField] private TextMeshProUGUI _playerName;
    [SerializeField] private Image _micIcon;
    [SerializeField] private string _pName;

    // =========== Function ===========
    public string GetAttachedName()
    {
        return _pName;
    }

    public void DisplayName(string pName, VivoxManager.ChannelSeshName channelSesh)
    {
        gameObject.SetActive(true);

        _pName = pName;

        _playerName.text = _pName;

        SetColor(channelSesh);
    }

    public void ShowName(VivoxManager.ChannelSeshName channelSesh)
    {
        gameObject.SetActive(true);
        SetColor(channelSesh);
    }

    public void HideName()
    {
        gameObject.SetActive(false);
    }

    private void SetColor(VivoxManager.ChannelSeshName channelSesh)
    {
        if (channelSesh == VivoxManager.ChannelSeshName.World)
        {
            _playerName.color = Color.white;
            _micIcon.color = Color.white;
        }
        else if (channelSesh == VivoxManager.ChannelSeshName.Death)
        {
            _playerName.color = Color.cyan;
            _micIcon.color = Color.cyan;
        }
        else if (channelSesh == VivoxManager.ChannelSeshName.Sabo)
        {
            _playerName.color = Color.red;
            _micIcon.color = Color.red;
        }
        else
        {
            _playerName.color = Color.white;
            _micIcon.color = Color.white;
        }
    }
}
