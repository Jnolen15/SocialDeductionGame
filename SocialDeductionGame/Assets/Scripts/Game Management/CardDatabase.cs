using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    [SerializeField] private List<CardEntry> _globalCardList = new List<CardEntry>();

    [System.Serializable]
    public class CardEntry
    {
        public int cardID = 0;
        public GameObject cardObj = null;

        //public int GetCardID() { return cardID; }
        //public GameObject GetCardObj() { return cardObj; }
    }

    public GameObject GetCard(int cardID)
    {
        foreach(CardEntry card in _globalCardList)
        {
            if (card.cardID == cardID)
                return card.cardObj;
        }

        Debug.LogError($"Card with ID:{cardID} not found in global card list.");
        return null;
    }

    public int DrawCard()
    {
        return _globalCardList[Random.Range(0, _globalCardList.Count)].cardID;
    }
}
