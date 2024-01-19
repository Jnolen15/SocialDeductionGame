using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class MenuManager : MonoBehaviour
{
    // ============== Refrences ==============
    [SerializeField] private GameObject _tutorialMenu;
    [SerializeField] private GameObject _settingsMenu;
    [SerializeField] private GameObject _feedbackMenu;
    [SerializeField] private GameObject _bugReportMenu;
    [SerializeField] private TMP_InputField _playerNameIF;
    [SerializeField] private GameObject _playerNameLengthWarning;

    private PlayerNamer _playerNamer;

    // ============== Setup ==============
    private void Awake()
    {
        // Cleanup
        if (NetworkManager.Singleton != null)
            Destroy(NetworkManager.Singleton.gameObject);
        if (LobbyManager.Instance != null)
            Destroy(LobbyManager.Instance.gameObject);
        if (PlayerConnectionManager.Instance != null)
            Destroy(PlayerConnectionManager.Instance.gameObject);
        if (ConnectionManager.Instance != null)
            Destroy(ConnectionManager.Instance.gameObject);
    }

    private void Start()
    {
        _playerNamer = this.GetComponent<PlayerNamer>();

        UpdatePlayerName();
    }

    // ============== Functions ==============
    public void Play()
    {
        Debug.Log("Loading Lobby Scene");
        SceneLoader.Load(SceneLoader.Scene.LobbyScene);
    }

    public void ShowTutorial()
    {
        _tutorialMenu.SetActive(true);
    }

    public void HideTutorial()
    {
        _tutorialMenu.SetActive(false);
    }

    public void ShowSettings()
    {
        _settingsMenu.SetActive(true);
    }

    public void HideSettings()
    {
        _settingsMenu.SetActive(false);
    }

    public void ShowFeedback()
    {
        _feedbackMenu.SetActive(true);
    }

    public void HideFeedback()
    {
        _feedbackMenu.SetActive(false);
    }

    public void ShowBugReport()
    {
        _bugReportMenu.SetActive(true);
    }

    public void HideBugReport()
    {
        _bugReportMenu.SetActive(false);
    }

    public void OpenTwitterProfile()
    {
        Application.OpenURL("https://twitter.com/JaredNolen3");
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void OnEditNameField(string attemptedVal)
    {
        string cleanStr = Regex.Replace(attemptedVal, @"[^a-zA-Z0-9]", "");
        _playerNameIF.text = cleanStr;
    }

    public void OnEndEditNameField(string attemptedVal)
    {
        if(attemptedVal.Length < 2) 
        {
            _playerNameLengthWarning.SetActive(true);
            return;
        }
        else
            _playerNameLengthWarning.SetActive(false);

        _playerNamer.SetPlayerName(attemptedVal);

        UpdatePlayerName();
    }

    private void UpdatePlayerName()
    {
        _playerNameIF.text = _playerNamer.GetPlayerName();
    }
}
