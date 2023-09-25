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
    [SerializeField] private Transform[] _gearSlots;
    [SerializeField] private Gear[] _equipedGear;

    // ================ Setup ================
    #region Setup
    public override void OnNetworkSpawn()
    {
        if (!IsOwner && !IsServer) enabled = false;
    }

    private void Start()
    {
        _pcm = this.GetComponent<PlayerCardManager>();

        _equipedGear = new Gear[2];

        SetupHand(_pcm.GetHandSize());
    }
    #endregion

    // ================ Helpers ================
    #region Helpers
    public int GetNumCardsHeld()
    {
        return _playerDeck.Count;
    }

    public int GetRandomHeldCard()
    {
        if (_playerDeck.Count == 0)
            return 0;

        return _playerDeck[Random.Range(0, _playerDeck.Count)].GetCardID();
    }
    #endregion

    //================ Card Slots ================
    #region Card Slots
    private void SetupHand(int handLimit)
    {
        for (int i = 0; i < handLimit; i++)
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
        foreach (GameObject slot in _cardSlots)
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
        if(gearSlot != 1 && gearSlot != 2)
        {
            Debug.LogError($"Given gear slot {gearSlot} out of bounds");
            return;
        }

        GameObject newGear = Instantiate(CardDatabase.Instance.GetCard(cardID), _gearSlots[gearSlot-1]);
        Gear newGearCard = newGear.GetComponent<Gear>();

        newGearCard.SetupPlayable();
        EquipToSlot(gearSlot, newGearCard);

        Debug.Log($"Equiping a gear card {newGearCard.GetCardName()} to client {NetworkManager.Singleton.LocalClientId}");
    }

    private void EquipToSlot(int gearSlot, Gear gear)
    {
        Debug.Log($"Equiping a gear to slot {gearSlot}");
        _equipedGear[gearSlot-1] = gear;
        _equipedGear[gearSlot - 1].OnEquip();
    }

    public void RemoveGearCard(int gearSlot)
    {
        if (gearSlot != 1 && gearSlot != 2)
        {
            Debug.LogError($"Given gear slot {gearSlot} out of bounds");
            return;
        }

        Gear gearToRemove = _equipedGear[gearSlot - 1];

        gearToRemove.OnUnequip();

        _equipedGear[gearSlot - 1] = null;
        Destroy(gearToRemove.gameObject);

        Debug.Log($"Unequipping a gear card from slot {gearSlot}");
    }

    public int CheckGearTagsFor(CardTag tag)
    {
        Debug.Log("Checking gear for tag " + tag);

        foreach (Gear gear in _equipedGear)
        {
            if (gear != null && gear.HasTag(tag))
            {
                Debug.Log("Found matching tag on " + gear.GetCardName());
                return gear.GetCardID();
            }
        }

        Debug.Log("Did not find mathcing tag in either slot");
        return 0;
    }

    public void UseGear(int gearID)
    {
        // Find gear
        Gear gearUsed = null;
        foreach (Gear gear in _equipedGear)
        {
            if (gear != null && gear.GetCardID() == gearID)
            {
                gearUsed = gear;
                break;
            }
        }

        if(gearUsed == null)
        {
            Debug.Log($"Gear {gearID} used not found!");
            return;
        }

        gearUsed.OnUse();
    }
    #endregion
}
