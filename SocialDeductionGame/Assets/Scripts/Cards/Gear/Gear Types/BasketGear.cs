using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasketGear : Gear
{
    // ========== Basket Details ==========
    [Header("Basket Stats")]
    [SerializeField] private int _inventorySpace;
    private PlayerCardManager _pcm;

    // ========== Override Functions ==========
    public override void OnEquip()
    {
        if(!_pcm)
            _pcm = gameObject.GetComponentInParent<PlayerCardManager>();

        _pcm.IncrementPlayerHandSize(_inventorySpace);

        Debug.Log($"CARD {GetCardName()} EQUIPPED");
    }

    public override void OnUnquip()
    {
        if (!_pcm)
            _pcm = gameObject.GetComponentInParent<PlayerCardManager>();

        _pcm.IncrementPlayerHandSize(-_inventorySpace);

        Debug.Log($"CARD {GetCardName()} UNEQUIPPED");
    }
}
