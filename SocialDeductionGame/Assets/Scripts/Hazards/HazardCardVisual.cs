using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HazardCardVisual : MonoBehaviour
{
    // ================== Refrences ==================
    [Header("variables")]
    [SerializeField] private Sprite _cardBackLow;
    [SerializeField] private Sprite _cardBackMed;
    [SerializeField] private Sprite _cardBackHigh;
    [SerializeField] private Sprite _cardTitleLow;
    [SerializeField] private Sprite _cardTitleMed;
    [SerializeField] private Sprite _cardTitleHigh;
    [SerializeField] private Sprite _cardDescLow;
    [SerializeField] private Sprite _cardDescMed;
    [SerializeField] private Sprite _cardDescHigh;
    [SerializeField] private Color _colorLow;
    [SerializeField] private Color _colorMed;
    [SerializeField] private Color _colorHigh;

    [Header("Refrences")]
    [SerializeField] private Image _hazardCardBack;
    [SerializeField] private Image _hazardCardTitle;
    [SerializeField] private Image _hazardCardDesc;
    [SerializeField] private GameObject _slash;
    [SerializeField] private TextMeshProUGUI _hazardTitle;
    [SerializeField] private TextMeshProUGUI _hazardConsequences;
    private int _heldHazardID;
    private Hazard _hazardData;

    public delegate void HazardVisualEvent();
    public static event HazardVisualEvent OnHazardActivated;

    // ================== Setup ==================
    public void Setup(int hazardID)
    {
        _heldHazardID = hazardID;
        _hazardData = CardDatabase.Instance.GetHazard(hazardID);
        _hazardTitle.text = _hazardData.GetHazardName();
        _hazardConsequences.text = _hazardData.GetHazardConsequences();
        Hazard.DangerLevel dangerLevel = _hazardData.GetHazardDangerLevel();

        if (dangerLevel == Hazard.DangerLevel.Low)
        {
            _hazardCardBack.sprite = _cardBackLow;
            _hazardCardTitle.sprite = _cardTitleLow;
            _hazardCardDesc.sprite = _cardDescLow;
            //_hazardTitle.color = _colorLow;
        }
        else if (dangerLevel == Hazard.DangerLevel.Medium)
        {
            _hazardCardBack.sprite = _cardBackMed;
            _hazardCardTitle.sprite = _cardTitleMed;
            _hazardCardDesc.sprite = _cardDescMed;
            //_hazardTitle.color = _colorMed;
        }
        else if (dangerLevel == Hazard.DangerLevel.High)
        {
            _hazardCardBack.sprite = _cardBackHigh;
            _hazardCardTitle.sprite = _cardTitleHigh;
            _hazardCardDesc.sprite = _cardDescHigh;
            //_hazardTitle.color = _colorHigh;
        }
    }

    public int GetHazardID()
    {
        return _heldHazardID;
    }

    public void RunHazard(HandManager handMan)
    {
        if (!_hazardData.RunHazard(handMan))
        {
            // Hazard card prevented
            _hazardCardBack.color = Color.grey;
            _slash.SetActive(true);
        }
        else
        {
            // Hazard card happened
            OnHazardActivated?.Invoke();
        }
    }
}
