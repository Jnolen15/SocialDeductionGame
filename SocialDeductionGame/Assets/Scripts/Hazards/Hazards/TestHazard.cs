using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hazard/Test Hazard")]
public class TestHazard : Hazard
{
    // ========== METHOD OVERRIDES ==========
    public override void InvokeHazardConsequence()
    {
        Debug.Log("<color=red>HAZARD ACTIVATE</color>");
    }
}
