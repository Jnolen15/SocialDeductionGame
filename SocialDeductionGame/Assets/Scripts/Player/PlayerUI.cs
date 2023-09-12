using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.Collections;

public class PlayerUI : MonoBehaviour
{
    private PlayerData _playerData;
    private PlayerHealth _playerHealth;

    [Header("UI Refrences")]
    [SerializeField] private GameObject _readyButton;
    [SerializeField] private GameObject _readyIndicator;
    [SerializeField] private GameObject _islandMap;
    [SerializeField] private TextMeshProUGUI _locationText;
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _hungerText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _deathMessage;
    [SerializeField] private Image _healthFlashSprite;
    [SerializeField] private Image _hungerFlashSprite;

    [Header("Exile Vote UI Refrences")]
    [SerializeField] private GameObject _votePrefab;
    [SerializeField] private Transform _voteArea;
    [SerializeField] private GameObject _exileUI;
    [SerializeField] private GameObject _closeUIButton;

    private bool hasVoted;

    // ================== Setup ==================
    #region Setup
    public void OnEnable()
    {
        _playerData = this.GetComponentInParent<PlayerData>();
        _playerHealth = this.GetComponentInParent<PlayerHealth>();

        _playerData._netPlayerName.OnValueChanged += UpdatePlayerNameText;
        GameManager.OnStateChange += EnableReadyButton;
        PlayerConnectionManager.OnPlayerReady += Ready;
        PlayerConnectionManager.OnPlayerUnready += Unready;
        GameManager.OnStateForage += ToggleMap;
        PlayerHealth.OnHealthModified += UpdateHealth;
        PlayerHealth.OnHungerModified += UpdateHunger;
        PlayerHealth.OnDeath += DisplayDeathMessage;
    }

    private void OnDisable()
    {
        _playerData._netPlayerName.OnValueChanged -= UpdatePlayerNameText;
        GameManager.OnStateChange -= EnableReadyButton;
        PlayerConnectionManager.OnPlayerReady -= Ready;
        PlayerConnectionManager.OnPlayerUnready -= Unready;
        GameManager.OnStateForage -= ToggleMap;
        PlayerHealth.OnHealthModified -= UpdateHealth;
        PlayerHealth.OnHungerModified -= UpdateHunger;
        PlayerHealth.OnDeath -= DisplayDeathMessage;
    }
    #endregion

    // ================== Misc UI ==================
    #region Misc UI
    public void UpdatePlayerNameText(FixedString32Bytes old, FixedString32Bytes current)
    {
        _nameText.text = current.ToString();
    }

    private void EnableReadyButton()
    {
        if (!_playerHealth.IsLiving())
            return;

        _readyButton.SetActive(true);
    }

    public void DisableReadyButton()
    {
        _readyButton.SetActive(false);
    }

    public void Ready()
    {
        _readyIndicator.SetActive(true);

        _readyButton.GetComponent<Image>().color = Color.green;
    }

    public void Unready()
    {
        _readyIndicator.SetActive(false);

        _readyButton.GetComponent<Image>().color = Color.red;
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
        if (ModifiedAmmount == 0)
            return;

        if (ModifiedAmmount > 0)
            _healthFlashSprite.color = Color.green;
        else if (ModifiedAmmount < 0)
            _healthFlashSprite.color = Color.red;

        _healthFlashSprite.DOFade(0.8f, 0.25f).SetEase(Ease.Flash).OnComplete(() => { _healthFlashSprite.DOFade(0, 0.25f).SetEase(Ease.Flash); });

        _healthText.text = newTotal.ToString();
    }

    private void UpdateHunger(float ModifiedAmmount, float newTotal)
    {
        if (ModifiedAmmount == 0)
            return;

        if (ModifiedAmmount > 0)
            _hungerFlashSprite.color = Color.green;
        else if (ModifiedAmmount < 0)
            _hungerFlashSprite.color = Color.red;

        _hungerFlashSprite.DOFade(0.8f, 0.25f).OnComplete(() => { _hungerFlashSprite.DOFade(0, 0.25f); });

        _hungerText.text = newTotal.ToString();
    }

    private void DisplayDeathMessage()
    {
        _deathMessage.SetActive(true);
    }
    #endregion
}
