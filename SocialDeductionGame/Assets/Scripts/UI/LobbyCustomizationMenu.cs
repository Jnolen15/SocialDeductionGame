using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class LobbyCustomizationMenu : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private TMP_InputField _lobbyName;
    [SerializeField] private GameObject _lobbyNameWarning;
    [SerializeField] private Toggle _lobbyPrivacy;
    [SerializeField] private TextMeshProUGUI _maxPlayersText;
    [SerializeField] private int _maxPlayers;

    // ============== Functions ==============
    public void CreateLobby()
    {
        Debug.Log($"Creating a lobby. name {_lobbyName.text}, is private {_lobbyPrivacy.isOn}");

        if (_lobbyName.text.Length < 5)
        {
            _lobbyNameWarning.SetActive(true);
            return;
        }

        var lobbyData = new LobbyData
        {
            Name = _lobbyName.text,
            IsPrivate = _lobbyPrivacy.isOn,
            MaxPlayers = _maxPlayers,
        };

        LobbyManager.Instance.CreateLobby(lobbyData);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void IncrementMaxPlayers()
    {
        if(_maxPlayers < 6)
            _maxPlayers++;

        _maxPlayersText.text = _maxPlayers.ToString();
    }

    public void DecrementMaxPlayers()
    {
        if (_maxPlayers > 4)
            _maxPlayers--;

        _maxPlayersText.text = _maxPlayers.ToString();
    }

    public void InputValueChanged(string attemptedVal)
    {
        string cleanStr = Regex.Replace(attemptedVal, @"[^a-zA-Z0-9]", "");
        _lobbyName.text = cleanStr;
    }
}

public struct LobbyData
{
    public string Name;
    public bool IsPrivate;
    public int MaxPlayers;
}
