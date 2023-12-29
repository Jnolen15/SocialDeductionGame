using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalismanGear : Gear
{
    // ========== Talisman Details ==========
    [Header("Talisman Stats")]
    [SerializeField] private int _givenCardID;
    [SerializeField] private int _odds;
    [SerializeField] private string _nightRecapMessage;

    private CardManager _cardManager;

    public delegate void TalismanEvent(string message);
    public static event TalismanEvent OnGiveCard;

    // ========== Talisman Functions ==========
    private void GiveFood()
    {
        if (_cardManager != null)
        {
            int rand = Random.Range(1, _odds);

            if(rand == 1)
            {
                Debug.Log(GetCardName() + " giving card");
                _cardManager.GiveCard(_givenCardID);
                OnGiveCard?.Invoke(_nightRecapMessage);
            }
        }
        else
            Debug.LogError("Talisman does not have card manager refrence");
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
