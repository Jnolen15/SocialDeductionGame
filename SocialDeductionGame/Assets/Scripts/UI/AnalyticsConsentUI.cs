using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnalyticsConsentUI : MonoBehaviour
{
    // ============= Refrences =============
    [SerializeField] private GameObject _pannel;
    [SerializeField] private CanvasGroup _buttons;
    bool _pannelActive;
    bool _buttonsActive;
    float _buttonDelay = 1f;

    // ============= Function =============
    private void Start()
    {
        if (PlayerPrefs.GetInt("ShownConsentUI") == 0)
        {
            _pannel.SetActive(true);
            _pannelActive = true;
        }
    }

    private void Update()
    {
        if (!_pannelActive || _buttonsActive) return;

        if (_buttonDelay > 0f)
            _buttonDelay -= Time.deltaTime;
        else
        {
            _buttons.alpha = 1;
            _buttons.interactable = true;
        }
    }

    public void Accept()
    {
        PlayerPrefs.SetInt("AnalyticsConsent", 1);
        PlayerPrefs.SetInt("ShownConsentUI", 1);
        _pannel.SetActive(false);
    }

    public void Deny()
    {
        PlayerPrefs.SetInt("AnalyticsConsent", 0);
        PlayerPrefs.SetInt("ShownConsentUI", 1);
        _pannel.SetActive(false);
    }
}
