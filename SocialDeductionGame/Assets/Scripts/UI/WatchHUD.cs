using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

public class WatchHUD : MonoBehaviour
{
    // ================== Variables / Refrences ==================
    #region Variables / Refrences
    private PlayerData _playerData;
    private PlayerHealth _playerHealth;

    [Header("Colors")]
    [SerializeField] private WatchColors _watchColors;
    [Header("UI Params")]
    [SerializeField] private float _flashDuration;
    [SerializeField] private float _flashPause;
    private float _curFlashTime;
    private bool _flashActive;
    [Header("Player Name and Team")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _saboIcon;
    [SerializeField] private GameObject _survivorIcon;
    [Header("Day Info Refrences")]
    [SerializeField] private TextMeshProUGUI _gameStateText;
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private Image _stateTimerFill;
    [SerializeField] private GameObject _morningIcon;
    [SerializeField] private GameObject _afternoonIcon;
    [SerializeField] private GameObject _eveningIcon;
    [SerializeField] private GameObject _nightIcon;
    private bool _updatedDay;
    [Header("Player Stats")]
    [SerializeField] private GameObject _playerStats;
    [SerializeField] private GameObject _playerDead;
    [SerializeField] private List<Image> _healthSegments;
    [SerializeField] private Image _healthWarning;
    [SerializeField] private List<Image> _hungerSegments;
    [SerializeField] private Image _hungerWarning;


    private Dictionary<string, UIFlashObj> _flashDict = new();
    private class UIFlashObj
    {
        public Image Sprite;
        public int NumFlashes;

        public UIFlashObj(Image sprite, int numFlashes)
        {
            Sprite = sprite;
            NumFlashes = numFlashes;
        }

        public void SetFlashes(int numFlashes)
        {
            NumFlashes = numFlashes;
        }

        public int GetFlashes()
        {
            return NumFlashes;
        }

        public void DecrementFlashes()
        {
            NumFlashes--;
        }
    }
    #endregion

    // ================== Setup ==================
    #region Setup
    // COMENTED OUT ONLY FOR TESTING
    public void OnEnable()
    {
        _playerData = this.GetComponentInParent<PlayerData>();
        _playerHealth = this.GetComponentInParent<PlayerHealth>();

        _playerData._netPlayerName.OnValueChanged += UpdatePlayerNameText;
        PlayerData.OnTeamUpdated += UpdateTeam;
        PlayerHealth.OnHealthModified += UpdateHealth;
        PlayerHealth.OnHungerModified += UpdateHunger;
        PlayerHealth.OnDeath += OnDeath;
        GameManager.OnStateChange += UpdateStateUI;
    }

    private void OnDisable()
    {
        _playerData._netPlayerName.OnValueChanged -= UpdatePlayerNameText;
        PlayerData.OnTeamUpdated -= UpdateTeam;
        PlayerHealth.OnHealthModified -= UpdateHealth;
        PlayerHealth.OnHungerModified -= UpdateHunger;
        PlayerHealth.OnDeath -= OnDeath;
        GameManager.OnStateChange -= UpdateStateUI;
    }
    #endregion 

    // ================== Update ==================
    #region Update
    private void Update()
    {
        // Flash Stuff
        if (_flashActive)
            RunFlashTimer();

        // Update Day
        UpdateGameInfo();
    }

    private void RunFlashTimer()
    {
        // Every X seconds flash
        // Loop through list of flash objects and flash
        if (_curFlashTime > 0)
            _curFlashTime -= Time.deltaTime;
        else
        {
            StartCoroutine(Flash());

            _curFlashTime = _flashPause;
        }
    }

    private void UpdateGameInfo()
    {
        if (GameManager.Instance.GetCurrentGameState() == GameManager.GameState.Morning && !_updatedDay)
        {
            UpdateDayText();
            _updatedDay = true;
        }
        else if (_updatedDay)
        {
            _updatedDay = false;
        }

        // State Timer stuff
        _stateTimerFill.fillAmount = GameManager.Instance.GetStateTimer();
    }
    #endregion

    // ================== Helpers ==================
    #region Helpers
    private void SetFlashActive(bool flashActive)
    {
        if (_flashActive == flashActive)
            return;

        Debug.Log("Setting Flash " + flashActive);
        _curFlashTime = 0;
        _flashActive = flashActive;
    }

    private IEnumerator Flash()
    {
        Debug.Log("Flash!");

        bool flashing = false;

        foreach (UIFlashObj flashObj in _flashDict.Values)
        {
            if(flashObj.GetFlashes() > 0)
            {
                flashObj.Sprite.color = _watchColors.GetSecondaryColor();
                flashing = true;
            }
        }

        yield return new WaitForSeconds(_flashDuration);

        foreach (UIFlashObj flashObj in _flashDict.Values)
        {
            if (flashObj.GetFlashes() > 0)
            {
                flashObj.Sprite.color = _watchColors.GetPrimaryColor();
                flashObj.DecrementFlashes();
            }
        }

        // If nothing flashing, set flashing to false
        if(!flashing)
            SetFlashActive(false);
    }

    private void UpdateFlashObj(string objName, Image objImage, int numFlashes)
    {
        // First check if flash object exists, if not make one
        if (!_flashDict.ContainsKey(objName))
        {
            // Make new entry and add to dictionary
            _flashDict.Add(objName, new UIFlashObj(objImage, numFlashes));
        }

        UIFlashObj flashObj = _flashDict[objName];

        if(flashObj == null)
        {
            Debug.LogError($"Flash object {objName} not found in dictionary! Aborting");
            return;
        }

        // Add flashes
        Debug.Log($"Setting {objName} flashes to {numFlashes}");
        flashObj.SetFlashes(numFlashes);

        SetFlashActive(true);
    }
    #endregion

    // ================== UI TESTING DELETE LATER ==================
    #region UI TESTING DELETE LATER
    [Header("TESTING ONLY DELETE LATER")]
    public int CurrentHP;
    public int CurrentHunger;

    [Button("TestSetColors")]
    private void TestSetColors(WatchColors colors)
    {
        if (!colors)
            return;

        SetColorPallet(colors);
    }

    [Button("TestLooseHP")]
    private void TestLooseHP(int num)
    {
        if(num == 0)
            CurrentHP--;
        else
            CurrentHP -= num;

        if (CurrentHP < 0)
            CurrentHP = 0;
        else if (CurrentHP > 6)
            CurrentHP = 6;

        UpdateHealth(-1, CurrentHP);
    }

    [Button("TestGainHP")]
    private void TestGainHP(int num)
    {
        if (num == 0)
            CurrentHP++;
        else
            CurrentHP += num;

        if (CurrentHP < 0)
            CurrentHP = 0;
        else if (CurrentHP > 6)
            CurrentHP = 6;

        UpdateHealth(1, CurrentHP);
    }

    [Button("TestLooseHunger")]
    private void TestLooseHunger(int num)
    {
        if (num == 0)
            CurrentHunger--;
        else
            CurrentHunger -= num;

        if (CurrentHunger < 0)
            CurrentHunger = 0;
        else if (CurrentHunger > 6)
            CurrentHunger = 6;

        UpdateHunger(-1, CurrentHunger);
    }

    [Button("TestGainHunger")]
    private void TestGainHunger(int num)
    {
        if (num == 0)
            CurrentHunger++;
        else
            CurrentHunger += num;

        if (CurrentHunger < 0)
            CurrentHunger = 0;
        else if (CurrentHunger > 12)
            CurrentHunger = 12;

        UpdateHunger(1, CurrentHunger);
    }
    #endregion

    // ================== Player Name and Team ==================
    #region Player Name and Team
    public void UpdatePlayerNameText(FixedString32Bytes old, FixedString32Bytes current)
    {
        _nameText.text = current.ToString();
    }

    public void UpdateTeam(PlayerData.Team prev, PlayerData.Team current)
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
    #endregion

    // ================== Player Info ==================
    #region Player Info
    private void UpdateHealth(int ModifiedAmmount, int newTotal)
    {
        if (ModifiedAmmount == 0)
            return;

        if (ModifiedAmmount < 0) // Health Down
            UpdateFlashObj(_healthWarning.name, _healthWarning, 3);

        // Update segments
        int place = 0;
        foreach(Image image in _healthSegments)
        {
            // Flash segement where health is
            if (place == (newTotal - 1))
                UpdateFlashObj(_healthSegments[place].gameObject.name, _healthSegments[place], 3);
            else
                UpdateFlashObj(_healthSegments[place].gameObject.name, _healthSegments[place], 0);

            // Update color
            if (place <= newTotal-1)
                image.color = _watchColors.GetPrimaryColor();
            else
                image.color = _watchColors.GetSecondaryColor();

            place++;
        }
    }

    private void UpdateHunger(int ModifiedAmmount, int newTotal)
    {
        if (ModifiedAmmount == 0)
            return;

        if (ModifiedAmmount < 0) // Hunger Down
            UpdateFlashObj(_hungerWarning.name, _hungerWarning, 3);

        // Update segments
        int place = 0;
        foreach (Image image in _hungerSegments)
        {
            // Flash segement where health is
            if (place == (newTotal - 1))
                UpdateFlashObj(_hungerSegments[place].gameObject.name, _hungerSegments[place], 3);
            else
                UpdateFlashObj(_hungerSegments[place].gameObject.name, _hungerSegments[place], 0);

            // Update color
            if (place <= newTotal - 1)
                image.color = _watchColors.GetPrimaryColor();
            else
                image.color = _watchColors.GetSecondaryColor();

            place++;
        }
    }

    private void OnDeath()
    {
        _playerStats.SetActive(false);
        _playerDead.SetActive(true);
    }
    #endregion

    // ================== Day Info ==================
    #region Day Info
    private void UpdateStateUI(GameManager.GameState prev, GameManager.GameState current)
    {
        _gameStateText.text = current.ToString();

        _morningIcon.SetActive(false);
        _afternoonIcon.SetActive(false);
        _eveningIcon.SetActive(false);
        _nightIcon.SetActive(false);

        switch (current)
        {
            case GameManager.GameState.Morning:
                _morningIcon.SetActive(true);
                break;
            case GameManager.GameState.Afternoon:
                _afternoonIcon.SetActive(true);
                break;
            case GameManager.GameState.Evening:
                _eveningIcon.SetActive(true);
                break;
            case GameManager.GameState.Night:
                _nightIcon.SetActive(true);
                break;
        }
    }

    private void UpdateDayText()
    {
        _dayText.text = GameManager.Instance.GetCurrentDay().ToString();
    }
    #endregion

    // ================== Colors ==================
    #region Colors
    public void SetColorPallet(WatchColors colors)
    {
        _watchColors = colors;

        ColorSetter[] coloredObjs = this.GetComponentsInChildren<ColorSetter>();

        foreach(ColorSetter cs in coloredObjs)
        {
            cs.SetColor(colors);
        }
    }
    #endregion
}
