using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HazardCardVisual : MonoBehaviour, IKeywordKeeper
{
    // ================== Refrences ==================
    [Header("variables")]
    [SerializeField] private Color _colorLow;
    [SerializeField] private Color _colorMed;
    [SerializeField] private Color _colorHigh;

    [Header("Refrences")]
    [SerializeField] private Image _hazardCardBack;
    [SerializeField] private Image _hazardCardBackDetail;
    [SerializeField] private Image _hazardCardTopLable;
    [SerializeField] private Image _hazardCardBottomLable;
    [SerializeField] private Image _hazardArt;
    [SerializeField] private GameObject _slash;
    [SerializeField] private TextMeshProUGUI _hazardTitle;
    [SerializeField] private TextMeshProUGUI _hazardConsequences;
    private int _heldHazardID;
    private Hazard _hazardData;

    public delegate void HazardVisualEvent(Hazard.DangerLevel level);
    public static event HazardVisualEvent OnHazardActivated;

    // ================== Setup ==================
    #region Setup
    public void Setup(int hazardID)
    {
        _heldHazardID = hazardID;
        _hazardData = CardDatabase.Instance.GetHazard(hazardID);
        _hazardTitle.text = _hazardData.GetHazardName();
        _hazardConsequences.text = _hazardData.GetHazardConsequences();
        _hazardArt.sprite = _hazardData.GetHazardArt();
        Hazard.DangerLevel dangerLevel = _hazardData.GetHazardDangerLevel();

        if (dangerLevel == Hazard.DangerLevel.Low)
        {
            SetColor(_colorLow);
        }
        else if (dangerLevel == Hazard.DangerLevel.Medium)
        {
            SetColor(_colorMed);
        }
        else if (dangerLevel == Hazard.DangerLevel.High)
        {
            SetColor(_colorHigh);
        }
    }

    private void SetColor(Color color)
    {
        _hazardCardBack.color = color;
        _hazardCardBackDetail.color = color;
        _hazardCardTopLable.color = color;
        _hazardCardBottomLable.color = color;
    }
    #endregion

    // ================== Function ==================
    #region Function
    public int GetHazardID()
    {
        return _heldHazardID;
    }

    public void RunHazard(HandManager handMan)
    {
        if (!_hazardData.RunHazard(handMan))
        {
            // Hazard card prevented
            SetColor(Color.grey);
            _slash.SetActive(true);
        }
        else
        {
            // Hazard card happened
            OnHazardActivated?.Invoke(_hazardData.GetHazardDangerLevel());
        }
    }
    #endregion

    // ========== INTERFACE ==========
    public List<KeywordSO> GetKeywords()
    {
        return _hazardData.GetHazardKeywords();
    }
}
