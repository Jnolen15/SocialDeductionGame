using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameInfoUI : MonoBehaviour
{
    // =================== Refrences ===================
    [Header("UI Refrences")]
    [SerializeField] private TextMeshProUGUI _gameStateText;
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private Image _stateTimerFill;
    private bool _updatedDay;

    // =================== Setup ===================
    private void Awake()
    {
        GameManager.OnStateChange += UpdateStateText;
    }

    private void OnDisable()
    {
        GameManager.OnStateChange -= UpdateStateText;
    }

    // =================== Update ===================
    void Update()
    {
        if(GameManager.GetCurrentGameState() == GameManager.GameState.Morning && !_updatedDay)
        {
            UpdateDayText();
            _updatedDay = true;
        } else if (_updatedDay)
        {
            _updatedDay = false;
        }

        // State Timer stuff
        _stateTimerFill.fillAmount = GameManager.GetStateTimer();
    }

    // =================== UI Functions ===================
    private void UpdateStateText()
    {
        _gameStateText.text = GameManager.GetCurrentGameState().ToString();
    }

    private void UpdateDayText()
    {
        _dayText.text = GameManager.GetCurrentDay().ToString();
    }
}
