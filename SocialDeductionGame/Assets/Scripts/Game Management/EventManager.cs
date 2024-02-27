using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class EventManager : NetworkBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private NightEventPicker _nightEventPickerMenu;
    [SerializeField] private NightEventResults _stockpileRecap;
    [SerializeField] private NightEventRecapUI _nightEventRecap;
    [SerializeField] private Stockpile _stockpile;

    [SerializeField] private NetworkVariable<int> _netCurrentNightEventID = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netNumEventPlayers = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netPreviousNightEventID = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netPassedNightEvent = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<bool> _netEarnedBonusNightEvent = new(writePerm: NetworkVariableWritePermission.Server);

    private NightEventPreview _nightEventThumbnail;
    private PlayerData.Team _localplayerTeam;

    public delegate void EventManagerEvent();
    public static event EventManagerEvent OnAfterNightEvent;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        _nightEventThumbnail = GameObject.FindGameObjectWithTag("GameInfoUI").GetComponentInChildren<NightEventPreview>();
        _localplayerTeam = PlayerData.Team.Survivors;

        PlayerData.OnTeamUpdated += AssignLocalTeam;
        GameManager.OnStateNight += ShowRecap;
        GameManager.OnStateNight += ShowNightEventPicker;
        TabButtonUI.OnEventPressed += ShowStockpileRecap;

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
        TabButtonUI.OnEventPressed -= ShowStockpileRecap;

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
    private void UpdateEventUIClientRpc(int[] goodCardIDs, int[] badCardIDs, ulong[] contributorIDS, int eventID, bool passed, bool bonus, Vector2 objectivePoints, Vector3 bonusPoints)
    {
        _nightEventThumbnail.SetEventResults(passed);

        // Show results
        ShowStockpileRecap();
        _stockpileRecap.DisplayResults(goodCardIDs, badCardIDs, contributorIDS, eventID, _netNumEventPlayers.Value, passed, bonus, objectivePoints, bonusPoints);
    }

    private void ShowStockpileRecap()
    {
        if (GameManager.Instance.GetCurrentGameState() == GameManager.GameState.Evening)
            _stockpileRecap.gameObject.SetActive(true);
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
        if (_localplayerTeam == PlayerData.Team.Survivors)
            _nightEventRecap.UpdateNightEvent(_netPreviousNightEventID.Value, _netNumEventPlayers.Value, _netPassedNightEvent.Value, _netEarnedBonusNightEvent.Value);

        _nightEventRecap.OpenRecap();
    }
    #endregion

    // ================== Event Setup ==================
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
    #endregion

    // ================== Event Execution ==================
    #region Event Execution
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
                // Globaly invoked events are invoked just once
                if (CardDatabase.Instance.GetEvent(_netCurrentNightEventID.Value).GetEventIsGloballyInvoked())
                    InvokeGlobalNightEventBonusServerRpc(_netCurrentNightEventID.Value);
                // Otherwise the event is invoted on each player object
                else
                    InvokeNightEventBonusServerRpc(_netCurrentNightEventID.Value);
            }
        }
        else
        {
            Debug.Log("<color=yellow>Server: </color>Event failed, time for suffering");
            // Globaly invoked events are invoked just once
            if (CardDatabase.Instance.GetEvent(_netCurrentNightEventID.Value).GetEventIsGloballyInvoked())
                InvokeGlobalNightEventServerRpc(_netCurrentNightEventID.Value);
            // Otherwise the event is invoted on each player object
            else
                InvokePlayerNightEventServerRpc(_netCurrentNightEventID.Value);
        }

        // Let clients know night event has completed
        AfterNightEventServerRpc();
    }

    // Gets event from database and invokes it on server
    [ServerRpc]
    private void InvokeGlobalNightEventServerRpc(int eventID)
    {
        Debug.Log("<color=yellow>Server: </color>Invoking global event");

        // Invoke Night Event
        NightEvent nEvent = CardDatabase.Instance.GetEvent(eventID);

        if (nEvent)
            nEvent.InvokeEvent();
        else
            Debug.LogError("<color=yellow>Server: </color>No Night Event found");
    }

    // Gets event from database and invokes it on each client
    [ServerRpc]
    private void InvokePlayerNightEventServerRpc(int eventID)
    {
        // Get a list of all player ids
        List<ulong> playerIDs = PlayerConnectionManager.Instance.GetLivingPlayerIDs();

        // Cycle through all players
        foreach (ulong playerID in playerIDs)
        {
            // Get player object
            GameObject playerObj = PlayerConnectionManager.Instance.GetPlayerObjectByID(playerID);

            if(playerObj != null)
            {
                // If they are not a sabotuer, invoke the night event and pass the game object
                if (PlayerConnectionManager.Instance.GetPlayerTeamByID(playerID) == PlayerData.Team.Survivors 
                    && playerObj.GetComponent<PlayerHealth>().IsLiving())
                {
                    Debug.Log($"<color=yellow>Server: </color>Invoking night event on player {playerID}!");

                    // Invoke Night Event
                    NightEvent nEvent = CardDatabase.Instance.GetEvent(eventID);

                    if (nEvent)
                        nEvent.InvokeEvent(playerObj);
                    else
                        Debug.LogError("<color=yellow>Server: </color>No Night Event found");
                }
                else { Debug.Log($"<color=yellow>Server: </color>Player {playerID} is a sabo and not effected by event"); }
            }
            else { Debug.LogError($"Player ID:{playerID}'s game object could not be found or returned null"); }
        }
    }

    // Gets event from database and invokes bonus
    [ServerRpc]
    private void InvokeGlobalNightEventBonusServerRpc(int eventID)
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
    [ServerRpc]
    private void InvokeNightEventBonusServerRpc(int eventID)
    {
        List<ulong> playerIDs = PlayerConnectionManager.Instance.GetLivingPlayerIDs();

        foreach (ulong playerID in playerIDs)
        {
            GameObject playerObj = PlayerConnectionManager.Instance.GetPlayerObjectByID(playerID);

            if (playerObj != null)
            {
                // If they are not a sabotuer, invoke the night event and pass the game object
                if (PlayerConnectionManager.Instance.GetPlayerTeamByID(playerID) == PlayerData.Team.Survivors 
                    && playerObj.GetComponent<PlayerHealth>().IsLiving())
                {
                    Debug.Log($"<color=yellow>Server: </color>Invoking night bonus on player {playerID}!");

                    // Invoke Night Event Bonus
                    NightEvent nEvent = CardDatabase.Instance.GetEvent(eventID);

                    if (nEvent)
                        nEvent.InvokeBonus(playerObj);
                    else
                        Debug.LogError("<color=yellow>Server: </color>No Night Event found");
                }
                else { Debug.Log($"<color=yellow>Server: </color>Player {playerID} is a sabo and not effected by bonus"); }
            }
            else { Debug.LogError($"Player ID:{playerID}'s game object could not be found or returned null"); }
        }
    }

    [ServerRpc]
    private void AfterNightEventServerRpc()
    {
        Debug.Log("<color=yellow>Server: </color>Night event completed, Sending event to start hunger drain.");
        OnAfterNightEvent?.Invoke();
    }
    #endregion

    // ================== Event Testing ==================
    #region Event Testing
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
        Vector2 objectivePoints = new Vector2(0, 0); // X=Primary, Y=Secondary
        Vector3 bonusPoints = new Vector3(0, 0, 0); // X=Total, Y=Good, Z=Bad

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
                objectivePoints.x++;
                primaryCards.Add(cardID);
                Debug.Log($"<color=yellow>SERVER: </color>Card Tested: {card.GetComponent<Card>().GetCardName()}, " +
                            $"contained tag {nEvent.GetPrimaryResource()}. Primary required remaining {primaryReq}");
            }
            else if (card.GetComponent<Card>().HasTag(nEvent.GetSecondaryResource()))
            {
                secondaryReq--;
                objectivePoints.y++;
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

        // Get ammount below 0 both are, subtract sabo from this num
        int extraPrime = 0;
        int extraSecondary = 0;
        if (primaryReq <= 0)
            extraPrime = Mathf.Abs(primaryReq);
        if (secondaryReq <= 0)
            extraSecondary = Mathf.Abs(secondaryReq);
        int overBonus = extraPrime + extraSecondary;
        bonusPoints.y = overBonus;
        bonusPoints.z = saboCards;
        Debug.Log($"<color=yellow>SERVER: </color>{overBonus} extra resources were added.");
        overBonus -= saboCards;
        Debug.Log($"<color=yellow>SERVER: </color>-{saboCards} bonus now {overBonus}.");

        bonusPoints.x = overBonus;

        // After if both primary and secondary are not at or below 0 fail
        if (primaryReq > 0 || secondaryReq > 0)
        {
            Debug.Log($"<color=yellow>SERVER: </color>FAILED! not enough resources: needed {primaryReq} more primary and {secondaryReq} more secondary");
        }
        else
        {
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
                Debug.Log("<color=yellow>SERVER: </color>Event sabotaged! Fail!");
                successfulSabotage = true;
            }
        }

        // Award suffering
        if (successfulSabotage)
            SufferingManager.Instance.ModifySuffering(2, 103, true);
        else if (attemptedSabotage)
            SufferingManager.Instance.ModifySuffering(1, 102, true);

        // Track Analytics
        AnalyticsTracker.Instance.TrackStockpileResult(_netPassedNightEvent.Value, _netEarnedBonusNightEvent.Value, attemptedSabotage);

        // Combine lists for clients
        List<int> goodCardIDList = new();
        goodCardIDList.AddRange(primaryCards);
        goodCardIDList.AddRange(secondaryCards);
        List<int> badCardIDList = new();
        badCardIDList.AddRange(otherCards);

        // Update all clients visually
        UpdateEventUIClientRpc(goodCardIDList.ToArray(), badCardIDList.ToArray(), _stockpile.GetContributorIDs(), _netCurrentNightEventID.Value, 
            _netPassedNightEvent.Value, _netEarnedBonusNightEvent.Value, objectivePoints, bonusPoints);
    }
    #endregion
}
