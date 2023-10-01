using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameInfoUI : MonoBehaviour
{
    // =================== Refrences ===================
    [Header("Day Info Refrences")]
    [SerializeField] private TextMeshProUGUI _gameStateText;
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private Image _stateTimerBackground;
    [SerializeField] private Image _stateTimerFill;
    private bool _updatedDay;

    [Header("Timer Images")]
    [SerializeField] private Sprite _morningTimerSprite;
    [SerializeField] private Sprite _afternoonTimerSprite;
    [SerializeField] private Sprite _eveningTimerSprite;
    [SerializeField] private Sprite _nightTimerSprite;

    [Header("Event Refrences")]
    [SerializeField] private GameObject _eventTagIconPref;
    [SerializeField] private TextMeshProUGUI _eventThumbnailTitle;
    [SerializeField] private TextMeshProUGUI _eventThumbnailRequiredNum;
    [SerializeField] private Transform _eventThumbnailTagIconSlot;
    [SerializeField] private NightEventCardVisual _eventCardSmall;
    [SerializeField] private NightEventCardVisual _eventCardLarge;
    private int _currentNightEventID;

    [Header("Quit Menu")]
    [SerializeField] private GameObject _quitMenu;

    // =================== Setup ===================
    #region Setup
    private void Awake()
    {
        GameManager.OnStateChange += UpdateStateUI;
        GameManager.OnStateForage += HideLargeCard;
    }

    private void OnDisable()
    {
        GameManager.OnStateChange -= UpdateStateUI;
        GameManager.OnStateForage -= HideLargeCard;
    }
    #endregion

    // =================== Update ===================
    #region Update
    void Update()
    {
        // Quit Menu
        if (Input.GetKeyDown(KeyCode.Escape))
            _quitMenu.SetActive(true);

        // Update Day
        if (GameManager.Instance.GetCurrentGameState() == GameManager.GameState.Morning && !_updatedDay)
        {
            UpdateDayText();
            _updatedDay = true;
        } else if (_updatedDay)
        {
            _updatedDay = false;
        }

        // State Timer stuff
        _stateTimerFill.fillAmount = GameManager.Instance.GetStateTimer();
    }
    #endregion

    // =================== UI Functions ===================
    public void QuitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
    }

    public void ReturnToMainMenu()
    {
        ConnectionManager.Instance.Shutdown();

        SceneLoader.Load(SceneLoader.Scene.MainMenu);
    }

    #region Day Info
    private void UpdateStateUI()
    {
        _gameStateText.text = GameManager.Instance.GetCurrentGameState().ToString();

        switch (GameManager.Instance.GetCurrentGameState())
        {
            case GameManager.GameState.Morning:
                _stateTimerBackground.sprite = _morningTimerSprite;
                _stateTimerFill.sprite = _morningTimerSprite;
                break;
            case GameManager.GameState.Afternoon:
                _stateTimerBackground.sprite = _afternoonTimerSprite;
                _stateTimerFill.sprite = _afternoonTimerSprite;
                break;
            case GameManager.GameState.Evening:
                _stateTimerBackground.sprite = _eveningTimerSprite;
                _stateTimerFill.sprite = _eveningTimerSprite;
                break;
            case GameManager.GameState.Night:
                _stateTimerBackground.sprite = _nightTimerSprite;
                _stateTimerFill.sprite = _nightTimerSprite;
                break;
        }
    }

    private void UpdateDayText()
    {
        _dayText.text = "Day: " + GameManager.Instance.GetCurrentDay().ToString();
    }
    #endregion

    #region Event Info
    public void SetEvent(int eventID, int playerNum)
    {
        Debug.Log("Updating event UI info");

        _currentNightEventID = eventID;

        UpdateEventThumbnail(playerNum);
        UpdateEventCard(playerNum);
    }

    private void UpdateEventThumbnail(int playerNum)
    {
        // Clear tags (in case of reused card assets)
        foreach (Transform t in _eventThumbnailTagIconSlot)
        {
            if (t != _eventThumbnailTagIconSlot.GetChild(0))
                Destroy(t.gameObject);
        }

        // Setup new
        NightEvent eventData = CardDatabase.Instance.GetEvent(_currentNightEventID);
        _eventThumbnailTitle.text = eventData.GetEventName();
        _eventThumbnailRequiredNum.text = eventData.GetSuccessPoints(playerNum) + " = ";
        foreach (CardTag t in eventData.GetRequiredCardTags())
        {
            TagIcon icon = Instantiate(_eventTagIconPref, _eventThumbnailTagIconSlot).GetComponent<TagIcon>();
            icon.SetupIcon(t.visual, t.name);
        }
    }

    private void UpdateEventCard(int playerNum)
    {
        //_eventCardSmall.gameObject.SetActive(true);
        _eventCardLarge.gameObject.SetActive(true);

        _eventCardSmall.Setup(_currentNightEventID, playerNum);
        _eventCardLarge.Setup(_currentNightEventID, playerNum);
    }

    public void ToggleSmallCard()
    {
        _eventCardSmall.gameObject.SetActive(!_eventCardSmall.gameObject.activeSelf);
    }

    private void HideLargeCard()
    {
        _eventCardLarge.gameObject.SetActive(false);
    }

    #endregion
}
