using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerController : NetworkBehaviour
{
    private CardDatabase cardDB;
    private Transform cardSlot;
    private TextMeshProUGUI cardPlay;
    [SerializeField] private GameObject cardText;

    [SerializeField] private List<CardData> playerDeck = new List<CardData>();

    public override void OnNetworkSpawn()
    {
        //if (!IsOwner) Destroy(this);
    }

    private void Start()
    {
        cardDB = GameObject.FindGameObjectWithTag("cardDB").GetComponent<CardDatabase>();
        cardSlot = GameObject.FindGameObjectWithTag("cardSlot").transform;
        cardPlay = GameObject.FindGameObjectWithTag("cardPlays").GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (IsHost) // Host has no need to call server RPC as it is the server
            {
                Debug.Log("Host call draw card");
                AddCardToPlayerDeck(cardDB.DrawCard());
            }
            else // If not the host, request server for a card
            {
                Debug.Log("Client call draw card");
                DrawCardServerRPC();
            }
        }

        if (Input.GetKeyDown(KeyCode.P) && playerDeck.Count > 0)
        {
            if (IsHost) // Host has no need to call server RPC as it is the server
            {
                Debug.Log("Host call play card");
                PlayCardServerRPC(playerDeck[0].CardID);
                //RemoveCardFromPlayerDeck(playerDeck[0].CardID);
            }
            else // If not the host, request server for a card
            {
                Debug.Log("Client call play card");
                PlayCardServerRPC(playerDeck[0].CardID);
            }
        }
    }

    // ================ CARD DRAW ================
    #region Card Draw
    [ServerRpc]
    private void DrawCardServerRPC(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        Debug.Log($"{clientId} drew a card");

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        GiveCardClientRpc(cardDB.DrawCard(), clientRpcParams);
        
        // For the server to own all the decks, Call this instead. But then will need a new client RPC to update it with what it has
        //AddCardToPlayerDeck(cardDB.DrawCard());
    }

    [ClientRpc]
    private void GiveCardClientRpc(int cardID, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} recieved a card with id {cardID}");

        AddCardToPlayerDeck(cardID);
    }

    private void AddCardToPlayerDeck(int cardID)
    {
        Debug.Log($"Adding a card with ID {cardID} to client {NetworkManager.Singleton.LocalClientId}");
        var newCard = cardDB.GetCard(cardID);
        playerDeck.Add(newCard);

        var cardTxt = Instantiate(cardText, cardSlot);
        cardTxt.GetComponent<TextMeshProUGUI>().text = newCard.CardName;
    }
    #endregion


    // ================ CARD Play ================
    #region Card Play
    [ServerRpc]
    private void PlayCardServerRPC(int cardID, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        Debug.Log($"{clientId} played a card with id {cardID}");

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        RemoveCardClientRpc(cardID, clientRpcParams);

        AnnounceCardPlayClientRpc(cardID, clientId);
    }

    [ClientRpc]
    private void RemoveCardClientRpc(int cardID, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} removing card with ID {cardID}");

        RemoveCardFromPlayerDeck(cardID);
    }

    private void RemoveCardFromPlayerDeck(int cardID)
    {
        Debug.Log($"Removing card with ID {cardID} from client {NetworkManager.Singleton.LocalClientId}");

        foreach (CardData card in playerDeck)
        {
            if (card.CardID == cardID)
            {
                playerDeck.Remove(card);
                break;
            }
        }

        Destroy(cardSlot.GetChild(0).gameObject);
    }

    [ClientRpc]
    private void AnnounceCardPlayClientRpc(int cardID, ulong clientID, ClientRpcParams clientRpcParams = default)
    {
        cardPlay.text = $"Player {clientID} Played card: {cardDB.GetCard(cardID).CardName}";
    }

    #endregion
}
