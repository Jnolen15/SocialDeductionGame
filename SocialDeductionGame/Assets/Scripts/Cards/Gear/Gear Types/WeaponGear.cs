using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponGear : Gear
{
    // ========== Weapon Details ==========
    [Header("Weapon Stats")]
    [SerializeField] private int _durability;
    private GearDurabilityVisual _durabilityVisual;

    // ========== Weapon Functions ==========
    public int GetDurability()
    {
        return _durability;
    }

    public void LowerDurability()
    {
        _durability--;

        if(_durability <= 0)
        {
            Debug.Log($"Gear {_cardName} has broken!");
            GetComponentInParent<GearSlot>().Unequip(_cardID);
        }

        if (_durabilityVisual)
            _durabilityVisual.UpdateDurability(_durability);
        else
            Debug.LogError("_durabilityVisual not assinged!");
    }

    // ========== Override Functions ==========
    // Playable card for in the playerss hand
    public override void SetupPlayable()
    {
        GameObject cardVisual = Instantiate(_cardPlayablePrefab, transform);
        cardVisual.GetComponent<CardVisual>().Setup(_cardName, _cardDescription, _tags);
        _durabilityVisual = cardVisual.GetComponent<GearDurabilityVisual>();
        _durabilityVisual.Setup(true, _durability);
    }

    // Selectable card for foraging
    public override void SetupSelectable()
    {
        GameObject cardVisual = Instantiate(_cardSelectablePrefab, transform);
        cardVisual.GetComponent<CardVisual>().Setup(_cardName, _cardDescription, _tags);
        cardVisual.GetComponent<GearDurabilityVisual>().Setup(true, _durability);
    }

    // Visual card for non-interactable UI
    public override void SetupUI()
    {
        GameObject cardVisual = Instantiate(_cardUIPrefab, transform);
        cardVisual.GetComponent<CardVisual>().Setup(_cardName, _cardDescription, _tags);
        cardVisual.GetComponent<GearDurabilityVisual>().Setup(true, _durability);
    }

    public override void OnUse()
    {
        LowerDurability();
        Debug.Log($"WEAPON CARD {GetCardName()} USED. DURABILITY NOW {GetDurability()}");
    }
}
