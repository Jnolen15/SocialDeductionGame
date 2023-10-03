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
        if(_dangerLevel == DangerLevel.High)
        {
            Debug.Log("High danger! Unpreventable hazard!");
            return false;
        }

        // Tests to see if can be prevented by player gear
        int gearID = 0;
        if (_preventionTag != null)
        {
            gearID = handMan.CheckGearTagsFor(_preventionTag);
            if (gearID != 0)
            {
                handMan.UseGear(gearID);
                return true;
            }
        }

        // Tests to see if can be prevented with Talisman of protection
        gearID = handMan.CheckGearTagsFor("Protection");
        if (gearID != 0)
        {
            // 1/3 chance success
            int rand = Random.Range(1, 4);
            Debug.Log("Rolling for talisman of protection: " + rand);
            if(rand == 1)
            {
                Debug.Log("Roll == 1. Success!");
                handMan.UseGear(gearID);
                return true;
            }
        }

        return false;
    }

    // The gameplay consecqunces of the hazard
    public abstract void InvokeHazardConsequence();
}
