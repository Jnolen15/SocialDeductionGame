using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ServerSidePlayerData : NetworkBehaviour
{
    private HandManager _handManager;
    private CardDatabase _cardDB;
    private TextMeshProUGUI _cardPlay;
    [SerializeField] private List<int> _playerDeckIDs = new();

    private void Start()
    {
        _handManager = gameObject.GetComponent<HandManager>();
        _cardDB = GameObject.FindGameObjectWithTag("cardDB").GetComponent<CardDatabase>();
        _cardPlay = GameObject.FindGameObjectWithTag("cardPlays").GetComponent<TextMeshProUGUI>();
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

    // ================ CARD DRAW ================
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


    // ================ CARD Play ================
    #region Card Play
    [ServerRpc]
    private void PlayCardServerRPC(int cardID, ServerRpcParams serverRpcParams = default)
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

        if (_playerDeckIDs.Contains(cardID))
        {
            // Remove from player's networked deck
            _playerDeckIDs.Remove(cardID);

            // Update player hand
            RemoveCardClientRpc(cardID, clientRpcParams);

            // Display card play to all players
            AnnounceCardPlayClientRpc(cardID, clientId);
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

    [ClientRpc]
    private void AnnounceCardPlayClientRpc(int cardID, ulong clientID, ClientRpcParams clientRpcParams = default)
    {
        _cardPlay.text = $"Player {clientID} Played card: {_cardDB.GetCard(cardID).GetComponent<Card>().GetCardName()}";
    }

    #endregion
}
