using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    // =================== Refrences ===================
    [Header("Pause Refrences")]
    [SerializeField] private GameObject _pauseUI;
    [SerializeField] private GameObject _voiceSettings;
    [SerializeField] private GameObject _gameSettings;
    [SerializeField] private PlayRandomSound _randomBookSound;

    // =================== Update ===================
    #region Update
    void Update()
    {
        // Quit Menu
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }
    #endregion

    // =================== UI Functions ===================
    public void TogglePause()
    {
        _randomBookSound.PlayRandom();
        _pauseUI.SetActive(!_pauseUI.activeSelf);
    }

    public void ShowVoiceSettings()
    {
        _voiceSettings.SetActive(true);
        _gameSettings.SetActive(false);
    }

    public void ShowGameSettings()
    {
        _voiceSettings.SetActive(false);
        _gameSettings.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
    }

    public void ReturnToMainMenu()
    {
        ConnectionManager.Instance.Shutdown();
        VivoxManager.Instance.LeaveAll();

        SceneLoader.Load(SceneLoader.Scene.MainMenu);
    }
}
