using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    // =================== Refrences ===================
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _endScreenText;

    // =================== Setup ===================
    private void Awake()
    {
        GameManager.OnGameEnd += OnGameEnd;

        // unpause Game
        Time.timeScale = 1;
    }

    private void OnDisable()
    {
        GameManager.OnGameEnd -= OnGameEnd;
    }

    // =================== UI Functions ===================
    private void OnGameEnd(bool survivorWin)
    {
        Debug.Log("<color=yellow>SERVER: </color>SHOWING GAME OVER SCREEN");
        Show();

        if (survivorWin)
        {
            _endScreenText.text = "Survivors Win";
            _endScreenText.color = Color.green;
        } else
        {
            _endScreenText.text = "Saboteur Wins";
            _endScreenText.color = Color.red;
        }

        // Pause Game
        Time.timeScale = 0;
    }

    public void ReturnToMainMenu()
    {
        ConnectionManager.Instance.Shutdown();
        VivoxManager.Instance.LeaveAll();

        SceneLoader.Load(SceneLoader.Scene.MainMenu);
    }

    private void Show()
    {
        _panel.SetActive(true);
    }

    private void Hide()
    {
        _panel.SetActive(false);
    }
}
