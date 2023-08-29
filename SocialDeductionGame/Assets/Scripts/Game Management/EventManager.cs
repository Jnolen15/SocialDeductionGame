using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class EventManager : NetworkBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private NightEventPicker _nightEventPickerMenu;
    [SerializeField] private NightEventResults _nightEventResults;
    [SerializeField] private NightEventRecapUI _nightEventRecap;
    [SerializeField] private NightEventCardVisual _eventCardSmall;
    [SerializeField] private NightEventCardVisual _eventCardLarge;
    [SerializeField] private GameObject _eventFailText;
    [SerializeField] private GameObject _eventPassText;
    [SerializeField] private Stockpile _stockpile;

    [SerializeField] private NetworkVariable<int> _netCurrentNightEventID = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netPreviousNightEventID = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netPassedNightEvent = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netEarnedBonusNightEvent = new(writePerm: NetworkVariableWritePermission.Server);

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateForage += HideLargeCard;
        GameManager.OnStateNight += DoEvent;

        if (IsServer)
        {
            GameManager.OnStateMorning += SetupNewEventServerRpc;
            GameManager.OnSetup += PickRandomEvent;
            GameManager.OnStateEvening += TestEvent;
        }
    }

    private void OnDisable()
    {
        GameManager.OnStateForage -= HideLargeCard;
        GameManager.OnStateNight -= DoEvent;

        if (IsServer)
        {
            GameManager.OnStateMorning -= SetupNewEventServerRpc;
            GameManager.OnSetup -= PickRandomEvent;
            GameManager.OnStateEvening -= TestEvent;
        }
    }
    #endregion

    // ================== UI ELEMENTS ==================
    #region UI Elements
    // Updates night event card UI elements
    private void UpdateEventUI()
    {
        _eventFailText.SetActive(false);
        _eventPassText.SetActive(false);

        _eventCardSmall.gameObject.SetActive(true);
        _eventCardLarge.gameObject.SetActive(true);

        _eventCardSmall.Setup(_netCurrentNightEventID.Value);
        _eventCardLarge.Setup(_netCurrentNightEventID.Value);
    }

    // Updates small event card with pass / fail text
    [ClientRpc]
    private void UpdateEventUIClientRpc(int[] cardIDs, ulong[] contributorIDS, int eventID, bool passed, bool bonus)
    {
        if(passed)
            _eventPassText.SetActive(true);
        else
            _eventFailText.SetActive(true);

        // Show ressults
        _nightEventResults.gameObject.SetActive(true);
        _nightEventResults.DisplayResults(cardIDs, contributorIDS, eventID, passed, bonus);
    }

    // Hides large event card from morning phase
    private void HideLargeCard()
    {
        _eventCardLarge.gameObject.SetActive(false);
    }
    #endregion

    // ================== Player Night Event Picking Menu ==================
    #region Player Night Event Choice Menu
    public void OpenNightEventPicker()
    {
        _nightEventPickerMenu.gameObject.SetActive(true);
        _nightEventPickerMenu.DealOptions(_netPreviousNightEventID.Value);
    }

    public void ShowRecap()
    {
        _nightEventRecap.gameObject.SetActive(true);
        _nightEventRecap.Setup(_netPreviousNightEventID.Value, _netPassedNightEvent.Value, _netEarnedBonusNightEvent.Value);
    }
    #endregion

    // ================== Night Events ==================
    #region Night Events
    // Checks if event ID is correct then updates the networked night event id
    [ServerRpc(RequireOwnership = false)]
    public void SetNightEventServerRpc(int eventID)
    {
        if (!CardDatabase.ContainsEvent(eventID))
            return;

        _netCurrentNightEventID.Value = eventID;
    }

    private void PickRandomEvent()
    {
        Debug.Log($"<color=yellow>SERVER: </color>PICKING RANDOM EVENT");
        SetNightEventServerRpc(CardDatabase.GetRandEvent(_netPreviousNightEventID.Value));
    }

    [ServerRpc]
    private void SetupNewEventServerRpc()
    {
        // Check to see current event is not the same as previous event
        if(_netPreviousNightEventID.Value == _netCurrentNightEventID.Value)
        {
            Debug.Log("<color=yellow>SERVER: </color>Prev and current events are the same, picking random");
            _netCurrentNightEventID.Value = CardDatabase.GetRandEvent(_netPreviousNightEventID.Value);
        }

        // Set previous event to current event
        _netPreviousNightEventID.Value = _netCurrentNightEventID.Value;

        // Update the event UI on clients
        SetupNewEventClientRpc();
    }

    [ClientRpc]
    private void SetupNewEventClientRpc()
    {
        // Update the event UI
        UpdateEventUI();
    }

    private void DoEvent()
    {
        // Calls InvokeNightEvent if event test failed
        if (_netPassedNightEvent.Value)
        {
            Debug.Log("<color=blue>CLIENT: </color>Event passed, no suffering");
            if (_netEarnedBonusNightEvent.Value)
            {
                Debug.Log("<color=blue>CLIENT: </color>Event Bonus Earned!");
                InvokeNightEventBonus(_netCurrentNightEventID.Value);
            }
        }
            
        else
            InvokeNightEvent(_netCurrentNightEventID.Value);
    }

    // Gets event from database and invokes it
    private void InvokeNightEvent(int eventID)
    {
        // Saboteurs not effected by night events
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Cannot enact night event. Player object not found!");
            return;
        }

        if (player.GetComponent<PlayerData>().GetPlayerTeam() == PlayerData.Team.Saboteurs)
            return;

        // Invoke Night Event
        NightEvent nEvent = CardDatabase.GetEvent(eventID);

        if (nEvent)
            nEvent.InvokeEvent();
        else
            Debug.LogError("<color=blue>CLIENT: </color>No Night Event found");
    }

    // Gets event from database and invokes bonus
    private void InvokeNightEventBonus(int eventID)
    {
        // Saboteurs not effected by night event Bonuses
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Cannot enact night event bonus. Player object not found!");
            return;
        }

        if (player.GetComponent<PlayerData>().GetPlayerTeam() == PlayerData.Team.Saboteurs)
            return;

        // Invoke Night Event Bonus
        NightEvent nEvent = CardDatabase.GetEvent(eventID);

        if (nEvent)
            nEvent.InvokeBonus();
        else
            Debug.LogError("<color=blue>CLIENT: </color>No Night Event found");
    }

    // Tests if event was successfully prevented via correct resourcess in pile
    // Only the server runs this
    private void TestEvent()
    {
        Debug.Log("<color=yellow>SERVER: </color> Testing Event");

        // IF SERVER then Run tests
        if (!IsServer)
            return;

        // Get night event
        NightEvent nEvent = CardDatabase.GetEvent(_netCurrentNightEventID.Value);
        if (!nEvent)
            return;

        // Reset bools
        _netPassedNightEvent.Value = false;
        _netEarnedBonusNightEvent.Value = false;

        // Keep track of success points locally
        int successPoints = 0;
        // In a loop, get each card in the stockpile
        int totCards = _stockpile.GetNumCards();
        int[] cardIDS = new int[totCards]; // For pass to results screen
        for (int i = 0; i <= totCards; i++)
        {
            int cardID = _stockpile.GetTopCard();
            if (cardID == -1)
            {
                Debug.Log("<color=yellow>SERVER: </color>No cards in stockpile");
                break;
            }
            GameObject card = CardDatabase.GetCard(cardID);
            bool matched = false;

            cardIDS[i] = cardID;

            // Test if card subtype matches Night event subtype requirement list
            foreach (CardTag tag in nEvent.GetRequiredCardTags())
            {
                if (card.GetComponent<Card>().GetSubTags().Contains(tag))
                {
                    matched = true;
                    Debug.Log($"<color=yellow>SERVER: </color>Card Tested: {card.GetComponent<Card>().GetCardName()}, contained subtag {tag}");
                    break;
                }
            }

            if (matched) // If it does +1 SP    
            {
                successPoints++;
                Debug.Log("<color=yellow>SERVER: </color>Card Matched! " + successPoints);
            }
            else        // If not -1 SP
            {
                successPoints--;
                Debug.Log("<color=yellow>SERVER: </color>Card did not Match! " + successPoints);
            }
        }

        int spRequirement = nEvent.GetSuccessPoints(PlayerConnectionManager.GetNumLivingPlayers());
        // If number of points >= number of required points, success
        if (successPoints >= spRequirement)
        {
            Debug.Log("<color=yellow>SERVER: </color>Event Pass!");
            _netPassedNightEvent.Value = true;

            // If number of points >= extra bonus
            // Extra bonus calculated with number of connected players not living players
            if ((successPoints - spRequirement) >= nEvent.SPBonusCalculation(PlayerConnectionManager.GetNumConnectedPlayers()))
            {
                Debug.Log("<color=yellow>SERVER: </color>Earned Bonus!");
                _netEarnedBonusNightEvent.Value = true;
            }
        }
        else
            Debug.Log("<color=yellow>SERVER: </color>Event Fail!");

        // Update all clients visually
        UpdateEventUIClientRpc(cardIDS, _stockpile.GetContributorIDs(), _netCurrentNightEventID.Value, _netPassedNightEvent.Value, _netEarnedBonusNightEvent.Value);
    }
    #endregion
}
