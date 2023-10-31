using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalismanTranquilityGear : Gear
{
    // ========== Talisman Details ==========
    [Header("Talisman Stats")]
    [SerializeField] private int _dangerReduction;

    private PlayerData _playerData;

    // ========== Talisman Functions ==========
    private void ReduceDanger()
    {
        Debug.Log("Talisman of Tranquility reducing danger");

        if (_playerData != null)
            Debug.Log("REPLACE EFFECT");//_playerData.ModifyDangerLevel(-_dangerReduction);
        else
            Debug.LogError("Talisman of Tranquility does not have player data refrence");
    }

    // ========== Override Functions ==========
    public override void OnEquip()
    {
        _playerData = gameObject.GetComponentInParent<PlayerData>();
        GameManager.OnStateMorning += ReduceDanger;

        Debug.Log($"CARD {GetCardName()} EQUIPPED");
    }

    public override void OnUnequip()
    {
        GameManager.OnStateMorning -= ReduceDanger;

        Debug.Log($"CARD {GetCardName()} UNEQUIPPED");
    }
}
