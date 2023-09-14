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

    // ============== Functions ==============
    public void CreateLobby()
    {
        Debug.Log($"Creating a lobby. name {_lobbyName.text}, is private {_lobbyPrivacy.isOn}");
        LobbyManager.Instance.CreateLobby(_lobbyName.text, _lobbyPrivacy.isOn);
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
