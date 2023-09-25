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
    [SerializeField] protected Sprite _hazardArt;
    public enum HazardType
    {
        Animal,
        Environmental
    }
    [SerializeField] protected HazardType _hazardType;
    public enum DangerLevel
    {
        Low,
        Medium,
        High
    }
    [SerializeField] protected DangerLevel _dangerLevel;
    [SerializeField] protected CardTag _preventionTag;


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

    public string GetHazardConsequences()
    {
        return _hazardConsequences;
    }
    
    public HazardType GetHazardType()
    {
        return _hazardType;
    }
    
    public DangerLevel GetHazardDangerLevel()
    {
        return _dangerLevel;
    }
    #endregion

    // ========== OVERRIDE CLASSES ==========
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
        if(_preventionTag == null)
        {
            Debug.Log("Unpreventable hazard!");
            return false;
        }

        // Tests to see if can be prevented by player gear
        int gearID = handMan.CheckGearTagsFor(_preventionTag);
        if (gearID != 0)
        {
            handMan.UseGear(gearID);
            return true;
        }

        return false;
    }

    // The gameplay consecqunces of the hazard
    public abstract void InvokeHazardConsequence();
}
