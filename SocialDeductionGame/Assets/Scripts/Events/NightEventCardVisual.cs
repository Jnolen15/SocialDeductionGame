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

    // ================== Setup ==================
    public void Setup(int eventID)
    {
        _eventTitle.text = CardDatabase.GetEvent(eventID).GetEventName();
        _eventRequiredNum.text = "At least " + CardDatabase.GetEvent(eventID).GetSuccessPoints(PlayerConnectionManager.GetNumConnectedPlayers());
        string cardTypes = "";
        foreach (Card.CardSubType cardType in CardDatabase.GetEvent(eventID).GetCardTypes())
        {
            cardTypes += cardType + " ";
        }
        _eventRequiredTypes.text = cardTypes;
        _eventDescription.text = CardDatabase.GetEvent(eventID).GetEventDescription();
    }
}
