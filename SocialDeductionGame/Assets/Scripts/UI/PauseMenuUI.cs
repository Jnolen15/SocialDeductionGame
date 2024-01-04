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
        _pauseUI.SetActive(!_pauseUI.activeSelf);
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
