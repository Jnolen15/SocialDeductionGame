using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NightEventCardVisual : MonoBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private TextMeshProUGUI _eventTitle;
    [SerializeField] private TextMeshProUGUI _eventRequiredNum;
    [SerializeField] private TextMeshProUGUI _eventRequiredTypes;
    [SerializeField] private TextMeshProUGUI _eventDescription;
    private int _heldEventID;

    // ================== Setup ==================
    public void Setup(int eventID)
    {
        _heldEventID = eventID;

        _eventTitle.text = CardDatabase.GetEvent(eventID).GetEventName();
        _eventRequiredNum.text = "At least " + CardDatabase.GetEvent(eventID).GetSuccessPoints(PlayerConnectionManager.GetNumConnectedPlayers());
        _eventDescription.text = CardDatabase.GetEvent(eventID).GetEventDescription();
        List<string> reqTags = new();
        foreach (CardTag t in CardDatabase.GetEvent(eventID).GetRequiredCardTags())
            reqTags.Add(t.Name);
        _eventRequiredTypes.text = string.Join(", ", reqTags);
    }

    public int GetEventID()
    {
        return _heldEventID;
    }
}
