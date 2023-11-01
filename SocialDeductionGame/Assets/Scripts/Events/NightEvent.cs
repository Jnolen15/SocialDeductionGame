using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NightEvent : ScriptableObject
{
    // =============== DATA ===============
    [Header("Night Event ID")]
    [SerializeField] private int _eventID;

    [Header("Night Event Details")]
    [SerializeField] private string _eventName;
    [TextArea]
    [SerializeField] private string _eventConsequences;
    [TextArea]
    [SerializeField] private string _eventBonuses;
    [SerializeField] private Sprite _eventArt;
    [SerializeField] private bool _serverInvoked;

    [Header("Required Resources")]
    [SerializeField] private EventRequirementsSO _requirements;

    [Header("Bonus")]
    [SerializeField] private EventBonus _eventBonus;


    // ========== Getters ==========
    #region Getters
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

    public bool GetEventIsServerInvoked()
    {
        return _serverInvoked;
    }

    public int GetBonusRequirements()
    {
        return 2;
    }

    public CardTag GetPrimaryResource()
    {
        return _requirements.GetPrimaryTag();
    }

    public CardTag GetSecondaryResource()
    {
        return _requirements.GetSecondaryTag();
    }

    public Vector2 GetRequirements(int numPlayers)
    {
        return _requirements.GetRequirements(numPlayers);
    }
    #endregion

    // ========== OVERRIDE Functions ==========
    // The gameplay consecqunces of the event
    public abstract void InvokeEvent();

    // The gameplay Bonuses of the event
    public virtual void InvokeBonus()
    {
        Debug.Log("Invoking Event Bonus");
        _eventBonus.InvokeBonus();
    }
}
