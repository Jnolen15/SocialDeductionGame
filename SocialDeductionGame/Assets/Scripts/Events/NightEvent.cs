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
    [SerializeField] private string _eventConsequences;
    [TextArea]
    [SerializeField] private string _eventBonuses;
    [SerializeField] private Sprite _eventArt;

    [Header("Required Resources")]
    [SerializeField] private List<CardTag> _requiredCardTags = new();
    [Header("Requirement = Celing(#players*this)")]
    [SerializeField] private float _requirementMod;
    [Header("Requirement =  Celing(#players/this)")]
    [SerializeField] private float _bonusMod;

    [Header("Bonus")]
    [SerializeField] private EventBonus _eventBonus;


    // ========== Getters ==========
    public int GetEventID()
    {
        return _eventID;
    }

    public string GetEventName()
    {
        return _eventName;
    }

    public string GetEventConsequences()
    {
        return _eventConsequences;
    }

    public string GetEventBonuses()
    {
        return _eventBonuses;
    }

    public int GetSuccessPoints(int numPlayers)
    {
        return SPCalculation(numPlayers);
    }

    public List<CardTag> GetRequiredCardTags()
    {
        return _requiredCardTags;
    }

    // ========== OVERRIDE CLASSES ==========
    // Calculates the SuccessPoints needed to prevent the event consequences
    public virtual int SPCalculation(int numPlayers)
    {
        int num = Mathf.CeilToInt(numPlayers * _requirementMod);

        if (num <= 1)
            num = 1;

        return num;
    }

    // Calculates the SuccessPoints needed to achive the event bonus
    public virtual int SPBonusCalculation(int numPlayers)
    {
        int num = Mathf.CeilToInt(numPlayers / _bonusMod);

        if (num <= 1)
            num = 1;

        return num;
    }

    // The gameplay consecqunces of the event
    public abstract void InvokeEvent();

    // The gameplay Bonuses of the event
    public virtual void InvokeBonus()
    {
        Debug.Log("Invoking Event Bonus");
        _eventBonus.InvokeBonus();
    }
}
