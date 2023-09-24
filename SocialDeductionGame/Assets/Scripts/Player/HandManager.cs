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
    [SerializeField] private Transform _gearSlotOne;
    [SerializeField] private Transform _gearSlotTwo;
    [SerializeField] private Gear _equipedGearOne;
    [SerializeField] private Gear _equipedGearTwo;

    // ================ Setup ================
    #region Setup
    public override void OnNetworkSpawn()
    {
        if (!IsOwner && !IsServer) enabled = false;
    }

    private void Start()
    {
        _pcm = this.GetComponent<PlayerCardManager>();

        SetupHand(_pcm.GetHandSize());
    }
    #endregion

    // ================ Helpers ================
    #region Helpers
    private void SetupHand(int handLimit)
    {
        for(int i = 0; i < handLimit; i++)
        {
            GameObject newSlot = Instantiate(_cardSlotPref, _handZone);
            newSlot.transform.SetAsFirstSibling();
            _cardSlots.Add(newSlot);
        }
    }

    public void UpdateHandSlots(int newSlotCount)
    {
        int difference = (newSlotCount - _cardSlots.Count);

        // Increment
        if (difference >= 1)
        {
            Debug.Log("Adding hand slot(s)");

            for (int i = 0; i < difference; i++)
            {
                GameObject newSlot = Instantiate(_cardSlotPref, _handZone);
                newSlot.transform.SetAsFirstSibling();
                _cardSlots.Add(newSlot);
            }
        }
        // Decrement
        else if (difference <= -1)
        {
            Debug.Log("Removing hand slot(s)");

            Debug.LogError("REMOVING SLOT NOT YET IMPLEMENTED");
        }
        // The same
        else if (difference == 0)
        {
            Debug.Log("UpdateHandSlots was called but slot count already the given number");
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
    #endregion

    // ================ Deck Management ================
    #region Deck Management

    public void AddCard(int cardID)
    {
        GameObject newCard = Instantiate(CardDatabase.Instance.GetCard(cardID), _handZone);
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

    // ================ Gear Management ================
    #region Gear Management
    public void AddGearCard(int cardID, int gearSlot)
    {
        GameObject newGear = null;
        Gear newCardScript = null;

        if (gearSlot == 1)
        {
            newGear = Instantiate(CardDatabase.Instance.GetCard(cardID), _gearSlotOne);
            newCardScript = newGear.GetComponent<Gear>();
        }
        else if (gearSlot == 2)
        {
            newGear = Instantiate(CardDatabase.Instance.GetCard(cardID), _gearSlotTwo);
            newCardScript = newGear.GetComponent<Gear>();
        }

        newCardScript.SetupPlayable();
        EquipToSlot(gearSlot, newCardScript);

        Debug.Log($"Equiping a gear card {newCardScript.GetCardName()} to client {NetworkManager.Singleton.LocalClientId}");
    }

    private void EquipToSlot(int gearSlot, Gear gear)
    {
        Debug.Log($"Equiping a gear to slot {gearSlot}");

        if (gearSlot == 1)
        {
            _equipedGearOne = gear;
            _equipedGearOne.OnEquip();
        }
        else if (gearSlot == 2)
        {
            _equipedGearTwo = gear;
            _equipedGearTwo.OnEquip();
        }
    }
    #endregion
}
