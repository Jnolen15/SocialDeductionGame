using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NightEvent : ScriptableObject
{
    [Header("Night Event ID")]
    [SerializeField] private int _eventID;

    [Header("Night Event Details")]
    [SerializeField] private string _eventName;
    [TextArea]
    [SerializeField] private string _eventDescription;
    [SerializeField] private Sprite _eventArt;

    [Header("Required Resources")]
    [SerializeField] private List<Card.CardSubType> _requiredCardTypes = new();


    // ========== Getters ==========
    public int GetEventID()
    {
        return _eventID;
    }

    public string GetEventName()
    {
        return _eventName;
    }

    public string GetEventDescription()
    {
        return _eventDescription;
    }

    public int GetSuccessPoints(int numPlayers)
    {
        return SPCalculation(numPlayers);
    }

    public List<Card.CardSubType> GetCardTypes()
    {
        return _requiredCardTypes;
    }

    // ========== OVERRIDE CLASSES ==========
    // Calculates the SuccessPoints needed to prevent the event consequences
    public abstract int SPCalculation(int numPlayers);

    // The gameplay consecqunces of the event
    public abstract void InvokeEvent();
}
