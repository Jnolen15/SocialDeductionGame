using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NightEventPicker : MonoBehaviour
{
    // ================== Refrences ==================
    private EventManager _eventManager;
    [SerializeField] private GameObject _eventSelectable;
    [SerializeField] private Transform _eventCardArea;

    // ================== Setup ==================
    void OnEnable()
    {
        if(_eventManager == null)
            _eventManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EventManager>();
    }

    // ================== Function ==================
    public void DealOptions(int prevDayEvent = 0)
    {
        List<int> randEvents = CardDatabase.GetRandEvents(3, prevDayEvent);

        foreach (int eventNum in randEvents)
        {
            GameObject eventCard = Instantiate(_eventSelectable, _eventCardArea);
            eventCard.GetComponent<NightEventCardVisual>().Setup(eventNum);

            Debug.Log("<color=blue>CLIENT: </color>Made new event card " + eventCard.name);
        }
    }

    public void ClearOptions()
    {
        Debug.Log("Clearing");

        foreach (Transform child in _eventCardArea)
            Destroy(child.gameObject);
    }

    public void SelectEvent(int nightEventID)
    {
        if(nightEventID != 0)
        {
            _eventManager.SetNightEventServerRpc(nightEventID);
            _eventManager.CloseNightEventPicker();
        }
        else
            Debug.LogError("nightEventID was 0");
    }
}
