using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoginMessageUI : MonoBehaviour
{
    // ============== Variables ==============
    [SerializeField] private TextMeshProUGUI _message;
    [SerializeField] private GameObject _returnToMain;
    private bool _lobbyLoginComplete;
    private bool _vivoxLoginComplete;

    // ============== Setup ==============
    void Awake()
    {
        LobbyManager.OnAlreadyLoggedIn += Hide;
        LobbyManager.OnLoginComplete += LobbyLoginSuccess;
        VivoxManager.OnLoginSuccess += VivoxLoginSuccess;
        LobbyManager.OnFailLogin += LobbyLoginFail;
        LobbyManager.OnFailedLobbyRefresh += LobbyFetchFail;
    }

    private void OnDestroy()
    {
        LobbyManager.OnAlreadyLoggedIn -= Hide;
        LobbyManager.OnLoginComplete -= LobbyLoginSuccess;
        VivoxManager.OnLoginSuccess -= VivoxLoginSuccess;
        LobbyManager.OnFailLogin -= LobbyLoginFail;
        LobbyManager.OnFailedLobbyRefresh -= LobbyFetchFail;
    }

    // ============== UI Functions ==============
    private void CheckComplete()
    {
        if (!_lobbyLoginComplete)
            return;

        if (!_vivoxLoginComplete)
            return;

        Hide();
    }

    private void LobbyLoginSuccess()
    {
        _lobbyLoginComplete = true;

        CheckComplete();
    }

    private void VivoxLoginSuccess()
    {
        _vivoxLoginComplete = true;

        CheckComplete();
    }

    private void LobbyLoginFail(string reason)
    {
        Show();

        _message.text = reason;
        _returnToMain.SetActive(true);
    }
    
    private void LobbyFetchFail()
    {
        Show();

        _message.text = "Failed to fetch lobbies. Lost connection.";
        _returnToMain.SetActive(true);
    }

    private void Show()
    {
        this.gameObject.SetActive(true);
    }

    private void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
