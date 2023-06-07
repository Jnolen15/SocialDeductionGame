using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CardManager : NetworkBehaviour
{
    // Events
    public delegate void GainCards(int[] cardIDs);
    public static event GainCards OnCardsGained;

    public void GiveCards(int[] cardIDs)
    {
        Debug.Log("Cards Given");
        OnCardsGained(cardIDs);
    }
}
