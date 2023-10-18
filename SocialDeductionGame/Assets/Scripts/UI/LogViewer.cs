using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LogViewer : MonoBehaviour
{
    [SerializeField] private Transform _logPannel;
    [SerializeField] private Transform _messageZone;
    [SerializeField] private GameObject _logMessagePref;
    [SerializeField] private Transform _hidden;
    [SerializeField] private Transform _shown;
    private bool _isShown;

    [SerializeField] private GameObject _localTestModeButton;
    [SerializeField] private GameObject _dontTestWinButton;
    [SerializeField] private GameObject _doCheatsButton;

    [Header("Cheats")]
    [SerializeField] private bool _localTestMode;
    [SerializeField] private bool _dontTestWin;
    [SerializeField] private bool _doCheats;

    // ============== Singleton pattern ==============
    #region Singleton
    public static LogViewer Instance { get; private set; }
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        DontDestroyOnLoad(gameObject);
    }
    #endregion

    // ==================== Setup ====================
    #region Setup
    private void Awake()
    {
        InitializeSingleton();
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;

        UpdateDontTestWinButton();
        UpdateDoCheatsButton();
        UpdateLocalTestModeButton();
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
    #endregion

    // ==================== Update ====================
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            ToggleVisible();
        }
    }

    // ==================== Function ====================
    #region Function
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        GameObject logMessage =  Instantiate(_logMessagePref, _messageZone);

        TextMeshProUGUI logText = logMessage.GetComponentInChildren<TextMeshProUGUI>();
        logText.text = logString;
    }

    public void ToggleVisible()
    {
        _isShown = !_isShown;

        if (!_isShown)
            _logPannel.transform.position = _hidden.position;
        else
            _logPannel.transform.position = _shown.position;
    }

    // Cheat buttons
    public void ToggleDontTestWin()
    {
        _dontTestWin = !_dontTestWin;
        UpdateDontTestWinButton();
    }

    public void UpdateDontTestWinButton()
    {
        if (_dontTestWin)
            _dontTestWinButton.GetComponent<Image>().color = Color.green;
        else
            _dontTestWinButton.GetComponent<Image>().color = Color.red;
    }

    public void ToggleDoCheats()
    {
        _doCheats = !_doCheats;
        UpdateDoCheatsButton();
    }

    public void UpdateDoCheatsButton()
    {
        if (_doCheats)
            _doCheatsButton.GetComponent<Image>().color = Color.green;
        else
            _doCheatsButton.GetComponent<Image>().color = Color.red;
    }

    public void ToggleLocalTestMode()
    {
        _localTestMode = !_localTestMode;
        UpdateLocalTestModeButton();
    }

    public void UpdateLocalTestModeButton()
    {
        if (_localTestMode)
            _localTestModeButton.GetComponent<Image>().color = Color.green;
        else
            _localTestModeButton.GetComponent<Image>().color = Color.red;
    }
    #endregion

    // ==================== Script Interaction ====================
    public bool GetTestForWin()
    {
        return _dontTestWin;
    }

    public bool GetDoCheats()
    {
        return _doCheats;
    }

    public bool GetLocalTestMode()
    {
        return _localTestMode;
    }
}
