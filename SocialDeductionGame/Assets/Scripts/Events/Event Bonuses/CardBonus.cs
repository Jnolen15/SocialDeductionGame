using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Night Event/Card Bonus")]
public class CardBonus : EventBonus
{
    [Header("CardID")]
    [SerializeField] private int _cardID;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeBonus(GameObject player = null)
    {
        Debug.LogWarning("CardBonus envoked, but is non-functional!");
        // Since bonuses have been changed to invoked on the server
        // This no longer workds. Hwoever it is un-used
        // If it is to be used again, likely have to make CardManager a network object
        /*CardManager cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();

        if (cardManager == null)
        {
            Debug.LogError("Cannot enact night event. Card Manager not found!");
            return;
        }

        cardManager.GiveCard(_cardID);*/
    }
}
