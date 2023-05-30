using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    private CardDatabase cardDB;

    [SerializeField] private List<CardData> playerDeck = new List<CardData>();

    public override void OnNetworkSpawn()
    {
        //if (!IsOwner) Destroy(this);
    }

    private void Start()
    {
        cardDB = GameObject.FindGameObjectWithTag("cardDB").GetComponent<CardDatabase>();
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

        if (Input.GetKeyDown(KeyCode.P))
            Debug.Log($"{NetworkManager.Singleton.LocalClientId} played a card");
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

        playerDeck.Add(cardDB.GetCard(cardID));

        Debug.Log("Deck now contains:");
        foreach (CardData card in playerDeck)
        {
            Debug.Log(card.CardName);
        }
    }
    #endregion
}
