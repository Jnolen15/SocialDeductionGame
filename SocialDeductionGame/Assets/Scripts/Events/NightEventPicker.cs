using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NightEventPicker : MonoBehaviour
{
    // ================== Refrences ==================
    private EventManager _eventManager;
    [SerializeField] private GameObject _eventSelectable;
    [SerializeField] private Transform _eventCardArea;
    [SerializeField] private List<NightEventSelectable> _selectableEventList = new();

    // ================== Setup ==================
    void OnEnable()
    {
        if(_eventManager == null)
            _eventManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EventManager>();

        GameManager.OnStateMorning += CloseEventPicker;
    }

    private void OnDisable()
    {
        GameManager.OnStateMorning -= CloseEventPicker;
    }

    // ================== Function ==================
    public void DealOptions(int prevDayEvent = 0)
    {
        // Get 3 random events then deal them to the UI
        List<int> randEvents = CardDatabase.GetRandEvents(3, prevDayEvent);

        foreach (int eventID in randEvents)
        {
            GameObject eventCard = Instantiate(_eventSelectable, _eventCardArea);
            eventCard.GetComponent<NightEventCardVisual>().Setup(eventID);
            AddToList(eventCard.GetComponent<NightEventSelectable>());
            Debug.Log("<color=blue>CLIENT: </color>Made new event card " + eventCard.name);
        }

        // Select one event by default
        _selectableEventList[0].OnSelect();
    }

    public void AddToList(NightEventSelectable eventSelectable)
    {
        _selectableEventList.Add(eventSelectable);
    }

    public void SelectEvent(int nightEventID)
    {
        if(nightEventID != 0)
        {
            // Deselect other event
            DeselectEvents();

            // Select new event
            _eventManager.SetNightEventServerRpc(nightEventID);
        }
        else
            Debug.LogError("nightEventID was 0");
    }

    public void DeselectEvents()
    {
        foreach (NightEventSelectable eventSelectable in _selectableEventList)
            eventSelectable.Deselect();
    }

    private void CloseEventPicker()
    {
        Debug.Log("<color=blue>CLIENT: </color>Closing event picker");
        ClearOptions();
        gameObject.SetActive(false);
    }

    public void ClearOptions()
    {
        _selectableEventList.Clear();
        foreach (Transform child in _eventCardArea)
            Destroy(child.gameObject);
    }
}
