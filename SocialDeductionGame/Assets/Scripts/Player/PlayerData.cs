using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerData : NetworkBehaviour
{
    // ================== Refrences ==================
    private HandManager _handManager;
    private PlayerController _playerController;
    private PlayerUI _playerUI;
    private LocationManager _locationManager;


    // ================== Variables ==================
    [SerializeField] private NetworkVariable<LocationManager.Location> _netCurrentLocation = new(writePerm: NetworkVariableWritePermission.Owner);
    [SerializeField] private List<int> _playerDeckIDs = new();

    // ================== Setup ==================
    public override void OnNetworkSpawn()
    {
        if (!IsOwner && !IsServer)
            enabled = false;

        if (IsOwner)
        {
            LocationManager.OnForceLocationChange += ChangeLocation;
            CardManager.OnCardsGained += GainCards;
        }
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        LocationManager.OnForceLocationChange -= ChangeLocation;
        CardManager.OnCardsGained -= GainCards;
    }

    private void Start()
    {
        _handManager = gameObject.GetComponent<HandManager>();
        _playerController = gameObject.GetComponent<PlayerController>();
        _playerUI = gameObject.GetComponentInChildren<PlayerUI>();
        _locationManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<LocationManager>();
    }

    // ================ Player Deck ================
    #region Player Deck Functions
    // Triggered by CardManager's On Card Gained event, adds cards to players hand (server and client)
    public void GainCards(int[] cardIDs)
    {
        DrawCardsServerRPC(cardIDs);
    }

    // TESTING: Gets a random card from DB
    public void DrawCard()
    {
        int[] ranCard = { (CardDatabase.DrawCard()) };

        DrawCardsServerRPC(ranCard);
    }

    #endregion

    #region Player Deck Helpers

    public int GetDeckSize()
    {
        return _playerDeckIDs.Count;
    }

    #endregion

    // ================ Card DRAW ================
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
}
