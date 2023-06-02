using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class HandManager : NetworkBehaviour
{
    private ServerSidePlayerData _pData;
    private CardDatabase _cardDB;

    private Transform _cardSlot;

    [SerializeField] private List<Card> _playerDeck = new();

    private void Start()
    {
        _pData = gameObject.GetComponent<ServerSidePlayerData>();
        _cardDB = GameObject.FindGameObjectWithTag("cardDB").GetComponent<CardDatabase>();
        _cardSlot = GameObject.FindGameObjectWithTag("cardSlot").transform;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // TEST Draw a card
        if (Input.GetKeyDown(KeyCode.D))
        {
            _pData.DrawCard();
        }

        // TEST Play top card
        if (Input.GetKeyDown(KeyCode.P) && _playerDeck.Count > 0)
        {
            _pData.PlayCard(_playerDeck[0].GetCardID());
        }
    }

    // ================ Deck Management ================
    #region Deck Management

    public void AddCard(int cardID)
    {
        GameObject newCard = Instantiate(_cardDB.GetCard(cardID), _cardSlot);
        Card newCardScript = newCard.GetComponent<Card>();

        _playerDeck.Add(newCardScript);

        newCardScript.Setup();

        Debug.Log($"Adding a card {newCardScript.GetCardName()} to client {NetworkManager.Singleton.LocalClientId}");
    }

    public void RemoveCard(int cardID)
    {
        Debug.Log($"Removing card with ID {cardID} from client {NetworkManager.Singleton.LocalClientId}");

        Card cardToRemove = GetCardInDeck(cardID);

        if (cardToRemove != null)
        {
            _playerDeck.Remove(cardToRemove);

            Destroy(cardToRemove.gameObject);
        }
        else
            Debug.LogError($"{cardID} not found in player's local hand!");
    }

    public Card GetCardInDeck(int cardID)
    {
        foreach (Card card in _playerDeck)
        {
            if (card.GetCardID() == cardID)
                return card;
        }

        return null;
    }

    #endregion
}
