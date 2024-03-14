using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Hazard : ScriptableObject
{
    [Header("Hazard ID")]
    [SerializeField] protected int _hazardID;

    [Header("Hazard Details")]
    [SerializeField] protected string _hazardName;
    [TextArea]
    [SerializeField] protected string _hazardConsequences;
    [SerializeField] protected List<KeywordSO> _hazardKeywords;
    [SerializeField] protected Sprite _hazardArt;
    public enum DangerLevel
    {
        Low,
        Medium,
        High
    }
    [SerializeField] protected DangerLevel _dangerLevel;
    [SerializeField] protected bool _preventable;


    // ========== Getters ==========
    #region Getters
    public int GetHazardID()
    {
        return _hazardID;
    }

    public string GetHazardName()
    {
        return _hazardName;
    }

    public Sprite GetHazardArt()
    {
        return _hazardArt;
    }

    public string GetHazardConsequences()
    {
        return _hazardConsequences;
    }
    
    public List<KeywordSO> GetHazardKeywords()
    {
        return _hazardKeywords;
    }
    
    public DangerLevel GetHazardDangerLevel()
    {
        return _dangerLevel;
    }
    #endregion

    // ========== OVERRIDE CLASSES ==========
    #region Override Classes
    public virtual bool RunHazard(HandManager handMan)
    {
        if (!TestForPrevention(handMan))
        {
            Debug.Log("<color=red>HAZARD: </color>Not prevented, invoking consequence");
            InvokeHazardConsequence();
            return true;
        }

        Debug.Log("<color=red>HAZARD: </color>Prevented, nothing happen");
        return false;
    }

    public virtual bool TestForPrevention(HandManager handMan)
    {
        if(_dangerLevel == DangerLevel.High)
        {
            Debug.Log("High danger! Unpreventable hazard!");
            return false;
        }

        // Tests to see if can be prevented by player gear
        int gearID = 0;
        if (_preventable)
        {
            gearID = handMan.CheckGearTagsFor("Weapon");
            if (gearID != 0)
            {
                handMan.UseGear(gearID);
                return true;
            }
        }
        return false;
    }

    // The gameplay consecqunces of the hazard
    public abstract void InvokeHazardConsequence();
    #endregion
}
