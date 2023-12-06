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

    // ==================== Setup ====================
    void OnEnable()
    {
        PlayerHealth.OnHealthIncrease += DisplayHealthMessage;
        PlayerHealth.OnHealthDecrease += DisplayHealthMessage;
        PlayerHealth.OnHungerIncrease += DisplayHungerMessage;
        PlayerHealth.OnHungerDecrease += DisplayHungerMessage;
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
        string healthMesage = (ammount.ToString() + " Health: " + mesage);
        DisplayMessage(ammount, healthMesage);
    }

    private void DisplayHungerMessage(int ammount, string mesage)
    {
        string hungerMesage = (ammount.ToString() + " Hunger: " + mesage);
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
