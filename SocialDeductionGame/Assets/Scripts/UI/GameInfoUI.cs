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

    [Header("Quit Menu")]
    [SerializeField] private GameObject _quitMenu;

    // =================== Setup ===================
    #region Setup
    private void Awake()
    {
        GameManager.OnStateChange += UpdateStateUI;
    }

    private void OnDisable()
    {
        GameManager.OnStateChange -= UpdateStateUI;
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
    private void UpdateStateUI(GameManager.GameState prev, GameManager.GameState current)
    {
        _gameStateText.text = current.ToString();

        switch (current)
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
}
