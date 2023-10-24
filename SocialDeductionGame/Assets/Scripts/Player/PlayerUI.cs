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

    [Header("Player Info")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _saboIcon;
    [SerializeField] private GameObject _survivorIcon;
    [SerializeField] private Image _healthFill;
    [SerializeField] private Image _hungerFill;
    [SerializeField] private Image _healthFlash;
    [SerializeField] private Image _hungerFlash;
    [SerializeField] private Image _healthIcon;
    [SerializeField] private Image _hungerIcon;
    [SerializeField] private TextMeshProUGUI _healthNum;
    [SerializeField] private TextMeshProUGUI _hungerNum;
    [SerializeField] private List<Sprite> _healthIconStages;
    [SerializeField] private List<Sprite> _hungerIconStages;
    [SerializeField] private TextMeshProUGUI _dangerText;
    [SerializeField] private Image _dangerIcon;
    [SerializeField] private List<Sprite> _dangerIconStages;
    [SerializeField] private GameObject _deathMessage;

    [Header("Other")]
    [SerializeField] private GameObject _readyButton;
    [SerializeField] private GameObject _readyButtonIcon;
    [SerializeField] private Sprite _readyNormal;
    [SerializeField] private Sprite _readySpeedUp;
    [SerializeField] private GameObject _islandMap;
    [SerializeField] private GameObject _craftingMenu;
    [SerializeField] private GameObject _introRole;
    [SerializeField] private TextMeshProUGUI _movementText;
    [SerializeField] private GameObject _speakingIndicator;
    [SerializeField] private GameObject _mutedIndicator;
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
        PlayerHealth.OnHealthModified += UpdateHealth;
        PlayerHealth.OnHungerModified += UpdateHunger;
        PlayerHealth.OnDeath += OnDeath;
        VivoxClient.OnBeginSpeaking += SpeakingIndicatorOn;
        VivoxClient.OnEndSpeaking += SpeakingIndicatorOff;
    }

    private void OnDisable()
    {
        _playerData._netPlayerName.OnValueChanged -= UpdatePlayerNameText;
        GameManager.OnStateChange -= EnableReadyButton;
        GameManager.OnStateChange -= StateChangeEvent;
        PlayerConnectionManager.OnPlayerReady -= Ready;
        PlayerConnectionManager.OnPlayerUnready -= Unready;
        GameManager.OnStateIntro -= DisplayRole;
        PlayerHealth.OnHealthModified -= UpdateHealth;
        PlayerHealth.OnHungerModified -= UpdateHunger;
        PlayerHealth.OnDeath -= OnDeath;
        VivoxClient.OnBeginSpeaking -= SpeakingIndicatorOn;
        VivoxClient.OnEndSpeaking -= SpeakingIndicatorOff;
    }
    #endregion

    // ================== Player Info ==================
    #region Player Info
    public void UpdatePlayerNameText(FixedString32Bytes old, FixedString32Bytes current)
    {
        _nameText.text = current.ToString();
    }

    public void UpdateTeam(PlayerData.Team current)
    {
        if (current == PlayerData.Team.Survivors)
        {
            _saboIcon.SetActive(false);
            _survivorIcon.SetActive(true);
        }
        else if (current == PlayerData.Team.Saboteurs)
        {
            _saboIcon.SetActive(true);
            _survivorIcon.SetActive(false);
        }
    }

    private void UpdateHealth(float ModifiedAmmount, float newTotal)
    {
        // Changed flash
        if (ModifiedAmmount == 0)
            return;

        if (ModifiedAmmount > 0)
            _healthFlash.color = Color.green;
        else if (ModifiedAmmount < 0)
            _healthFlash.color = Color.red;

        _healthFlash.DOFade(0.8f, 0.25f).SetEase(Ease.Flash).OnComplete(() => { _healthFlash.DOFade(0, 0.25f).SetEase(Ease.Flash); });

        // Update Icon
        if(newTotal <= 2) // 1 or 0
        {
            _healthIcon.sprite = _healthIconStages[2];
            _healthFill.color = Color.red;
        }
        else if (newTotal >= _playerHealth.GetMaxHP()-1) // at or 1 below max
        {
            _healthIcon.sprite = _healthIconStages[0];
            _healthFill.color = Color.green;
        }
        else // anything in between
        {
            _healthIcon.sprite = _healthIconStages[1];
            _healthFill.color = Color.yellow;
        }

        // Update Fill ammount
        _healthFill.fillAmount = (newTotal / _playerHealth.GetMaxHP());

        // Update number
        _healthNum.text = newTotal.ToString();
    }

    private void UpdateHunger(float ModifiedAmmount, float newTotal)
    {
        // Changed flash
        if (ModifiedAmmount == 0)
            return;

        if (ModifiedAmmount > 0)
            _hungerFlash.color = Color.green;
        else if (ModifiedAmmount < 0)
            _hungerFlash.color = Color.red;

        _hungerFlash.DOFade(0.8f, 0.25f).OnComplete(() => { _hungerFlash.DOFade(0, 0.25f); });

        // Update Icon
        if (newTotal <= 1) // 1 or 0
        {
            _hungerIcon.sprite = _hungerIconStages[2];
            _hungerFill.color = Color.red;
        }
        else if (newTotal >= _playerHealth.GetMaxHunger() - 1) // at or 1 below max
        {
            _hungerIcon.sprite = _hungerIconStages[0];
            _hungerFill.color = Color.green;
        }
        else // anything in between
        {
            _hungerIcon.sprite = _hungerIconStages[1];
            _hungerFill.color = Color.yellow;
        }

        // Update Fill ammount
        _hungerFill.fillAmount = (newTotal / _playerHealth.GetMaxHunger());

        // Update number
        _hungerNum.text = newTotal.ToString();
    }

    public void UpdateDanger(int prev, int current)
    {
        _dangerText.text = current.ToString();

        // Should not hard code this (should have value refrences)
        _dangerText.color = new Color32(233, 195, 41, 255);
        _dangerIcon.sprite = _dangerIconStages[2];
        if (4 < current && current <= 8)
        {
            _dangerText.color = new Color32(217, 116, 24, 255);
            _dangerIcon.sprite = _dangerIconStages[1];
        }
        else if (8 < current)
        {
            _dangerText.color = new Color32(206, 60, 24, 255);
            _dangerIcon.sprite = _dangerIconStages[0];
        }
    }

    private void OnDeath()
    {
        DisableReadyButton();

        _deathMessage.SetActive(true);

        MutedIndicatorOn();

        // Close Menus if player died
        _islandMap.SetActive(false);
        _craftingMenu.SetActive(false);
    }
    #endregion

    // ================== Misc UI ==================
    #region Misc UI
    public void StateChangeEvent(GameManager.GameState prev, GameManager.GameState current)
    {
        if(_introRole != null && _introRole.activeInHierarchy)
            _introRole.SetActive(false);

        // Close Menus on a state change
        _islandMap.SetActive(false);
        _craftingMenu.SetActive(false);
    }

    private void EnableReadyButton(GameManager.GameState prev, GameManager.GameState current)
    {
        if (!_playerHealth.IsLiving())
            return;

        _readyButtonIcon.SetActive(true);
    }

    public void DisableReadyButton()
    {
        _readyButton.SetActive(false);
    }

    public void Ready()
    {
        _readyButtonIcon.GetComponent<Image>().sprite = _readySpeedUp;
    }

    public void Unready()
    {
        _readyButtonIcon.GetComponent<Image>().sprite = _readyNormal;
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

    public void ToggleCraft()
    {
        if (!_playerHealth.IsLiving())
            return;

        _craftingMenu.SetActive(!_craftingMenu.activeSelf);
    }

    public void ToggleMap()
    {
        if (!_playerHealth.IsLiving() || GameManager.Instance.GetCurrentGameState() != GameManager.GameState.Morning)
            return;

        _islandMap.SetActive(!_islandMap.activeSelf);
    }

    public void UpdateMovement(int prev, int current)
    {
        _movementText.text = "Movement: " + current;
    }

    public void SpeakingIndicatorOn()
    {
        _speakingIndicator.SetActive(true);
    }

    public void SpeakingIndicatorOff()
    {
        _speakingIndicator.SetActive(false);
    }
    
    public void MutedIndicatorOn()
    {
        _mutedIndicator.SetActive(true);
        _speakingIndicator.SetActive(false);
    }

    public void MutedIndicatorOff()
    {
        _mutedIndicator.SetActive(false);
    }

    #endregion
}
