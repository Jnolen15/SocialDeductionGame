using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NightEventPicker : MonoBehaviour
{
    // ================== Refrences ==================
    private EventManager _eventManager;
    [SerializeField] private GameObject _eventSelectable;
    [SerializeField] private Transform _eventCardArea;

    // ================== Variables ==================
    [SerializeField] private List<NightEvent> _eventList;

    // ================== Setup ==================
    void OnEnable()
    {
        if(_eventManager == null)
            _eventManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EventManager>();
    }

    // ================== Function ==================
    public void DealOptions()
    {
        Debug.Log("DEALING");

        for(int i = 0; i < 3; i++)
        {
            int randEvent = _eventList[Random.Range(0, _eventList.Count)].GetEventID();

            GameObject eventCard = Instantiate(_eventSelectable, _eventCardArea);
            eventCard.GetComponent<NightEventCardVisual>().Setup(randEvent);

            Debug.Log("made new event card " + eventCard.name);
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
