using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalismanNourishmentGear : Gear
{
    // ========== Talisman Details ==========
    [Header("Talisman Stats")]
    [SerializeField] private int _mealID;

    private CardManager _cardManager;

    public delegate void TalismanEvent();
    public static event TalismanEvent OnGiveMeal;

    // ========== Talisman Functions ==========
    private void GiveFood()
    {
        Debug.Log("Talisman of Nourishment giving daily meal");

        if (_cardManager != null)
        {
            _cardManager.GiveCard(_mealID);
            OnGiveMeal?.Invoke();
        }
        else
            Debug.LogError("Talisman of Nourishment does not have card manager refrence");
    }

    // ========== Override Functions ==========
    public override void OnEquip()
    {
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
        GameManager.OnStateNight += GiveFood;

        Debug.Log($"CARD {GetCardName()} EQUIPPED");
    }

    public override void OnUnequip()
    {
        GameManager.OnStateNight -= GiveFood;

        Debug.Log($"CARD {GetCardName()} UNEQUIPPED");
    }
}
