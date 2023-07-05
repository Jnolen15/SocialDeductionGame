using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CardManager : NetworkBehaviour
{
    // Acts as a middle man between various card giving sources and the player
    // Sends an event when a card is recieved, player picks up on this event

    // ================== Events ==================
    public delegate void GainCards(int[] cardIDs);
    public static event GainCards OnCardsGained;

    public void GiveCards(int[] cardIDs)
    {
        Debug.Log("Cards Given");
        OnCardsGained(cardIDs);
    }

    public void GiveCard(int cardID)
    {
        int[] card = { cardID };

        OnCardsGained(card);
    }
}
