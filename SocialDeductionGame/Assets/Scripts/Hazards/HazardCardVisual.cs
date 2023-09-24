using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HazardCardVisual : MonoBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private TextMeshProUGUI _hazardTitle;
    [SerializeField] private TextMeshProUGUI _hazardConsequences;
    [SerializeField] private TextMeshProUGUI _hazardType;
    [SerializeField] private TextMeshProUGUI _hazardDangerLevel;
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
        _hazardDangerLevel.text = dangerLevel.ToString();
        if (dangerLevel == Hazard.DangerLevel.Low)
            _hazardDangerLevel.color = Color.green;
        else if (dangerLevel == Hazard.DangerLevel.Medium)
            _hazardDangerLevel.color = Color.yellow;
        else if (dangerLevel == Hazard.DangerLevel.High)
            _hazardDangerLevel.color = Color.red;
    }

    public int GetHazardID()
    {
        return _heldHazardID;
    }

    public void RunHazard()
    {
        _hazardData.RunHazard();
    }
}
