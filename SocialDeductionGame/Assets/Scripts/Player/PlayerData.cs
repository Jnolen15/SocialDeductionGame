using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerData : NetworkBehaviour
{
    // Refrences
    private HandManager _handManager;
    private PlayerController _playerController;
    private CardDatabase _cardDB;
    [SerializeField] private TextMeshProUGUI _locationText;

    // Location
    public enum Location
    {
        Camp,
        Beach,
        Forest,
        Plateau
    }
    [SerializeField] private NetworkVariable<Location> _netCurrentLocation = new(writePerm: NetworkVariableWritePermission.Owner);

    // Data
    [SerializeField] private List<int> _playerDeckIDs = new();

    public override void OnNetworkSpawn()
    {
        if (!IsOwner && !IsServer)
            enabled = false;

        if (IsOwner)
        {
            LocationManager.OnLocationChanged += ChangeLocation;
            CardManager.OnCardsGained += GainCards;
        }
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        LocationManager.OnLocationChanged -= ChangeLocation;
        CardManager.OnCardsGained -= GainCards;
    }

    private void Start()
    {
        _handManager = gameObject.GetComponent<HandManager>();
        _playerController = gameObject.GetComponent<PlayerController>();
        _cardDB = GameObject.FindGameObjectWithTag("cardDB").GetComponent<CardDatabase>();
    }

    // ================ Player Deck ================
    #region Player Deck Functions
    public void GainCards(int[] cardIDs)
    {
        DrawCardsServerRPC(cardIDs);
    }

    // TESTING: Gets a random card from DB
    public void DrawCard()
    {
        int[] ranCard = { (_cardDB.DrawCard()) };

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

    // Test if card is in deck and can be played
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

    [ClientRpc]
    private void RemoveCardClientRpc(int cardID, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} removing card with ID {cardID}");

        _handManager.RemoveCard(cardID);
    }

    #endregion

    // ================ Location ================
    #region Location
    private void ChangeLocation(string locationName)
    {
        switch (locationName)
        {
            case "Camp":
               ChangeLocationServerRpc(PlayerData.Location.Camp);
                return;
            case "Beach":
                ChangeLocationServerRpc(PlayerData.Location.Beach);
                return;
            case "Forest":
                ChangeLocationServerRpc(PlayerData.Location.Forest);
                return;
            case "Plateau":
                ChangeLocationServerRpc(PlayerData.Location.Plateau);
                return;
            default:
                Debug.LogError("Set Player Location set default case");
                ChangeLocationServerRpc(PlayerData.Location.Camp);
                return;
        }
    }
    
    [ServerRpc]
    public void ChangeLocationServerRpc(Location location, ServerRpcParams serverRpcParams = default)
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
    public void ChangeLocationClientRpc(Location location, ClientRpcParams clientRpcParams = default)
    {
        _netCurrentLocation.Value = location;
        _locationText.text = location.ToString();
    }
    #endregion
}
