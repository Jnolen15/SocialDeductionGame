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
    [SerializeField] private List<WatchColors> _watchColorList;
    [Header("UI Params")]
    [SerializeField] private CanvasGroup _watchCanvasGroup;
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
    [Header("Ready")]
    [SerializeField] private GameObject _readyButton;
    [SerializeField] private GameObject _readyButtonIcon;
    [SerializeField] private Transform _readyOutPos;
    [SerializeField] private Transform _readyInPos;
    [Header("Sounds")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _beepSFX;
    [SerializeField] private AudioClip _readySFX;
    [SerializeField] private AudioClip _flatlineSFX;


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
        GameManager.OnStateChange += EnableReadyButton;
        GameManager.OnStateChange += OnStateChange;
        GameManager.OnGameEnd += OnGameEnd;
        PlayerConnectionManager.OnPlayerReady += Ready;
        PlayerConnectionManager.OnPlayerUnready += Unready;
    }

    private void Awake()
    {
        int colorIndex = PlayerPrefs.GetInt("WatchColor");
        if (colorIndex < _watchColorList.Count)
            SetColorPallet(_watchColorList[colorIndex]);
        else
            Debug.LogWarning("Watch color index out of bounds " + colorIndex + " of " + _watchColorList.Count);
    }

    private void OnDisable()
    {
        _playerData._netPlayerName.OnValueChanged -= UpdatePlayerNameText;
        PlayerData.OnTeamUpdated -= UpdateTeam;
        PlayerHealth.OnHealthModified -= UpdateHealth;
        PlayerHealth.OnHungerModified -= UpdateHunger;
        PlayerHealth.OnDeath -= OnDeath;
        GameManager.OnStateChange -= UpdateStateUI;
        GameManager.OnStateChange -= EnableReadyButton;
        GameManager.OnStateChange -= OnStateChange;
        GameManager.OnGameEnd -= OnGameEnd;
        PlayerConnectionManager.OnPlayerReady -= Ready;
        PlayerConnectionManager.OnPlayerUnready -= Unready;
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
        if (GameManager.Instance.IsCurrentState(GameManager.GameState.Morning) && !_updatedDay)
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

        //Debug.Log("Setting Flash " + flashActive);
        _curFlashTime = 0;
        _flashActive = flashActive;
    }

    private IEnumerator Flash()
    {
        //Debug.Log("Flash!");

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
        //Debug.Log($"Setting {objName} flashes to {numFlashes}");
        flashObj.SetFlashes(numFlashes);

        SetFlashActive(true);
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
        Debug.Log("<color=green>Watch:</color> Updating player team to " + current, this);

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

        //if (ModifiedAmmount < 0) // Health Down
        //    UpdateFlashObj(_healthWarning.name, _healthWarning, 3);

        // Warning if health low
        if (newTotal <= 1)
        {
            _healthWarning.gameObject.SetActive(true);
            UpdateFlashObj(_healthWarning.name, _healthWarning, 6);
        }
        else
            _healthWarning.gameObject.SetActive(false);

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

        // Play SFX if lost
        if (ModifiedAmmount < 0)
            _audioSource.PlayOneShot(_beepSFX);
    }

    private void UpdateHunger(int ModifiedAmmount, int newTotal)
    {
        if (ModifiedAmmount == 0)
            return;

        //if (ModifiedAmmount < 0) // Hunger Down
        //    UpdateFlashObj(_hungerWarning.name, _hungerWarning, 3);

        // Warning if hunger low
        if (newTotal <= 1)
        {
            _hungerWarning.gameObject.SetActive(true);
            UpdateFlashObj(_hungerWarning.name, _hungerWarning, 6);
        }
        else
            _hungerWarning.gameObject.SetActive(false);

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

        // Play SFX if lost
        if (ModifiedAmmount < 0)
            _audioSource.PlayOneShot(_beepSFX);
    }

    private void OnDeath()
    {
        DisableReadyButton();

        _playerStats.SetActive(false);
        _playerDead.SetActive(true);

        // Play SFX
        _audioSource.PlayOneShot(_flatlineSFX);
    }

    private void OnGameEnd(bool survivorWin)
    {
        _watchCanvasGroup.alpha = 0;
        _watchCanvasGroup.interactable = false;
    }
    #endregion

    // ================== Day Info ==================
    #region Day Info
    private void UpdateStateUI(GameManager.GameState prev, GameManager.GameState current)
    {
        _morningIcon.SetActive(false);
        _afternoonIcon.SetActive(false);
        _eveningIcon.SetActive(false);
        _nightIcon.SetActive(false);

        switch (current)
        {
            case GameManager.GameState.Intro:
                _gameStateText.text = current.ToString();
                break;
            case GameManager.GameState.Morning:
                _morningIcon.SetActive(true);
                _gameStateText.text = current.ToString();
                break;
            case GameManager.GameState.Afternoon:
                _afternoonIcon.SetActive(true);
                _gameStateText.text = current.ToString();
                break;
            case GameManager.GameState.Evening:
                _eveningIcon.SetActive(true);
                _gameStateText.text = current.ToString();
                break;
            case GameManager.GameState.Night:
                _nightIcon.SetActive(true);
                _gameStateText.text = current.ToString();
                break;
            case GameManager.GameState.Midnight:
                _nightIcon.SetActive(true);
                _gameStateText.text = current.ToString();
                break;
        }
    }

    private void UpdateDayText()
    {
        _dayText.text = GameManager.Instance.GetCurrentDay().ToString();
    }
    #endregion

    // ================== Ready ==================
    #region Ready
    private void EnableReadyButton(GameManager.GameState prev, GameManager.GameState current)
    {
        if (!_playerHealth.IsLiving())
            return;

        //_readyButtonIcon.SetActive(true);
    }

    public void DisableReadyButton()
    {
        _readyButton.SetActive(false);
    }

    public void Ready()
    {
        _audioSource.PlayOneShot(_readySFX);

        _readyButton.transform.position = _readyInPos.position;
        _readyButtonIcon.SetActive(true);
    }

    public void Unready()
    {
        //_readyButton.transform.position = _readyOutPos.position;
        _readyButtonIcon.SetActive(false);
    }

    public void OnStateChange(GameManager.GameState prev, GameManager.GameState cur)
    {
        // Can't ready in transition, Night, or Midnight
        if (GameManager.Instance.InTransition() || GameManager.Instance.IsCurrentState(GameManager.GameState.Night)
                || GameManager.Instance.IsCurrentState(GameManager.GameState.Midnight))
            _readyButton.transform.position = _readyInPos.position;
        else
            _readyButton.transform.position = _readyOutPos.position;
    }
    #endregion

    // ================== Colors ==================
    #region Colors
    public void SetColorPallet(WatchColors colors)
    {
        Debug.Log("Setting GPS color");

        _watchColors = colors;

        ColorSetter[] coloredObjs = this.GetComponentsInChildren<ColorSetter>(true);

        foreach(ColorSetter cs in coloredObjs)
        {
            cs.SetColor(colors);
        }
    }
    #endregion
}
