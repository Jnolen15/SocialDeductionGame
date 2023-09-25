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
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            if (_danger != 0)
                playerObj.GetComponent<PlayerData>().ModifyDangerLevel(_danger);
        }
        else
            Debug.LogError("Player object not found!");
    }
}
