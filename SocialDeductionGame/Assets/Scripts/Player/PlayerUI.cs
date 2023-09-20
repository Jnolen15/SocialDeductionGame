using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.Collections;

public class PlayerUI : MonoBehaviour
{
    // ================== Variables / Refrences ==================
    #region Variables / Refrences
    private PlayerData _playerData;
    private PlayerHealth _playerHealth;

    [Header("UI Refrences")]
    [SerializeField] private GameObject _readyButton;
    [SerializeField] private GameObject _readyIndicator;
    [SerializeField] private GameObject _islandMap;
    [SerializeField] private GameObject _introRole;
    [SerializeField] private TextMeshProUGUI _locationText;
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _hungerText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _movementText;
    [SerializeField] private GameObject _deathMessage;
    [SerializeField] private Image _healthFlashSprite;
    [SerializeField] private Image _hungerFlashSprite;
    #endregion

    // ================== Setup ==================
    #region Setup
    public void OnEnable()
    {
        _playerData = this.GetComponentInParent<PlayerData>();
        _playerHealth = this.GetComponentInParent<PlayerHealth>();

        _playerData._netPlayerName.OnValueChanged += UpdatePlayerNameText;
        GameManager.OnStateChange += EnableReadyButton;
        GameManager.OnStateChange += StateChangeEvent;
        PlayerConnectionManager.OnPlayerReady += Ready;
        PlayerConnectionManager.OnPlayerUnready += Unready;
        GameManager.OnStateIntro += DisplayRole;
        GameManager.OnStateForage += ToggleMap;
        PlayerHealth.OnHealthModified += UpdateHealth;
        PlayerHealth.OnHungerModified += UpdateHunger;
        PlayerHealth.OnDeath += DisplayDeathMessage;
        _playerData._netCurrentMP.OnValueChanged += UpdateMovement;
    }

    private void OnDisable()
    {
        _playerData._netPlayerName.OnValueChanged -= UpdatePlayerNameText;
        GameManager.OnStateChange -= EnableReadyButton;
        GameManager.OnStateChange += StateChangeEvent;
        PlayerConnectionManager.OnPlayerReady -= Ready;
        PlayerConnectionManager.OnPlayerUnready -= Unready;
        GameManager.OnStateIntro -= DisplayRole;
        GameManager.OnStateForage -= ToggleMap;
        PlayerHealth.OnHealthModified -= UpdateHealth;
        PlayerHealth.OnHungerModified -= UpdateHunger;
        PlayerHealth.OnDeath -= DisplayDeathMessage;
        _playerData._netCurrentMP.OnValueChanged -= UpdateMovement;
    }
    #endregion

    // ================== Misc UI ==================
    #region Misc UI
    public void StateChangeEvent()
    {
        if(_introRole != null && _introRole.activeInHierarchy)
            _introRole.SetActive(false);
    }

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

    private void DisplayRole()
    {
        _introRole.SetActive(true);
        TextMeshProUGUI roleText = _introRole.GetComponentInChildren<TextMeshProUGUI>();

        if (_playerData.GetPlayerTeam() == PlayerData.Team.Survivors)
        {
            roleText.text = "Survivors";
            roleText.color = Color.green;
        }
        else if (_playerData.GetPlayerTeam() == PlayerData.Team.Saboteurs)
        {
            roleText.text = "Saboteurs";
            roleText.color = Color.red;
        }
    }

    public void ToggleMap()
    {
        if (!_playerHealth.IsLiving() || GameManager.Instance.GetCurrentGameState() != GameManager.GameState.Midday)
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

    private void UpdateMovement(int prev, int current)
    {
        _movementText.text = "Movement: " + current;
    }

    private void DisplayDeathMessage()
    {
        _deathMessage.SetActive(true);
    }
    #endregion
}
