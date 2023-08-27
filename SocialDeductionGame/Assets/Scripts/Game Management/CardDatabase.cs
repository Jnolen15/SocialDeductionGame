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
    //  ===== Global Event List =====
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

    // Gets 1 random event
    public static int GetRandEvent(int prevEvent = 0)
    {
        int newEvent = Instance._globalEventList[Random.Range(0, Instance._globalEventList.Count)].EventID;
        int breakoutNum = 0;
        while (newEvent == prevEvent)
        {
            newEvent = Instance._globalEventList[Random.Range(0, Instance._globalEventList.Count)].EventID;

            // Emergency breakout
            breakoutNum++;
            if (breakoutNum > 100)
            {
                Debug.Log("Breakout of While triggered");
                break;
            }
        }

        return newEvent;
    }

    // Returns multiple random events, avoiding duplicates
    public static List<int> GetRandEvents(int num, int prevEvent = 0)
    {
        if (num > Instance._globalEventList.Count)
        {
            Debug.LogError("GetRandEvents: Number of entries to pick exceeds the size of the list");
            return null;
        }

        List<int> pickedEvents = new();
        List<EventEntry> eventListCopy = new();
        eventListCopy.AddRange(Instance._globalEventList);

        for (int i = 0; i < num; i++)
        {
            // Pick entry from copy list
            EventEntry randEvent = eventListCopy[Random.Range(0, eventListCopy.Count)];

            // Test if matches prevEvent
            Debug.Log($"Testing to see if picked matches previous Rand: {randEvent.EventID} Prev: {prevEvent}");
            if(randEvent.EventID != prevEvent)
            {
                Debug.Log("Did not match, adding");
                // Then add that to return list
                pickedEvents.Add(randEvent.EventID);
            }
            else
            {
                Debug.Log("Did match, i--");
                i--;
            }

            // Then remove that entry from the copy list
            eventListCopy.Remove(randEvent);
        }

        return pickedEvents;
    }
    #endregion
}
