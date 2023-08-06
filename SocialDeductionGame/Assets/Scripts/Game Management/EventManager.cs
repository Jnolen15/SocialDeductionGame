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
    [SerializeField] private NightEventCardVisual _eventCardSmall;
    [SerializeField] private NightEventCardVisual _eventCardLarge;
    [SerializeField] private GameObject _eventFailText;
    [SerializeField] private GameObject _eventPassText;
    [SerializeField] private Stockpile _stockpile;

    [SerializeField] private NetworkVariable<int> _netCurrentNightEventID = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netPassedNightEvent = new(writePerm: NetworkVariableWritePermission.Server);

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        GameManager.OnStateMorning += UpdateEventUI;
        GameManager.OnStateForage += HideLargeCard;
        GameManager.OnStateNight += DoEvent;

        if (IsServer)
        {
            GameManager.OnSetup += PickEvent;
            GameManager.OnStateEvening += TestEvent;
        }
    }

    private void OnDisable()
    {
        GameManager.OnStateMorning -= UpdateEventUI;
        GameManager.OnStateForage -= HideLargeCard;
        GameManager.OnStateNight -= DoEvent;

        if (IsServer)
        {
            GameManager.OnSetup -= PickEvent;
            GameManager.OnStateEvening -= TestEvent;
        }
    }

    // FOR TESTING
    private void PickEvent()
    {
        Debug.Log("PICKING EVENT");
        SetNightEventServerRpc(CardDatabase.GetRandEvent());
    }
    #endregion

    // ================== UI ELEMENTS ==================
    #region UI Elementss
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
    private void UpdateEventUIClientRpc(int[] cardIDs, ulong[] contributorIDS, int eventID, bool passed)
    {
        if(passed)
            _eventPassText.SetActive(true);
        else
            _eventFailText.SetActive(true);

        // Show ressults
        _nightEventResults.gameObject.SetActive(true);
        _nightEventResults.DisplayResults(cardIDs, contributorIDS, eventID, passed);
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
        _nightEventPickerMenu.DealOptions();
    }

    public void CloseNightEventPicker()
    {
        _nightEventPickerMenu.ClearOptions();
        _nightEventPickerMenu.gameObject.SetActive(false);
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

    private void DoEvent()
    {
        // Calls InvokeNightEvent if event test failed
        if (_netPassedNightEvent.Value)
            Debug.Log("Event passed, no suffering");
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
            Debug.LogError("No Night Event found");
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

        // If number of points >= number of required points, success
        if (successPoints >= nEvent.GetSuccessPoints(PlayerConnectionManager.GetNumLivingPlayers()))
        {
            Debug.Log("<color=yellow>SERVER: </color>Event Pass!");
            _netPassedNightEvent.Value = true;
        }
        else
        {
            Debug.Log("<color=yellow>SERVER: </color>Event Fail!");
            _netPassedNightEvent.Value = false;
        }

        // Update all clients visually
        UpdateEventUIClientRpc(cardIDS, _stockpile.GetContributorIDs(), _netCurrentNightEventID.Value, _netPassedNightEvent.Value);
    }
    #endregion
}
