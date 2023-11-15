using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hazard/Damage Hazard")]
public class DamageHazard : Hazard
{
    [Header("Damage Stats")]
    [SerializeField] private int _damage;
    [SerializeField] private int _hunger;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeHazardConsequence()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            if(_damage != 0)
                playerObj.GetComponent<PlayerHealth>().ModifyHealth(-_damage);
            if (_hunger != 0)
                playerObj.GetComponent<PlayerHealth>().ModifyHunger(-_hunger);
        }
        else
            Debug.LogError("Player object not found!");
    }
}
