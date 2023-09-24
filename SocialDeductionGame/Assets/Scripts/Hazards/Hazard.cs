using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Hazard : ScriptableObject
{
    [Header("Hazard ID")]
    [SerializeField] private int _hazardID;

    [Header("Hazard Details")]
    [SerializeField] private string _hazardName;
    [TextArea]
    [SerializeField] private string _hazardConsequences;
    [SerializeField] private Sprite _hazardArt;
    public enum HazardType
    {
        Animal,
        Environmental
    }
    [SerializeField] private HazardType _hazardType;
    public enum DangerLevel
    {
        Low,
        Medium,
        High
    }
    [SerializeField] private DangerLevel _dangerLevel;


    // ========== Getters ==========
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

    // ========== OVERRIDE CLASSES ==========
    public virtual void RunHazard()
    {
        if (!TestForPrevention())
        {
            Debug.Log("<color=red>HAZARD: </color>Not prevented, invoking consequence");
            InvokeHazardConsequence();
        }
        else
            Debug.Log("<color=red>HAZARD: </color>Prevented, nothing happen");
    }

    public virtual bool TestForPrevention()
    {
        // TODO
        // Tests to see if can be prevented by player gear
        return false;
    }

    // The gameplay consecqunces of the hazard
    public abstract void InvokeHazardConsequence();
}
