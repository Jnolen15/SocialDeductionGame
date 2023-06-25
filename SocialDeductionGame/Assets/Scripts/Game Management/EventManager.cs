using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class EventManager : NetworkBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private TextMeshProUGUI _eventName;
    [SerializeField] private Stockpile _stockpile;
    [SerializeField] private NetworkVariable<int> _netCurrentNightEventID = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netPassedNightEvent = new(writePerm: NetworkVariableWritePermission.Server);

    // ================== Setup ==================
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateNight += DoEvent;
        _netCurrentNightEventID.OnValueChanged += UpdateEventText;

        if (IsServer)
        {
            GameManager.OnStateMorning += PickEvent;
            GameManager.OnStateEvening += TestEvent;
        }
    }

    private void OnDisable()
    {
        GameManager.OnStateNight -= DoEvent;
        _netCurrentNightEventID.OnValueChanged -= UpdateEventText;

        if (IsServer)
        {
            GameManager.OnStateMorning -= PickEvent;
            GameManager.OnStateEvening -= TestEvent;
        }
    }

    // FOR TESTING
    private void PickEvent()
    {
        SetNightEventServerRpc(1001);
    }

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
            if(cardID == -1)
            {
                Debug.Log("No cards in stockpile");
                break;
            }
            GameObject card = CardDatabase.GetCard(cardID);
            bool matched = false;

            // Test if card subtype matches Night event subtipe requirement list
            foreach(Card.CardSubType type in nEvent.GetCardTypes())
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
        if(successPoints >= nEvent.GetSuccessPoints())
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

    private void DoEvent()
    {
        if (_netPassedNightEvent.Value)
            Debug.Log("Event passed, no suffering");
        else
            InvokeNightEvent(_netCurrentNightEventID.Value);
    }

    private void UpdateEventText(int prev, int current)
    {
        _eventName.text = "Night Event: " + CardDatabase.GetEvent(current).GetEventName();
    }

    // ================== Night Events ==================
    [ServerRpc(RequireOwnership = false)]
    public void SetNightEventServerRpc(int eventID)
    {
        if (CardDatabase.ContainsEvent(eventID))
            _netCurrentNightEventID.Value = eventID;
    }

    private void InvokeNightEvent(int eventID)
    {
        NightEvent nEvent = CardDatabase.GetEvent(eventID);

        if (nEvent)
            nEvent.InvokeEvent();
        else
            Debug.LogError("No Night Event found");
    }
}
