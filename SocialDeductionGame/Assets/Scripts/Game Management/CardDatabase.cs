using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    // Singleton pattern
    #region Singleton
    public static CardDatabase Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;
    }
    #endregion

    // Variables
    [SerializeField] private List<CardEntry> _globalCardList = new List<CardEntry>();

    [System.Serializable]
    public class CardEntry
    {
        public int cardID = 0;
        public GameObject cardObj = null;
    }

    // Functions
    public static GameObject GetCard(int cardID)
    {
        foreach(CardEntry card in Instance._globalCardList)
        {
            if (card.cardID == cardID)
                return card.cardObj;
        }

        Debug.LogError($"Card with ID:{cardID} not found in global card list.");
        return null;
    }

    // FOR TESTING: Get a random card from all cards in the DB
    public static int DrawCard()
    {
        return Instance._globalCardList[Random.Range(0, Instance._globalCardList.Count)].cardID;
    }
}
