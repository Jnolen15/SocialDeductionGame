using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LogViewer : MonoBehaviour
{
    [SerializeField] private Transform _logPannel;
    [SerializeField] private Transform _messageZone;
    [SerializeField] private GameObject _logMessagePref;
    [SerializeField] private Transform _hidden;
    [SerializeField] private Transform _shown;
    [SerializeField] private bool _isShown;

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        TextMeshProUGUI log =  Instantiate(_logMessagePref, _messageZone).GetComponent<TextMeshProUGUI>();
        log.transform.SetAsFirstSibling();

        log.text = logString;
    }

    public void ToggleVisible()
    {
        _isShown = !_isShown;

        if (!_isShown)
            _logPannel.transform.position = _hidden.position;
        else
            _logPannel.transform.position = _shown.position;
    }
}
