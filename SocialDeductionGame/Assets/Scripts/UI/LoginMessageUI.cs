using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginMessageUI : MonoBehaviour
{
    // ============== Variables ==============
    private bool _lobbyLoginComplete;
    private bool _vivoxLoginComplete;

    // ============== Setup ==============
    void Awake()
    {
        LobbyManager.OnAlreadyLoggedIn += Hide;
        LobbyManager.OnLoginComplete += LobbyLoginSuccess;
        VivoxManager.OnLoginSuccess += VivoxLoginSuccess;
    }

    private void OnDestroy()
    {
        LobbyManager.OnAlreadyLoggedIn -= Hide;
        LobbyManager.OnLoginComplete -= LobbyLoginSuccess;
        VivoxManager.OnLoginSuccess -= VivoxLoginSuccess;
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

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
