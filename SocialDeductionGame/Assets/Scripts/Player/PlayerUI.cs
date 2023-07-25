using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    private PlayerHealth _playerHealth;

    [Header("UI Refrences")]
    [SerializeField] private GameObject _readyButton;
    [SerializeField] private GameObject _readyIndicator;
    [SerializeField] private GameObject _islandMap;
    [SerializeField] private TextMeshProUGUI _locationText;
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _hungerText;
    [SerializeField] private GameObject _deathMessage;

    public void OnEnable()
    {
        _playerHealth = this.GetComponentInParent<PlayerHealth>();

        GameManager.OnStateChange += EnableReadyButton;
        GameManager.OnStateForage += ToggleMap;
        PlayerHealth.OnHealthModified += UpdateHealth;
        PlayerHealth.OnHungerModified += UpdateHunger;
        PlayerHealth.OnDeath += DisplayDeathMessage;
    }

    private void OnDisable()
    {
        GameManager.OnStateChange -= EnableReadyButton;
        GameManager.OnStateForage -= ToggleMap;
        PlayerHealth.OnHealthModified -= UpdateHealth;
        PlayerHealth.OnHungerModified -= UpdateHunger;
        PlayerHealth.OnDeath -= DisplayDeathMessage;
    }

    private void EnableReadyButton()
    {
        if (!_playerHealth.IsLiving())
            return;

        _readyButton.SetActive(true);
    }

    public void ToggleReady(bool toggle)
    {
        _readyIndicator.SetActive(toggle);
    }

    private void ToggleMap()
    {
        if (!_playerHealth.IsLiving())
            return;

        _islandMap.SetActive(!_islandMap.activeSelf);
    }

    public void UpdateLocationText(string location)
    {
        _locationText.text = location;
    }

    private void UpdateHealth(float ModifiedAmmount, float newTotal)
    {
        _healthText.text = "Health: " + newTotal;
    }

    private void UpdateHunger(float ModifiedAmmount, float newTotal)
    {
        _hungerText.text = $"Hunger: {newTotal}/3";
    }

    private void DisplayDeathMessage()
    {
        _deathMessage.SetActive(true);
    }
}
