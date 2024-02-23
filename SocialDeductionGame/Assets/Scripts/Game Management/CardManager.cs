using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Sirenix.OdinInspector;

public class CardManager : MonoBehaviour
{
    // Acts as a middle man between various card giving sources and the player OR Forage Location
    // Sends an event when a card is recieved, player picks up on this event

    // ================== Player ==================
    #region Player
    // ~~~~~~~~ Events ~~~~~~~~
    public delegate void GainCards(int[] cardIDs);
    public static event GainCards OnCardsGained;

    public void GiveCards(int[] cardIDs)
    {
        Debug.Log("Cards Given");
        OnCardsGained?.Invoke(cardIDs);
    }

    public void GiveCard(int cardID)
    {
        int[] card = { cardID };

        OnCardsGained?.Invoke(card);
    }

    // ~~~~~~~~ Testing ~~~~~~~~
    [Button("Give player card")]
    private void GivePlayerCard(int cardID)
    {
        int[] card = { cardID };

        OnCardsGained?.Invoke(card);
    }
    #endregion

    // ================== Forage ==================
    #region Forage
    // ~~~~~~~~ Events ~~~~~~~~
    public delegate void InjectCardsAction(LocationManager.LocationName locationName, int cardID, int num);
    public static event InjectCardsAction OnInjectCards;

    // Should only be called from the server
    public void InjectCards(LocationManager.LocationName locationName, int cardID, int num)
    {
        OnInjectCards?.Invoke(locationName, cardID, num);
    }
    #endregion
}
