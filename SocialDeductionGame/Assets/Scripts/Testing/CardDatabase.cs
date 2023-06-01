using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    [SerializeField] private List<CardData> _globalCardList = new List<CardData>();

    public CardData GetCard(int cardID)
    {
        foreach(CardData card in _globalCardList)
        {
            if (card.CardID == cardID)
                return card;
        }

        Debug.LogError($"Card with ID:{cardID} not found in global card list.");
        return null;
    }

    public int DrawCard()
    {
        return _globalCardList[Random.Range(0, _globalCardList.Count)].CardID;
    }
}
