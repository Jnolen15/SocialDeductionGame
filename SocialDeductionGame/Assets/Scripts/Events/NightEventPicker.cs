using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NightEventPicker : NetworkBehaviour
{
    // ================== Refrences ==================
    private EventManager _eventManager;
    [SerializeField] private GameObject _eventMenu;
    [SerializeField] private GameObject _eventSelectable;
    [SerializeField] private Transform _eventCardArea;
    [SerializeField] private List<NightEventSelectable> _selectableEventList = new();

    private int _currentSelectedEvent;

    // Server event vote tracking
    private Dictionary<int, int> _eventVoteDict = new();

    // ================== Setup ==================
    public override void OnNetworkSpawn()
    {
        if(_eventManager == null)
            _eventManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EventManager>();

        GameManager.OnStateMorning += CloseEventPicker;
    }

    public override void OnDestroy()
    {
        GameManager.OnStateMorning -= CloseEventPicker;

        base.OnDestroy();
    }

    // ================== Function ==================
    public void ShowMenu()
    {
        _eventMenu.SetActive(true);
    }

    public void HideMenu()
    {
        _eventMenu.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DealOptionsServerRpc(int prevDayEvent = 0)
    {
        Debug.Log("<color=yellow>SERVER: </color> Picking event cards serverside");

        _eventVoteDict.Clear();

        // Get 3 random events then call clients
        List<int> randEvents = CardDatabase.Instance.GetRandEvents(3, prevDayEvent);

        foreach(int eventID in randEvents)
        {
            _eventVoteDict.Add(eventID, 0);
        }

        DealOptionsClientRpc(randEvents.ToArray());
    }

    [ClientRpc]
    public void DealOptionsClientRpc(int[] events)
    {
        foreach (int eventID in events)
        {
            GameObject eventCard = Instantiate(_eventSelectable, _eventCardArea);
            eventCard.GetComponent<NightEventCardVisual>().Setup(eventID, PlayerConnectionManager.Instance.GetNumLivingPlayers());
            AddToList(eventCard.GetComponent<NightEventSelectable>());
            Debug.Log("<color=blue>CLIENT: </color>Made new event card " + eventCard.name);
        }
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
            if(_currentSelectedEvent != 0)
                DeselectEvent(_currentSelectedEvent);

            // Select new event
            _currentSelectedEvent = nightEventID;
            SelectEventServerRpc(nightEventID);
        }
        else
            Debug.LogError("nightEventID was 0");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SelectEventServerRpc(int nightEventID)
    {
        _eventVoteDict[nightEventID] += 1;

        List<int> tempVotesList = new(_eventVoteDict.Values);
        SetEventVotesClientRpc(tempVotesList.ToArray());

        SetEventServerRpc();
    }

    public void DeselectEvent(int nightEventID)
    {
        if (nightEventID == 0)
            return;

        foreach (NightEventSelectable eventSelectable in _selectableEventList)
            eventSelectable.Deselect();

        DeselectEventServerRpc(nightEventID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeselectEventServerRpc(int nightEventID)
    {
        _eventVoteDict[nightEventID] -= 1;

        List<int> tempVotesList = new(_eventVoteDict.Values);
        SetEventVotesClientRpc(tempVotesList.ToArray());
    }

    [ClientRpc]
    private void SetEventVotesClientRpc(int[] voteNums)
    {
        int i = 0;
        foreach(NightEventSelectable eventSelectable in _selectableEventList)
        {
            eventSelectable.UpdateVotes(voteNums[i]);
            i++;
        }
    }

    [ServerRpc]
    private void SetEventServerRpc()
    {
        if (_eventVoteDict.Count == 0)
            return;

        // Look for event with most votes and set it
        int prevHighestEvent = 0;
        int HighestEvent = 0;
        foreach (int id in _eventVoteDict.Keys)
        {
            if(HighestEvent == 0)
            {
                HighestEvent = id;
            }
            else if(_eventVoteDict[id] >= _eventVoteDict[HighestEvent])
            {
                prevHighestEvent = HighestEvent;
                HighestEvent = id;
            }
        }

        // Check for tie and set event
        int chosenNightEventID = 0;
        if (prevHighestEvent != 0 && _eventVoteDict[HighestEvent] == _eventVoteDict[prevHighestEvent])
        {
            Debug.Log($"<color=yellow>SERVER: </color> Event vote tie! Event {HighestEvent} with {_eventVoteDict[HighestEvent]} votes and " +
                $"event {prevHighestEvent} with {_eventVoteDict[prevHighestEvent]}. Picking random between the two");
            int rand = Random.Range(1, 3);
            if (rand == 1)
                chosenNightEventID = HighestEvent;
            else if (rand == 2)
                chosenNightEventID = prevHighestEvent;
        } else
        {
            Debug.Log($"<color=yellow>SERVER: </color> Event {HighestEvent} won with {_eventVoteDict[HighestEvent]} votes");
            chosenNightEventID = HighestEvent;
        }

        _eventManager.SetNightEventServerRpc(chosenNightEventID);
    }

    private void CloseEventPicker()
    {
        Debug.Log("<color=blue>CLIENT: </color>Closing event picker");
        _currentSelectedEvent = 0;
        ClearOptions();
        HideMenu();
    }

    public void ClearOptions()
    {
        _selectableEventList.Clear();
        foreach (Transform child in _eventCardArea)
            Destroy(child.gameObject);
    }
}
