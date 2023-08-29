using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;

public class PlayerData : NetworkBehaviour
{
    // ================== Refrences ==================
    private HandManager _handManager;
    private PlayerController _playerController;
    private PlayerHealth _playerHealth;
    [SerializeField] private PlayerUI _playerUI;

    private LocationManager _locationManager;
    private EventManager _nightEventManger;

    [SerializeField] private TextMeshProUGUI _teamText;

    // ================== Variables ==================
    public NetworkVariable<FixedString32Bytes> _netPlayerName = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<ulong> _netPlayerID = new();
    [SerializeField] private NetworkVariable<LocationManager.Location> _netCurrentLocation = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private List<int> _playerDeckIDs = new();
    public enum Team
    {
        Survivors,
        Saboteurs
    }
    private NetworkVariable<Team> _netTeam = new(writePerm: NetworkVariableWritePermission.Server);

    // Events
    public delegate void ChangeName(ulong id, string pName);
    public static event ChangeName OnChangeName;

    // ================== Setup ==================
    #region Setup
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocationManager.OnForceLocationChange += ChangeLocation;
            CardManager.OnCardsGained += GainCards;
            _netTeam.OnValueChanged += UpdateTeamText;
            GameManager.OnStateIntro += SetPlayerDefaultName;
            GameManager.OnStateNight += ShowEventChoices;

            SetPlayerIDServerRpc();
        } else
        {
            Destroy(_playerUI.gameObject);
            _playerUI = null;
        }

        if (!IsOwner && !IsServer)
            enabled = false;
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        LocationManager.OnForceLocationChange -= ChangeLocation;
        CardManager.OnCardsGained -= GainCards;
        _netTeam.OnValueChanged -= UpdateTeamText;
        GameManager.OnStateIntro -= SetPlayerDefaultName;
        GameManager.OnStateNight -= ShowEventChoices;
    }

    private void Start()
    {
        _handManager = gameObject.GetComponent<HandManager>();
        _playerController = gameObject.GetComponent<PlayerController>();
        _playerHealth = gameObject.GetComponent<PlayerHealth>();

        // TODO: NOT HAVE DIRECT REFRENCES, Use singleton or some other method
        GameObject gameMan = GameObject.FindGameObjectWithTag("GameManager");
        _locationManager = gameMan.GetComponent<LocationManager>();
        _nightEventManger = gameMan.GetComponent<EventManager>();

        UpdateTeamText(Team.Survivors, _netTeam.Value);
    }
    #endregion

    // ================ Player Name / ID ================
    #region Player Name / ID
    [ServerRpc]
    private void SetPlayerIDServerRpc(ServerRpcParams serverRpcParams = default)
    {
        _netPlayerID.Value = serverRpcParams.Receive.SenderClientId;
    }

    public ulong GetPlayerID()
    {
        return _netPlayerID.Value;
    }

    private void SetPlayerDefaultName()
    {
        _netPlayerName.Value = "Player " + _netPlayerID.Value;

        if(_playerUI != null)
            _playerUI.UpdatePlayerNameText(_netPlayerName.Value.ToString());
    }

    public void SetPlayerName(string pName)
    {
        if (pName == "")
            return;

        Debug.Log("Change Name " + _netPlayerID.Value + " to " + pName);
        _netPlayerName.Value = pName;
        _playerUI.UpdatePlayerNameText(_netPlayerName.Value.ToString());
        OnChangeName(_netPlayerID.Value, pName.ToString());
    }
    #endregion

    // ================ Teams ================
    #region Teams
    public void SetTeam(Team team)
    {
        _netTeam.Value = team;
    }

    private void UpdateTeamText(Team prev, Team current)
    {
        _teamText.text = current.ToString();

        if (current == Team.Survivors)
            _teamText.color = Color.green;
        else if (current == Team.Saboteurs)
            _teamText.color = Color.red;
    }

    // Show night event choices if Saboteur, else auto-ready
    private void ShowEventChoices()
    {
        if (_netTeam.Value == Team.Saboteurs)
            _nightEventManger.OpenNightEventPicker();
        else if (!_playerHealth.IsLiving())
            ReadyPlayer();
    }

    public Team GetPlayerTeam()
    {
        return _netTeam.Value;
    }
    #endregion

    // ====================== Player Readying ======================
    #region Player Readying
    public void ReadyPlayer()
    {
        GameManager.ReadyPlayer();
    }
    #endregion

    // ================ Player Deck ================
    #region Player Deck Functions
    // Triggered by CardManager's On Card Gained event, adds cards to players hand (server and client)
    public void GainCards(int[] cardIDs)
    {
        DrawCardsServerRPC(cardIDs);
    }
    #endregion

    #region Player Deck Helpers

    public int GetDeckSize()
    {
        return _playerDeckIDs.Count;
    }

    #endregion

    // ================ Card Add / Remove ================
    #region Card Draw
    [ServerRpc]
    private void DrawCardsServerRPC(int[] cardIDs, ServerRpcParams serverRpcParams = default)
    {
        // Get client data
        var clientId = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        foreach (int id in cardIDs)
        {
            // Add to player networked deck
            _playerDeckIDs.Add(id);

            // Update player hand
            GiveCardClientRpc(id, clientRpcParams);
        }
    }

    [ClientRpc]
    private void GiveCardClientRpc(int cardID, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} recieved a card with id {cardID}");

        _handManager.AddCard(cardID);
    }
    #endregion

    #region Card Discard
    // Discards all cards in players netwworked deck, and Hand Manager local deck
    [ServerRpc]
    public void DiscardHandServerRPC(ServerRpcParams serverRpcParams = default)
    {
        // Get client data
        var clientId = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        // Remove all cards from hand
        _playerDeckIDs.Clear();

        // Update player client hand
        DiscardHandClientRpc(clientRpcParams);
    }

    // Removes all cards from the clients hand locally
    [ClientRpc]
    private void DiscardHandClientRpc(ClientRpcParams clientRpcParams = default)
    {
        _handManager.DiscardHand();
    }
    #endregion

    // ================ Card Play ================
    #region Card Play

    // Test if card is in deck, then removes it and calls player controller to play it
    [ServerRpc]
    public void PlayCardServerRPC(int cardID, ServerRpcParams serverRpcParams = default)
    {
        // Get client data
        var clientId = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        // Test if networked deck contains the card that is being played
        if (_playerDeckIDs.Contains(cardID))
        {
            // Remove from player's networked deck
            _playerDeckIDs.Remove(cardID);

            // Update player client hand
            RemoveCardClientRpc(cardID, clientRpcParams);

            // Play card
            _playerController.ExecutePlayedCardClientRpc(cardID, clientRpcParams);
        }
        else
            Debug.LogError($"{cardID} not found in player's networked deck!");
    }

    // Removes cards from the clients hand
    [ClientRpc]
    private void RemoveCardClientRpc(int cardID, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} removing card with ID {cardID}");

        _handManager.RemoveCard(cardID);
    }

    #endregion

    // ================ Location ================
    #region Location
    // Called when the player chooses a location on their map
    // Or when location change is forced by game manager / location manager
    // Called by button
    public void ChangeLocation(string locationName)
    {
        LocationManager.Location newLocation;

        switch (locationName)
        {
            case "Camp":
                newLocation = LocationManager.Location.Camp;
                break;
            case "Beach":
                newLocation = LocationManager.Location.Beach;
                break;
            case "Forest":
                newLocation = LocationManager.Location.Forest;
                break;
            case "Plateau":
                newLocation = LocationManager.Location.Plateau;
                break;
            default:
                Debug.LogError("MoveToLocation picked default case, setting camp");
                newLocation = LocationManager.Location.Camp;
                break;
        }

        ChangeLocationServerRpc(newLocation);
    }

    // Called by event
    private void ChangeLocation(LocationManager.Location newLocation)
    {
        ChangeLocationServerRpc(newLocation);
    }
    
    [ServerRpc]
    public void ChangeLocationServerRpc(LocationManager.Location location, ServerRpcParams serverRpcParams = default)
    {
        // Get client data
        var clientId = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        ChangeLocationClientRpc(location, clientRpcParams);
    }

    [ClientRpc]
    public void ChangeLocationClientRpc(LocationManager.Location location, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("Changing player location to " + location.ToString());

        _netCurrentLocation.Value = location;
        _playerUI.UpdateLocationText(location.ToString());

        _locationManager.SetLocation(location);
    }
    #endregion

    // ================ Player Death ================
    public void OnPlayerDeath()
    {
        DiscardHandServerRPC();

        // Deal with ready for this round
        //ReadyPlayer();
        _playerUI.DisableReadyButton();
    }
}
