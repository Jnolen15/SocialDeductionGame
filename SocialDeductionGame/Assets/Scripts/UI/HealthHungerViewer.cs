using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HealthHungerViewer : MonoBehaviour
{
    [SerializeField] private Transform _logPannel;
    [SerializeField] private Transform _messageZone;
    [SerializeField] private GameObject _logMessagePref;
    [SerializeField] private Transform _hidden;
    [SerializeField] private Transform _shown;
    private bool _isShown = true;
    private GameManager _gameManager;

    // ==================== Setup ====================
    void OnEnable()
    {
        PlayerHealth.OnHealthIncrease += DisplayHealthMessage;
        PlayerHealth.OnHealthDecrease += DisplayHealthMessage;
        PlayerHealth.OnHungerIncrease += DisplayHungerMessage;
        PlayerHealth.OnHungerDecrease += DisplayHungerMessage;
    }

    private void Start()
    {
        _gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    void OnDisable()
    {
        PlayerHealth.OnHealthIncrease -= DisplayHealthMessage;
        PlayerHealth.OnHealthDecrease -= DisplayHealthMessage;
        PlayerHealth.OnHungerIncrease -= DisplayHungerMessage;
        PlayerHealth.OnHungerDecrease -= DisplayHungerMessage;
    }

    // ==================== Function ====================
    private void DisplayHealthMessage(int ammount, string mesage)
    {
        float timeStamp = (_gameManager.GetStateTimer() * 100);
        string healthMesage = ($"{_gameManager.GetCurrentGameState()} {timeStamp.ToString("F2")} {ammount} Health: {mesage}");
        DisplayMessage(ammount, healthMesage);
    }

    private void DisplayHungerMessage(int ammount, string mesage)
    {
        float timeStamp = (_gameManager.GetStateTimer() * 100);
        string hungerMesage = ($"{_gameManager.GetCurrentGameState()} {timeStamp.ToString("F2")} {ammount} Hunger: {mesage}");
        DisplayMessage(ammount, hungerMesage);
    }

    private void DisplayMessage(int ammount, string mesage)
    {
        GameObject logMessage = Instantiate(_logMessagePref, _messageZone);

        TextMeshProUGUI logText = logMessage.GetComponentInChildren<TextMeshProUGUI>();
        logText.text = mesage;

        if (ammount > 0)
            logText.color = Color.green;
        else
            logText.color = Color.red;
    }

    public void ToggleVisible()
    {
        _isShown = !_isShown;

        if (!_isShown)
            _logPannel.transform.position = _hidden.position;
        else
            _logPannel.transform.position = _shown.position;
    }
}
