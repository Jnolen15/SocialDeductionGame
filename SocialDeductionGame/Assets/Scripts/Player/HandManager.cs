using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class HandManager : NetworkBehaviour
{
    // Tracks the local verisions of player cards
    // A list of card's is controlled by the server in player card manager
    // ================ Refrences / Variables ================
    private PlayerCardManager _pcm;
    private CraftingUI _craftingUI;
    [SerializeField] private Transform _handZone;
    [SerializeField] private GameObject _cardSlotPref;
    [SerializeField] private List<CardSlot> _playerDeck = new();
    [SerializeField] private int numSlotsToDestroy;
    [SerializeField] private GearSlot[] _gearSlots;
    [SerializeField] private Gear[] _equipedGear;
    public class CardSlot
    {
        public Transform Slot;
        public Card HeldCard;

        // Constructors
        public CardSlot()
        {
            Slot = null;
            HeldCard = null;
        }

        public CardSlot(Transform slot, Card newCard = null)
        {
            Slot = slot;
            HeldCard = newCard;
        }

        // Methods
        public Transform GetSlot()
        {
            return Slot;
        }
        public Card GetCard()
        {
            return HeldCard;
        }

        public bool HasCard()
        {
            if (HeldCard)
                return true;

            return false;
        }

        public void SlotCard(Card newCard)
        {
            HeldCard = newCard;
        }

        public void RemoveCard()
        {
            HeldCard = null;
        }
    }

    // ================ Setup ================
    #region Setup
    public override void OnNetworkSpawn()
    {
        if (!IsOwner && !IsServer) enabled = false;
    }

    private void Start()
    {
        _pcm = this.GetComponent<PlayerCardManager>();
        _craftingUI = this.GetComponentInChildren<CraftingUI>();

        // Initialize gear and hand
        _equipedGear = new Gear[2];
        SetupHand(_pcm.GetHandSize());
    }
    #endregion

    // ================ Helpers ================
    #region Helpers
    public int GetNumCardsHeld()
    {
        int numCards = 0;
        foreach (CardSlot slot in _playerDeck)
        {
            if (slot.HasCard())
                numCards++;
        }
        return numCards;
    }

    public List<int> GetRandomHeldCards(int num)
    {
        // Make a temp list of only slots with cards
        List<CardSlot> tempSlotList = new();
        foreach (CardSlot slot in _playerDeck)
        {
            if (slot.HasCard())
                tempSlotList.Add(slot);
        }

        // Return list
        List<int> randomCards = new();

        // Fill randomCards
        for (int i = 0; i < num; i++)
        {
            if(tempSlotList.Count > 0)
            {
                CardSlot randSlot = tempSlotList[Random.Range(0, tempSlotList.Count)];
                randomCards.Add(randSlot.GetCard().GetCardID());
                tempSlotList.Remove(randSlot);
            }
            else
            {
                Debug.Log("Had less cards than the random ammount given");
                break;
            }
        }

        return randomCards;
    }
    #endregion

    //================ Card Slots ================
    #region Card Slots
    private void SetupHand(int handLimit)
    {
        for (int i = 0; i < handLimit; i++)
        {
            CreateNewCardSlot();
        }
    }

    private CardSlot CreateNewCardSlot()
    {
        Transform newSlotPref = Instantiate(_cardSlotPref, _handZone).transform;
        CardSlot newSlot = new CardSlot(newSlotPref);
        _playerDeck.Add(newSlot);
        return newSlot;
    }

    private void RemoveCardSlot()
    {
        // Note, its possible that this function could cause desync with lag
        // Since it removes a card locally before confirming with the server it was removed
        // But for now it works, but keep an eye on it for future

        // Remove the first slot with no card
        CardSlot slotToRemove = null;
        foreach (CardSlot slot in _playerDeck)
        {
            if (!slot.HasCard())
            {
                slotToRemove = slot;
                break;
            }
        }
        // Remove last card slot if all slots have cards
        if (slotToRemove == null)
        {
            slotToRemove = _playerDeck[_playerDeck.Count - 1];
        }

        // Remove card
        if (slotToRemove.HasCard())
        {
            Debug.Log("Removing a slot with a card id " + slotToRemove.GetCard().GetCardID());
            int[] toDiscard = new int[] { slotToRemove.GetCard().GetCardID() };
            _pcm.DiscardCardsServerRPC(toDiscard, false);
        }

        // Remove slot
        Debug.Log("Destroying a slot");
        Destroy(slotToRemove.Slot.gameObject);
        _playerDeck.Remove(slotToRemove);
        slotToRemove = null;
    }

    // Adds or removes card slots
    public void UpdateHandSlots(int newSlotCount)
    {
        int difference = (newSlotCount - _playerDeck.Count);

        // Increment
        if (difference >= 1)
        {
            Debug.Log("Adding hand slot(s)");

            for (int i = 0; i < difference; i++)
            {
                CreateNewCardSlot();
            }
        }
        // Decrement
        else if (difference <= -1)
        {
            Debug.Log("Removing hand slot(s)");
            for (int i = 0; i < difference*-1; i++)
            {
                RemoveCardSlot();
            }
        }
        // The same
        else if (difference == 0)
        {
            Debug.Log("UpdateHandSlots was called but slot count already the given number");
        }
    }

    private void AdjustSlots(CardSlot slot, bool bringFront)
    {
        if(bringFront)
            slot.GetSlot().SetAsFirstSibling();
        else
            slot.GetSlot().SetAsLastSibling();
    }
    #endregion

    // ================ Deck Management ================
    #region Deck Management
    private CardSlot GetFirstEmptySlot()
    {
        foreach (CardSlot slot in _playerDeck)
        {
            if (!slot.HasCard())
                return slot;
        }

        return null;
    }

    public void AddCard(int cardID)
    {
        CardSlot slot = GetFirstEmptySlot();

        if(slot == null)
        {
            Debug.LogError("Attempting to add a card with no empty slots left!");
            return;
        }

        Card newCard = Instantiate(CardDatabase.Instance.GetCard(cardID), slot.GetSlot()).GetComponent<Card>();
        newCard.SetupPlayable();
        slot.SlotCard(newCard);

        Debug.Log($"Adding a card {newCard.GetCardName()} to client {NetworkManager.Singleton.LocalClientId}");

        AdjustSlots(slot, true);
    }

    public void RemoveCard(int cardID)
    {
        Debug.Log($"Removing card with ID {cardID} from client {NetworkManager.Singleton.LocalClientId}");

        CardSlot cardToRemove = GetCardInDeck(cardID);

        if (cardToRemove != null)
        {
            Destroy(cardToRemove.GetCard().gameObject);
            cardToRemove.RemoveCard();
            AdjustSlots(cardToRemove, false);
        }
        else
            Debug.Log($"{cardID} not found in player's local hand!");

    }

    public CardSlot GetCardInDeck(int cardID)
    {
        foreach (CardSlot slot in _playerDeck)
        {
            if (slot.HasCard() && slot.GetCard().GetCardID() == cardID)
                return slot;
        }

        return null;
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

        Gear newGearCard = _gearSlots[gearSlot - 1].EqipGearCard(cardID);
        EquipToSlot(gearSlot, newGearCard);

        Debug.Log($"Equiping a gear card {newGearCard.GetCardName()} to client {NetworkManager.Singleton.LocalClientId}");
    }
    
    public void UpdateGearCard(int cardID, int gearSlot)
    {
        if(gearSlot != 1 && gearSlot != 2)
        {
            Debug.LogError($"Given gear slot {gearSlot} out of bounds");
            return;
        }

        RemoveGearCard(gearSlot);
        AddGearCard(cardID, gearSlot);
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

        if (!gearToRemove)
        {
            Debug.Log("Gear slot empty, nothing to lose");
            return;
        }

        gearToRemove.OnUnequip();

        _equipedGear[gearSlot - 1] = null;
        Destroy(gearToRemove.gameObject);

        Debug.Log($"Unequipping a gear card from slot {gearSlot}");
    }

    public int CheckGearTagsFor(string tag)
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

    public int CheckForHazardPreventionGear()
    {
        Debug.Log("Looking for equipped gear with 'Weapon' tag");

        foreach (Gear gear in _equipedGear)
        {
            if (gear != null && gear.HasTag("Weapon"))
            {
                Debug.Log("Found matching gear " + gear.GetCardName());
                return gear.GetCardID();
            }
        }

        Debug.Log("Did not find mathcing gear in either slot");
        return 0;
    }

    // This one is stackable, upodate CheckGearTagsFor to be stackable
    public int CheckForForageGear(string location)
    {
        Debug.Log("Looking for equipped gear with " + location + " tag");

        int matchingGear = 0;

        foreach (Gear gear in _equipedGear)
        {
            if (gear != null && gear.HasTag(location))
            {
                Debug.Log("Found matching gear " + gear.GetCardName());
                matchingGear++;
            }
        }

        Debug.Log($"Found {matchingGear} mathcing gear");
        return matchingGear;
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

    // ================ Crafting ================
    #region Crafting
    public void TryCraft(List<CardTag> requiredTags)
    {
        List<CardTag> tempTagList = requiredTags;
        List<int> cardIds = new();
        //int[] cardIds = new int[requiredTags.Count];

        Debug.Log("<color=blue>CLIENT: </color>Attempting to craft, searching local deck for required tags");

        // Look for them in local hand, then verify them in server
        foreach (CardSlot slot in _playerDeck)
        {
            if (slot.HasCard() && tempTagList.Count != 0)
            {
                foreach(CardTag tag in tempTagList)
                {
                    if (slot.GetCard().HasTag(tag))
                    {
                        Debug.Log($"<color=blue>CLIENT: </color>Card {slot.GetCard().GetCardName()} had tag {tag}");
                        cardIds.Add(slot.GetCard().GetCardID());
                        tempTagList.Remove(tag);
                        break;
                    }
                }
            }
        }

        // Found all tags
        if(tempTagList.Count == 0)
        {
            Debug.Log($"<color=blue>CLIENT: </color>Found all needed cards! Validating");
            _pcm.ValidateAndDiscardCardsServerRpc(cardIds.ToArray());
        }
        else
        {
            Debug.Log($"<color=blue>CLIENT: </color>Player does not have needed cards");
            CraftResults(false);
        }
    }

    public void CraftResults(bool crafted)
    {
        if(!_craftingUI)
            _craftingUI = this.GetComponentInChildren<CraftingUI>();

        Debug.Log("<color=blue>CLIENT: </color>Crafted = " + crafted);
        _craftingUI.Craft(crafted);
    }
    #endregion
}
