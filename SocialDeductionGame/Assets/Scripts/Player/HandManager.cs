using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class HandManager : NetworkBehaviour
{
    // Tracks the local verisions of player cards
    // A list of card's is controlled by the server in player data

    // ================ Refrences / Variables ================
    private PlayerCardManager _pcm;
    [SerializeField] private Transform _handZone;
    [SerializeField] private List<GameObject> _cardSlots = new();
    [SerializeField] private GameObject _cardSlotPref;

    [SerializeField] private List<Card> _playerDeck = new();

    // ================ Setup ================
    public override void OnNetworkSpawn()
    {
        if (!IsOwner && !IsServer) enabled = false;
    }

    private void Start()
    {
        _pcm = this.GetComponent<PlayerCardManager>();

        SetupHand(_pcm.GetHandSize());
    }

    // ================ Helpers ================
    private void SetupHand(int handLimit)
    {
        for(int i = 0; i < handLimit; i++)
        {
            GameObject newSlot = Instantiate(_cardSlotPref, _handZone);
            newSlot.transform.SetAsFirstSibling();
            _cardSlots.Add(newSlot);
        }
    }

    private void AdjustSlots()
    {
        foreach(GameObject slot in _cardSlots)
        {
            slot.SetActive(false);
        }

        int diff = _pcm.GetHandSize() - GetNumCardsHeld();

        for (int i = 0; i < diff; i++)
        {
            if (i >= _cardSlots.Count)
                Debug.LogError("Error, not enough card slots");

            _cardSlots[i].SetActive(true);
        }
    }

    public int GetNumCardsHeld()
    {
        return _playerDeck.Count;
    }

    // ================ Deck Management ================
    #region Deck Management

    public void AddCard(int cardID)
    {
        GameObject newCard = Instantiate(CardDatabase.GetCard(cardID), _handZone);
        newCard.transform.SetAsFirstSibling();
        Card newCardScript = newCard.GetComponent<Card>();

        _playerDeck.Add(newCardScript);

        newCardScript.SetupPlayable();

        Debug.Log($"Adding a card {newCardScript.GetCardName()} to client {NetworkManager.Singleton.LocalClientId}");

        AdjustSlots();
    }

    public void RemoveCard(int cardID)
    {
        Debug.Log($"Removing card with ID {cardID} from client {NetworkManager.Singleton.LocalClientId}");

        Card cardToRemove = GetCardInDeck(cardID);

        if (cardToRemove != null)
        {
            _playerDeck.Remove(cardToRemove);

            Destroy(cardToRemove.gameObject);

            AdjustSlots();
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

    public void DiscardHand()
    {
        // Clear list
        _playerDeck.Clear();

        // Destroy card objects
        foreach (Transform child in _handZone)
        {
            Destroy(child.gameObject);
        }

        AdjustSlots();
    }

    #endregion
}
