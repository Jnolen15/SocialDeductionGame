using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponGear : Gear
{
    // ========== Weapon Details ==========

    // ========== Weapon Functions ==========
    public override void OnUse()
    {
        int rand = Random.Range(1, 4);
        Debug.Log($"Gear used, rolling for break: {rand}");
        if (rand == 1)
        {
            Debug.Log($"Gear {_cardName} has broken!");
            GetComponentInParent<GearSlot>().GearBreak(_cardID);
        }
    }
}
