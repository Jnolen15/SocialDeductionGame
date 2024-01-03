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
    [SerializeField] private Stockpile _stockpile;

    [SerializeField] private NetworkVariable<int> _netCurrentNightEventID = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netNumEventPlayers = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netPreviousNightEventID = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netPassedNightEvent = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netEarnedBonusNightEvent = new(writePerm: NetworkVariableWritePermission.Server);

    private NightEventPreview _nightEventThumbnail;
    private PlayerData.Team _localplayerTeam;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        _nightEventThumbnail = GameObject.FindGameObjectWithTag("GameInfoUI").GetComponentInChildren<NightEventPreview>();
        _localplayerTeam = PlayerData.Team.Survivors;

        PlayerData.OnTeamUpdated += AssignLocalTeam;
        GameManager.OnStateNight += ShowRecap;
        GameManager.OnStateNight += ShowNightEventPicker;

        if (IsServer)
        {
            GameManager.OnStateNight += DoEventServerRpc;
            GameManager.OnStateMorning += SetupNewEventServerRpc;
            GameManager.OnSetup += PickRandomEvent;
            GameManager.OnStateEvening += TestEvent;
            GameManager.OnStateNight += UpdateNightEventPicker;
        }
    }

    private void OnDisable()
    {
        PlayerData.OnTeamUpdated -= AssignLocalTeam;
        GameManager.OnStateNight -= ShowRecap;
        GameManager.OnStateNight -= ShowNightEventPicker;

        if (IsServer)
        {
            GameManager.OnStateNight -= DoEventServerRpc;
            GameManager.OnStateMorning -= SetupNewEventServerRpc;
            GameManager.OnSetup -= PickRandomEvent;
            GameManager.OnStateEvening -= TestEvent;
            GameManager.OnStateNight -= UpdateNightEventPicker;
        }
    }
    #endregion

    // ================== UI ELEMENTS ==================
    #region UI Elements
    private void AssignLocalTeam(PlayerData.Team prev, PlayerData.Team current)
    {
        Debug.Log("Event manager updating local player team " + current);
        _localplayerTeam = current;

        _nightEventRecap.Setup(prev, current);
    }

    private void UpdateEventUI()
    {
        _nightEventThumbnail.SetEvent(_netCurrentNightEventID.Value, _netNumEventPlayers.Value);
    }

    [ClientRpc]
    private void UpdateEventUIClientRpc(int[] cardIDs, ulong[] contributorIDS, int eventID, bool passed, bool bonus, Vector3 scores)
    {
        _nightEventThumbnail.SetEventResults(passed);

        // Show results
        _nightEventResults.gameObject.SetActive(true);
        _nightEventResults.DisplayResults(cardIDs, contributorIDS, eventID, _netNumEventPlayers.Value, passed, bonus, scores);
    }

    public void UpdateNightEventPicker()
    {
        Debug.Log("<color=yellow>SERVER: </color> UpdateNightEventPicker");

        // Pick random event in case no votes
        PickRandomEvent();

        _nightEventPickerMenu.DealOptionsServerRpc(_netPreviousNightEventID.Value);
    }

    public void ShowNightEventPicker()
    {
        if (_localplayerTeam != PlayerData.Team.Saboteurs)
            return;

        _nightEventPickerMenu.ShowMenu();
    }

    public void ShowRecap()
    {
        _nightEventRecap.OpenRecap();

        if (_localplayerTeam == PlayerData.Team.Survivors)
            _nightEventRecap.UpdateNightEvent(_netPreviousNightEventID.Value, _netNumEventPlayers.Value, _netPassedNightEvent.Value, _netEarnedBonusNightEvent.Value);
    }
    #endregion

    // ================== Night Events ==================
    #region Night Events
    // Checks if event ID is correct then updates the networked night event id
    [ServerRpc(RequireOwnership = false)]
    public void SetNightEventServerRpc(int eventID)
    {
        if (!CardDatabase.Instance.ContainsEvent(eventID))
            return;

        _netCurrentNightEventID.Value = eventID;

        _netNumEventPlayers.Value = PlayerConnectionManager.Instance.GetNumLivingPlayers();

        Debug.Log($"<color=yellow>SERVER: </color>Setting event {_netCurrentNightEventID.Value} on player count {_netNumEventPlayers.Value}");
    }

    private void PickRandomEvent()
    {
        Debug.Log($"<color=yellow>SERVER: </color>PICKING RANDOM EVENT");
        SetNightEventServerRpc(CardDatabase.Instance.GetRandEvent(_netPreviousNightEventID.Value));
    }

    [ServerRpc]
    private void SetupNewEventServerRpc()
    {
        // Check to see current event is not the same as previous event
        if(_netPreviousNightEventID.Value == _netCurrentNightEventID.Value)
        {
            Debug.Log("<color=yellow>SERVER: </color>Prev and current events are the same, picking random");
            _netCurrentNightEventID.Value = CardDatabase.Instance.GetRandEvent(_netPreviousNightEventID.Value);
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

    [ServerRpc]
    private void DoEventServerRpc()
    {
        if (!IsServer)
            return;

        // Calls InvokeNightEvent if event test failed
        if (_netPassedNightEvent.Value)
        {
            Debug.Log("<color=yellow>Server: </color>Event passed, no suffering");
            if (_netEarnedBonusNightEvent.Value)
            {
                Debug.Log("<color=yellow>Server: </color>Event Bonus Earned!");
                // Invoke Server event bonus
                if (CardDatabase.Instance.GetEvent(_netCurrentNightEventID.Value).GetEventIsServerInvoked())
                    InvokeNightEventBonusServerRpc(_netCurrentNightEventID.Value);
                // Invoke client event bonus
                else
                    InvokeNightEventBonusClientRpc(_netCurrentNightEventID.Value);
            }
        }
        else
        {
            // Invoke Server event
            if (CardDatabase.Instance.GetEvent(_netCurrentNightEventID.Value).GetEventIsServerInvoked())
                InvokeNightEventServerRpc(_netCurrentNightEventID.Value);
            // Invoke client event
            else
                InvokeNightEventClientRpc(_netCurrentNightEventID.Value);
        }
    }

    // Gets event from database and invokes it on server
    [ServerRpc]
    private void InvokeNightEventServerRpc(int eventID)
    {
        Debug.Log("<color=yellow>Server: </color>Invoking server event");

        // Invoke Night Event
        NightEvent nEvent = CardDatabase.Instance.GetEvent(eventID);

        if (nEvent)
            nEvent.InvokeEvent();
        else
            Debug.LogError("<color=yellow>Server: </color>No Night Event found");
    }

    // Gets event from database and invokes it on each client
    [ClientRpc]
    private void InvokeNightEventClientRpc(int eventID)
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

        Debug.Log("<color=blue>CLIENT: </color>Invoking night event!");

        // Invoke Night Event
        NightEvent nEvent = CardDatabase.Instance.GetEvent(eventID);

        if (nEvent)
            nEvent.InvokeEvent();
        else
            Debug.LogError("<color=blue>CLIENT: </color>No Night Event found");
    }

    // Gets event from database and invokes bonus
    [ServerRpc]
    private void InvokeNightEventBonusServerRpc(int eventID)
    {
        Debug.Log("<color=yellow>SERVER: </color>Invoking server event bonus");

        // Invoke Night Event Bonus
        NightEvent nEvent = CardDatabase.Instance.GetEvent(eventID);

        if (nEvent)
            nEvent.InvokeBonus();
        else
            Debug.LogError("<color=yellow>SERVER: </color>No Night Event found");
    }

    // Gets event from database and invokes bonus
    [ClientRpc]
    private void InvokeNightEventBonusClientRpc(int eventID)
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

        Debug.Log("<color=blue>CLIENT: </color>Invoking night event bonus!");

        // Invoke Night Event Bonus
        NightEvent nEvent = CardDatabase.Instance.GetEvent(eventID);

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
        NightEvent nEvent = CardDatabase.Instance.GetEvent(_netCurrentNightEventID.Value);
        if (!nEvent)
            return;

        // Reset bools
        _netPassedNightEvent.Value = false;
        _netEarnedBonusNightEvent.Value = false;

        // Get requirement values
        int primaryReq = (int)nEvent.GetRequirements(_netNumEventPlayers.Value).x;
        int secondaryReq = (int)nEvent.GetRequirements(_netNumEventPlayers.Value).y;
        int saboCards = 0;

        int totCards = _stockpile.GetNumCards();

        // Suffering tracking
        bool attemptedSabotage = false;
        bool successfulSabotage = false;

        // For pass to results screen
        List<int> primaryCards = new();
        List<int> secondaryCards = new();
        List<int> otherCards = new();
        Vector3 scores = new Vector3(0, 0, 0);

        // Loop through all cards
        for (int i = 0; i <= totCards; i++)
        {
            int cardID = _stockpile.GetTopCard();

            // Break if no more cards
            if (cardID == -1)
            {
                Debug.Log("<color=yellow>SERVER: </color>No cards in stockpile");
                break;
            }

            GameObject card = CardDatabase.Instance.GetCard(cardID);

            // Check if card meets secondary or primary tag
            // If it does -1 to needed ammount of that
            if (card.GetComponent<Card>().HasTag(nEvent.GetPrimaryResource()))
            {
                primaryReq--;
                scores.x++;
                primaryCards.Add(cardID);
                Debug.Log($"<color=yellow>SERVER: </color>Card Tested: {card.GetComponent<Card>().GetCardName()}, " +
                            $"contained tag {nEvent.GetPrimaryResource()}. Primary required remaining {primaryReq}");
            }
            else if (card.GetComponent<Card>().HasTag(nEvent.GetSecondaryResource()))
            {
                secondaryReq--;
                scores.y++;
                secondaryCards.Add(cardID);
                Debug.Log($"<color=yellow>SERVER: </color>Card Tested: {card.GetComponent<Card>().GetCardName()}, " +
                            $"contained tag {nEvent.GetSecondaryResource()}. secondary required remaining {secondaryReq}");
            }
            // If it does not match either +1 to sabo
            else
            {
                attemptedSabotage = true;

                if (card.GetComponent<Card>().HasTag("Disruptive"))
                {
                    Debug.Log("<color=yellow>SERVER: </color>Disruptive card found, counting as 4");
                    saboCards += 4;
                }
                else
                    saboCards++;

                otherCards.Add(cardID);
                Debug.Log($"<color=yellow>SERVER: </color>Card Tested: {card.GetComponent<Card>().GetCardName()}, " +
                            $"did not match either resources. Sabo cards now {saboCards}");
            }
        }

        // After if both primary and secondary are not at or below 0 fail
        if (primaryReq > 0 || secondaryReq > 0)
        {
            Debug.Log($"<color=yellow>SERVER: </color>FAILED! not enough resources: needed {primaryReq} more primary and {secondaryReq} more secondary");
        }
        else
        {
            // Get ammount below 0 both are, subtract sabo from this num
            int overBonus = Mathf.Abs(primaryReq) + Mathf.Abs(secondaryReq);
            Debug.Log($"<color=yellow>SERVER: </color>{overBonus} extra resources were added.");
            overBonus -= saboCards;
            Debug.Log($"<color=yellow>SERVER: </color>-{saboCards} bonus now {overBonus}.");

            scores.z = overBonus;

            // If num is still positive pass
            if (overBonus >= 0)
            {
                Debug.Log("<color=yellow>SERVER: </color>Event Pass!");
                _netPassedNightEvent.Value = true;

                // If number of points >= extra bonus
                if (overBonus >= nEvent.GetBonusRequirements())
                {
                    Debug.Log("<color=yellow>SERVER: </color>Earned Bonus!");
                    _netEarnedBonusNightEvent.Value = true;
                }
            }
            // if its negitive, then event failed
            else
            {
                Debug.Log("<color=yellow>SERVER: </color>Event Fail!");
                successfulSabotage = true;
            }
        }

        // Award suffering
        if(successfulSabotage)
            SufferingManager.Instance.ModifySuffering(2, 103, true);
        else if (attemptedSabotage)
            SufferingManager.Instance.ModifySuffering(1, 102, true);

        // Combine lists for clients
        List<int> cardIDList = new();
        cardIDList.AddRange(primaryCards);
        cardIDList.AddRange(secondaryCards);
        cardIDList.AddRange(otherCards);

        // Update all clients visually
        UpdateEventUIClientRpc(cardIDList.ToArray(), _stockpile.GetContributorIDs(), _netCurrentNightEventID.Value, 
            _netPassedNightEvent.Value, _netEarnedBonusNightEvent.Value, scores);
    }
    #endregion
}
