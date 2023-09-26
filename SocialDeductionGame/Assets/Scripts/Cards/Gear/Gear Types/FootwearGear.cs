using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootwearGear : Gear
{
    // ========== Footwear Details ==========
    [Header("Shoe Stats")]
    [SerializeField] private int _movePointBonus;
    private PlayerData _pData;

    // ========== Override Functions ==========
    public override void OnEquip()
    {
        if (!_pData)
            _pData = gameObject.GetComponentInParent<PlayerData>();

        _pData.IncrementPlayerMaxMovement(_movePointBonus);

        Debug.Log($"CARD {GetCardName()} EQUIPPED");
    }

    public override void OnUnequip()
    {
        if (!_pData)
            _pData = gameObject.GetComponentInParent<PlayerData>();

        _pData.IncrementPlayerMaxMovement(-_movePointBonus);

        Debug.Log($"CARD {GetCardName()} UNEQUIPPED");
    }
}
