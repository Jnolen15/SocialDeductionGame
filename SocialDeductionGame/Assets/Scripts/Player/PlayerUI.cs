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
    [SerializeField] private TextMeshProUGUI _teamText;
    [SerializeField] private Image _healthFill;
    [SerializeField] private Image _hungerFill;
    [SerializeField] private Image _healthFlash;
    [SerializeField] private Image _hungerFlash;
    [SerializeField] private Image _healthIcon;
    [SerializeField] private Image _hungerIcon;
    [SerializeField] private List<Sprite> _healthIconStages;
    [SerializeField] private List<Sprite> _hungerIconStages;
    [SerializeField] private TextMeshProUGUI _dangerText;
    [SerializeField] private Image _dangerIcon;
    [SerializeField] private List<Sprite> _dangerIconStages;
    [SerializeField] private GameObject _deathMessage;

    [Header("Other")]
    [SerializeField] private GameObject _readyButton;
    [SerializeField] private GameObject _islandMap;
    [SerializeField] private GameObject _craftingMenu;
    [SerializeField] private GameObject _introRole;
    [SerializeField] private TextMeshProUGUI _locationText;
    [SerializeField] private TextMeshProUGUI _movementText;
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
    }
    #endregion

    // ================== Player Info ==================
    #region Player Info
    public void UpdatePlayerNameText(FixedString32Bytes old, FixedString32Bytes current)
    {
        _nameText.text = current.ToString();
    }

    public void UpdateTeamText(PlayerData.Team current)
    {
        if (current == PlayerData.Team.Survivors)
        {
            _teamText.text = "S";
            _teamText.color = Color.green;
        }
        else if (current == PlayerData.Team.Saboteurs)
        {
            _teamText.text = "T";
            _teamText.color = Color.red;
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
    }

    public void UpdateDanger(int prev, int current)
    {
        _dangerText.text = current.ToString();

        // Should not hard code this (should have value refrences)
        _dangerText.color = Color.green;
        _dangerIcon.sprite = _dangerIconStages[2];
        if (4 < current && current <= 8)
        {
            _dangerText.color = Color.yellow;
            _dangerIcon.sprite = _dangerIconStages[1];
        }
        else if (8 < current)
        {
            _dangerIcon.sprite = _dangerIconStages[0];
            _dangerText.color = Color.red;
        }
    }

    private void DisplayDeathMessage()
    {
        _deathMessage.SetActive(true);
    }
    #endregion

    // ================== Misc UI ==================
    #region Misc UI
    public void StateChangeEvent()
    {
        if(_introRole != null && _introRole.activeInHierarchy)
            _introRole.SetActive(false);
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
        //_readyIndicator.SetActive(true);

        _readyButton.GetComponent<Image>().color = Color.green;
    }

    public void Unready()
    {
        //_readyIndicator.SetActive(false);

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

    public void ToggleCraft()
    {
        if (!_playerHealth.IsLiving())
            return;

        _craftingMenu.SetActive(!_craftingMenu.activeSelf);
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

    

    public void UpdateMovement(int prev, int current)
    {
        _movementText.text = "Movement: " + current;
    }

    
    #endregion
}
