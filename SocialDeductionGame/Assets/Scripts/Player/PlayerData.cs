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
    private GameManager _gameManager;
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

    private void Start()
    {
        _handManager = gameObject.GetComponent<HandManager>();
        _playerController = gameObject.GetComponent<PlayerController>();
        _cardDB = GameObject.FindGameObjectWithTag("cardDB").GetComponent<CardDatabase>();
        _gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        if (IsOwner)
            _gameManager.SetThisPlayer(this);
    }

    // ================ Player Deck ================
    #region Player Deck Functions

    // TESTING: Gets a random card from DB
    public void DrawCard()
    {
        DrawCardServerRPC();

        /*if (IsHost) // Host has no need to call server RPC as it is the server
        {
            Debug.Log("Host call draw card");
            AddCardToPlayerDeck(_cardDB.DrawCard());
        }
        else // If not the host, request server for a card
        {
            Debug.Log("Client call draw card");
            DrawCardServerRPC();
        }*/
    }

    // Plays card with given ID if its in the player deck
    public void PlayCard(int cardID)
    {
        PlayCardServerRPC(cardID);

        /*if (IsHost) // Host has no need to call server RPC as it is the server
        {
            Debug.Log("Host call play card");
            PlayCardServerRPC(playerDeck[0].CardID);
            //RemoveCardFromPlayerDeck(playerDeck[0].CardID);
        }
        else // If not the host, request server for a card
        {
            Debug.Log("Client call play card");
            PlayCardServerRPC(playerDeck[0].CardID);
        }*/
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
    private void DrawCardServerRPC(ServerRpcParams serverRpcParams = default)
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

        // Get new random card
        int newCardID = _cardDB.DrawCard();

        // Add to player networked deck
        _playerDeckIDs.Add(newCardID);

        // Update player hand
        GiveCardClientRpc(newCardID, clientRpcParams);
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
