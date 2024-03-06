using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SaboteurShrineUI : MonoBehaviour
{
    // ================== Refrences / Variables ==================
    [SerializeField] private GameObject _shrineUI;
    [SerializeField] private Image _shrineLevelFill;
    [SerializeField] private TextMeshProUGUI _currentShrineLevel;
    [SerializeField] private TextMeshProUGUI _maxShrineLevel;
    [SerializeField] private List<GameObject> _sufferingIcons;
    [SerializeField] private GameObject _sacrificeHeader;
    [SerializeField] private GameObject _skullIcon;
    [SerializeField] private GameObject _deathMessage;

    // ================== Setup ==================
    #region Setup
    private void Start()
    {
        GameManager.OnStateMidnight += Show;
        GameManager.OnStateMorning += Hide;
        //SufferingManager.OnShrineLevelUp += UpdateShrineUI;
    }

    private void OnDisable()
    {
        GameManager.OnStateMidnight -= Show;
        GameManager.OnStateMorning -= Hide;
        //SufferingManager.OnShrineLevelUp -= UpdateShrineUI;
    }
    #endregion

    // ================== UI ==================
    #region UI
    private void Show()
    {
        //_shrineUI.SetActive(true);
    }

    private void Hide()
    {
        _shrineUI.SetActive(false);
    }

    private void UpdateShrineUI(int maxLevel, int newLevel, int numSuffering, bool deathReset)
    {
        _currentShrineLevel.text = newLevel.ToString();
        _maxShrineLevel.text = "of " + maxLevel;

        _shrineLevelFill.fillAmount = newLevel / (float)maxLevel;

        for (int i = 0; i < _sufferingIcons.Count; i++)
        {
            if (i < numSuffering)
                _sufferingIcons[i].SetActive(true);
            else
                _sufferingIcons[i].SetActive(false);
        }

        bool maxLevelReached = (maxLevel == newLevel);
        _sacrificeHeader.SetActive(maxLevelReached);
        _skullIcon.SetActive(maxLevelReached);

        _deathMessage.SetActive(deathReset);
    }
    #endregion
}
