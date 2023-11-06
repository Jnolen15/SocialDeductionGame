using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyCustomizationMenu : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private TMP_InputField _lobbyName;
    [SerializeField] private Toggle _lobbyPrivacy;
    [SerializeField] private TMP_Dropdown _sabosDropdown;

    // ============== Functions ==============
    public void CreateLobby()
    {
        TMP_Dropdown.OptionData selected = _sabosDropdown.options[_sabosDropdown.value];

        Debug.Log($"Creating a lobby. name {_lobbyName.text}, is private {_lobbyPrivacy.isOn}, Num Sabos {selected.text}");

        var lobbyData = new LobbyData
        {
            Name = _lobbyName.text,
            IsPrivate = _lobbyPrivacy.isOn,
            MaxPlayers = 6, // Must also be changed in SendlobbyData in LobbyManager
            NumSabos = selected.text,
            NumDays = 9 // Must also be changed in SendlobbyData in LobbyManager
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
}

public struct LobbyData
{
    public string Name;
    public bool IsPrivate;
    public int MaxPlayers;
    public string NumSabos;
    public int NumDays;
}
