using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    // =================== Refrences ===================
    [SerializeField] private TextMeshProUGUI _endScreenText;

    // =================== Setup ===================
    private void Awake()
    {
        GameManager.OnGameEnd += OnGameEnd;
    }

    private void OnDisable()
    {
        GameManager.OnGameEnd -= OnGameEnd;
    }

    // =================== UI Functions ===================
    private void OnGameEnd(bool survivorWin)
    {
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
