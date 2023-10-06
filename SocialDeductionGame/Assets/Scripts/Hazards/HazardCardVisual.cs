using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HazardCardVisual : MonoBehaviour
{
    // ================== Refrences ==================
    [Header("variables")]
    [SerializeField] private Sprite _cardLow;
    [SerializeField] private Sprite _cardMed;
    [SerializeField] private Sprite _cardHigh;
    [SerializeField] private Color _colorLow;
    [SerializeField] private Color _colorMed;
    [SerializeField] private Color _colorHigh;

    [Header("Refrences")]
    [SerializeField] private Image _hazardCard;
    [SerializeField] private GameObject _slash;
    [SerializeField] private TextMeshProUGUI _hazardTitle;
    [SerializeField] private TextMeshProUGUI _hazardConsequences;
    [SerializeField] private TextMeshProUGUI _hazardType;
    private int _heldHazardID;
    private Hazard _hazardData;

    // ================== Setup ==================
    public void Setup(int hazardID)
    {
        _heldHazardID = hazardID;
        _hazardData = CardDatabase.Instance.GetHazard(hazardID);
        _hazardTitle.text = _hazardData.GetHazardName();
        _hazardConsequences.text = _hazardData.GetHazardConsequences();
        _hazardType.text = _hazardData.GetHazardType().ToString();
        Hazard.DangerLevel dangerLevel = _hazardData.GetHazardDangerLevel();

        if (dangerLevel == Hazard.DangerLevel.Low)
        {
            _hazardCard.sprite = _cardLow;
            _hazardTitle.color = _colorLow;
        }
        else if (dangerLevel == Hazard.DangerLevel.Medium)
        {
            _hazardCard.sprite = _cardMed;
            _hazardTitle.color = _colorMed;
        }
        else if (dangerLevel == Hazard.DangerLevel.High)
        {
            _hazardCard.sprite = _cardHigh;
            _hazardTitle.color = _colorHigh;
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
            // If this triggerrs the card was prevented
            _hazardCard.color = Color.grey;
            _slash.SetActive(true);
        }
    }
}
