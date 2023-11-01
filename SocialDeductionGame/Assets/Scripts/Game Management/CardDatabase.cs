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
        public string CardName = "Card Name";
        public GameObject CardObj = null;
    }

    // Automatically fills IDs for all entires with a gameobject
    [Button("Fill IDs in card list")]
    private void FillCardIDs()
    {
        Debug.Log("Filling Card IDs");
        foreach (CardEntry entry in _globalCardList)
        {
            Card card = entry.CardObj.GetComponent<Card>();
            entry.CardID = card.GetCardID();
            entry.CardName = card.GetCardName();
            Debug.Log("Setting Card ID " + entry.CardObj.GetComponent<Card>().GetCardID());
        }
    }

    // ===== Card Functions =====
    public bool VerifyCard(int cardID)
    {
        foreach (CardEntry card in _globalCardList)
        {
            if (card.CardID == cardID)
                return true;
        }

        Debug.LogError($"Card with ID:{cardID} not found in global card list.");
        return false;
    }

    public GameObject GetCard(int cardID)
    {
        foreach (CardEntry card in _globalCardList)
        {
            if (card.CardID == cardID)
                return card.CardObj;
        }

        Debug.LogError($"Card with ID:{cardID} not found in global card list.");
        return null;
    }

    public string GetCardName(int cardID)
    {
        foreach (CardEntry card in _globalCardList)
        {
            if (card.CardID == cardID)
                return card.CardName;
        }

        Debug.LogError($"Card with ID:{cardID} not found in global card list.");
        return null;
    }

    // FOR TESTING: Get a random card from all cards in the DB
    public int DrawCard()
    {
        return _globalCardList[Random.Range(0, _globalCardList.Count)].CardID;
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
        foreach (EventEntry entry in _globalEventList)
        {
            entry.EventID = entry.EventSO.GetEventID();
            Debug.Log("Setting Event ID " + entry.EventSO.GetEventID());
        }
    }

    //  ===== Event Functions =====
    public bool ContainsEvent(int eventID)
    {
        foreach (EventEntry entry in _globalEventList)
        {
            if (entry.EventID == eventID)
                return true;
        }

        return false;
    }

    public NightEvent GetEvent(int eventID)
    {
        foreach (EventEntry entry in _globalEventList)
        {
            if (entry.EventID == eventID)
                return entry.EventSO;
        }

        Debug.LogError($"Night Event with ID:{eventID} not found in global event list.");
        return null;
    }

    // Gets 1 random event
    public int GetRandEvent(int prevEvent = 0)
    {
        int newEvent = _globalEventList[Random.Range(0, _globalEventList.Count)].EventID;
        int breakoutNum = 0;
        while (newEvent == prevEvent)
        {
            newEvent = _globalEventList[Random.Range(0, _globalEventList.Count)].EventID;

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
    public List<int> GetRandEvents(int num, int prevEvent = 0)
    {
        if (num > _globalEventList.Count)
        {
            Debug.LogError("GetRandEvents: Number of entries to pick exceeds the size of the list");
            return null;
        }

        List<int> pickedEvents = new();
        List<EventEntry> eventListCopy = new();
        eventListCopy.AddRange(_globalEventList);

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

    // ============== Hazards ==============
    #region Hazards
    //  ===== Global Hazard List =====
    [SerializeField] private List<HazardEntry> _globalHazardList = new List<HazardEntry>();
    [System.Serializable]
    public class HazardEntry
    {
        public int HazardID = 0;
        public Hazard HazardSO = null;
    }

    // Automatically fills IDs for all entires with a SO
    [Button("Fill IDs in hazard list")]
    private void FillHazardIDs()
    {
        Debug.Log("Filling Hazard IDs");
        foreach (HazardEntry entry in _globalHazardList)
        {
            entry.HazardID = entry.HazardSO.GetHazardID();
            Debug.Log("Setting Hazard ID " + entry.HazardSO.GetHazardID());
        }
    }

    //  ===== Hazard Functions =====
    public Hazard GetHazard(int hazardID)
    {
        foreach (HazardEntry entry in _globalHazardList)
        {
            if (entry.HazardID == hazardID)
                return entry.HazardSO;
        }

        Debug.LogError($"Hazard with ID:{hazardID} not found in global event list.");
        return null;
    }

    // Gets 1 random hazard
    public int GetRandHazard(Hazard.DangerLevel dangerLevel)
    {
        // FOR NOW JUST REROLLS UNTIL CORRENT TEIR. BUT IN FUTURE HAVE MULTIPLE LISTS? AUTO SORTED ON START
        HazardEntry newHazard = _globalHazardList[Random.Range(0, _globalHazardList.Count)];
        int breakoutNum = 0;
        while (newHazard.HazardSO.GetHazardDangerLevel() != dangerLevel)
        {
            newHazard = _globalHazardList[Random.Range(0, _globalHazardList.Count)];

            // Emergency breakout
            breakoutNum++;
            if (breakoutNum > 100)
            {
                Debug.Log("Breakout of While triggered");
                break;
            }
        }

        return newHazard.HazardID;
    }
    #endregion
}
