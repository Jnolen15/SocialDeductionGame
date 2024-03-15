using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSlotUI : MonoBehaviour
{
    // =================== Refrences ===================
    public Card HeldCard;

    // ================ Card ================
    public void SlotCard(Card newCard)
    {
        HeldCard = newCard;
    }

    public void RemoveCard()
    {
        Destroy(HeldCard.gameObject);
        Destroy(gameObject);
    }

    //================ Helpers ================
    public Card GetCard()
    {
        return HeldCard;
    }

    public bool HasCard()
    {
        if (HeldCard)
            return true;

        return false;
    }
}
