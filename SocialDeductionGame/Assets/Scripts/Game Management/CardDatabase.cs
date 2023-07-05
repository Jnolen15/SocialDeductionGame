using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class CardDatabase : MonoBehaviour
{
    // ============== Singleton pattern ==============
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

    // ============== Cards ==============
    #region Cards
    //  ===== Global Card List =====
    [SerializeField] private List<CardEntry> _globalCardList = new List<CardEntry>();
    [System.Serializable]
    public class CardEntry
    {
        public int CardID = 0;
        public GameObject CardObj = null;
    }

    // Automatically fills IDs for all entires with a gameobject
    [Button("Fill IDs in card list")]
    private void FillCardIDs()
    {
        Debug.Log("Filling Card IDs");
        Instance = this;
        foreach (CardEntry entry in Instance._globalCardList)
        {
            entry.CardID = entry.CardObj.GetComponent<Card>().GetCardID();
            Debug.Log("Setting Card ID " + entry.CardObj.GetComponent<Card>().GetCardID());
        }
        Instance = null;
    }

    // ===== Card Functions =====
    public static GameObject GetCard(int cardID)
    {
        foreach (CardEntry card in Instance._globalCardList)
        {
            if (card.CardID == cardID)
                return card.CardObj;
        }

        Debug.LogError($"Card with ID:{cardID} not found in global card list.");
        return null;
    }

    // FOR TESTING: Get a random card from all cards in the DB
    public static int DrawCard()
    {
        return Instance._globalCardList[Random.Range(0, Instance._globalCardList.Count)].CardID;
    }
    #endregion


    // ============== Night Events ==============
    #region Night Events
    //  ===== Global Card List =====
    [SerializeField] private List<EventEntry> _globalEventList = new List<EventEntry>();
    [System.Serializable]
    public class EventEntry
    {
        public int EventID = 0;
        public NightEvent EventSO = null;
    }

    // Automatically fills IDs for all entires with a SO
    [Button("Fill IDs in event list")]
    private void FillEventIDs()
    {
        Debug.Log("Filling Event IDs");
        Instance = this;
        foreach (EventEntry entry in Instance._globalEventList)
        {
            entry.EventID = entry.EventSO.GetEventID();
            Debug.Log("Setting Event ID " + entry.EventSO.GetEventID());
        }
        Instance = null;
    }

    //  ===== Event Functions =====
    public static bool ContainsEvent(int eventID)
    {
        foreach (EventEntry entry in Instance._globalEventList)
        {
            if (entry.EventID == eventID)
                return true;
        }

        return false;
    }

    public static NightEvent GetEvent(int eventID)
    {
        foreach (EventEntry entry in Instance._globalEventList)
        {
            if (entry.EventID == eventID)
                return entry.EventSO;
        }

        Debug.LogError($"Night Event with ID:{eventID} not found in global event list.");
        return null;
    }

    // FOR TESTING: Get a random event from all events in the DB
    public static int GetRandEvent()
    {
        return Instance._globalEventList[Random.Range(0, Instance._globalEventList.Count)].EventID;
    }
    #endregion
}
