using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class EventManager : NetworkBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private TextMeshProUGUI _eventTitle;
    [SerializeField] private TextMeshProUGUI _eventRequiredNum;
    [SerializeField] private TextMeshProUGUI _eventRequiredTypes;
    [SerializeField] private TextMeshProUGUI _eventDescription;
    [SerializeField] private Stockpile _stockpile;
    [SerializeField] private NetworkVariable<int> _netCurrentNightEventID = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netPassedNightEvent = new(writePerm: NetworkVariableWritePermission.Server);

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateNight += DoEvent;

        if (IsServer)
        {
            GameManager.OnStateMorning += PickEvent;
            GameManager.OnStateEvening += TestEvent;
        }

        PickEvent();
    }

    private void OnDisable()
    {
        GameManager.OnStateNight -= DoEvent;

        if (IsServer)
        {
            GameManager.OnStateMorning -= PickEvent;
            GameManager.OnStateEvening -= TestEvent;
        }
    }

    // FOR TESTING
    private void PickEvent()
    {
        Debug.Log("PICKING EVENT");
        SetNightEventServerRpc(CardDatabase.GetRandEvent());
    }

    // Updates night event card UI elements
    [ClientRpc]
    private void UpdateEventUIClientRpc(int eventID)
    {
        _eventTitle.text = CardDatabase.GetEvent(eventID).GetEventName();
        _eventRequiredNum.text = "At least " + CardDatabase.GetEvent(eventID).GetSuccessPoints(PlayerConnectionManager.GetNumConnectedPlayers());
        string cardTypes = "";
        foreach(Card.CardSubType cardType in CardDatabase.GetEvent(eventID).GetCardTypes())
        {
            cardTypes += cardType + " ";
        }
        _eventRequiredTypes.text = cardTypes;
        _eventDescription.text = CardDatabase.GetEvent(eventID).GetEventDescription();
    }
    #endregion

    // ================== Night Events ==================
    #region Night Events
    // Checks if evnt ID is correct then updates the networked night event id
    [ServerRpc(RequireOwnership = false)]
    public void SetNightEventServerRpc(int eventID)
    {
        if (!CardDatabase.ContainsEvent(eventID))
            return;

        _netCurrentNightEventID.Value = eventID;
        UpdateEventUIClientRpc(eventID);
    }

    // Calls InvokeNightEvent if event test failed
    private void DoEvent()
    {
        if (_netPassedNightEvent.Value)
            Debug.Log("Event passed, no suffering");
        else
            InvokeNightEvent(_netCurrentNightEventID.Value);
    }

    // Gets event from database and invokes it
    private void InvokeNightEvent(int eventID)
    {
        NightEvent nEvent = CardDatabase.GetEvent(eventID);

        if (nEvent)
            nEvent.InvokeEvent();
        else
            Debug.LogError("No Night Event found");
    }

    // Tests if event was successfully prevented via correct resourcess in pile
    private void TestEvent()
    {
        Debug.Log("Testing Event");

        // IF SERVER then Run tests
        if (!IsServer)
            return;

        // Get night event
        NightEvent nEvent = CardDatabase.GetEvent(_netCurrentNightEventID.Value);
        if (!nEvent)
            return;

        // Keep track of success points locally
        int successPoints = 0;
        // In a loop, get each card in the stockpile
        int totCards = _stockpile.GetNumCards();
        for (int i = 0; i <= totCards; i++)
        {
            int cardID = _stockpile.GetTopCard();
            if (cardID == -1)
            {
                Debug.Log("No cards in stockpile");
                break;
            }
            GameObject card = CardDatabase.GetCard(cardID);
            bool matched = false;

            // Test if card subtype matches Night event subtipe requirement list
            foreach (Card.CardSubType type in nEvent.GetCardTypes())
            {
                if (card.GetComponent<Card>().GetCardSubType() == type)
                    matched = true;
            }

            Debug.Log($"Card Tested: {card.GetComponent<Card>().GetCardName()}, subtype {card.GetComponent<Card>().GetCardSubType()}");

            if (matched) // If it does +1 SP    
            {
                successPoints++;
                Debug.Log("Card Matched! " + successPoints);
            }
            else        // If not -1 SP
            {
                successPoints--;
                Debug.Log("Card did not Match! " + successPoints);
            }
        }

        // Send card to all players for visibility

        // If number of points >= number of required points, success
        if (successPoints >= nEvent.GetSuccessPoints(PlayerConnectionManager.GetNumConnectedPlayers()))
        {
            Debug.Log("Event Pass!");
            _netPassedNightEvent.Value = true;
        }
        else
        {
            Debug.Log("Event Fail!");
            _netPassedNightEvent.Value = false;
        }

    }
    #endregion
}
