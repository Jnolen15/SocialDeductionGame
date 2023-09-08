using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NightEventCardVisual : MonoBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _eventTagIconPref;
    [SerializeField] private TextMeshProUGUI _eventTitle;
    [SerializeField] private TextMeshProUGUI _eventRequiredNum;
    [SerializeField] private TextMeshProUGUI _eventConsequences;
    [SerializeField] private TextMeshProUGUI _eventBonuses;
    [SerializeField] private Transform _eventTagIconSlot;
    private int _heldEventID;

    // ================== Setup ==================
    public void Setup(int eventID)
    {
        // Clear tags (in case of reused card assets)
        foreach (Transform t in _eventTagIconSlot)
        {
            if(t != _eventTagIconSlot.GetChild(0))
                Destroy(t.gameObject);
        }

        // Setup new
        _heldEventID = eventID;
        NightEvent eventData = CardDatabase.GetEvent(eventID);
        _eventTitle.text = eventData.GetEventName();
        _eventRequiredNum.text = eventData.GetSuccessPoints(PlayerConnectionManager.Instance.GetNumLivingPlayers()) + " = ";
        _eventConsequences.text = "Fail: " + eventData.GetEventConsequences();
        _eventBonuses.text = $"Bonus: Add {eventData.SPBonusCalculation(PlayerConnectionManager.Instance.GetNumConnectedPlayers())} additional cards to {eventData.GetEventBonuses()}";
        foreach (CardTag t in eventData.GetRequiredCardTags())
        {
            TagIcon icon = Instantiate(_eventTagIconPref, _eventTagIconSlot).GetComponent<TagIcon>();
            icon.SetupIcon(t.visual, t.name);
        }
    }

    public int GetEventID()
    {
        return _heldEventID;
    }
}
