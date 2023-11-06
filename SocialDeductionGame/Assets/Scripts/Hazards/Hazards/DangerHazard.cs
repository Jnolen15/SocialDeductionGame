using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hazard/Danger Hazard")]
public class DangerHazard : Hazard
{
    [Header("Danger Stats")]
    [SerializeField] private int _danger;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeHazardConsequence()
    {
        GameObject LocationManObj = GameObject.FindGameObjectWithTag("GameManager");

        if (LocationManObj != null)
        {
            //if (_danger != 0)
            //    playerObj.GetComponent<PlayerData>().ModifyDangerLevel(_danger);
            Debug.LogError("Danger hazard not currently used!");
        }
        else
            Debug.LogError("Player object not found!");
    }
}
