using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Night Event/Card Bonus")]
public class CardBonus : EventBonus
{
    [Header("CardID")]
    [SerializeField] private int _cardID;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeBonus()
    {
        CardManager cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();

        if (cardManager == null)
        {
            Debug.LogError("Cannot enact night event. Card Manager not found!");
            return;
        }

        cardManager.GiveCard(_cardID);
    }
}
