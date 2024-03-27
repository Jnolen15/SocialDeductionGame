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
    [SerializeField] private Transform _handZone;
    [SerializeField] private TextMeshProUGUI _cardCount;
    [SerializeField] private TextMeshProUGUI _cardMax;
    [SerializeField] private GameObject _cardSlotPref;
    [SerializeField] private List<CardSlotUI> _playerDeck = new();
    [SerializeField] private GearSlot[] _gearSlots;
    [SerializeField] private Gear[] _equipedGear;
    private PlayerCardManager _pcm;
    private CraftingUI _craftingUI;

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
    }
    #endregion

    // ================ Helpers ================
    #region Helpers
    public int GetNumCardsHeld()
    {
        int numCards = 0;
        foreach (CardSlotUI slot in _playerDeck)
        {
            if (slot.HasCard())
                numCards++;
        }
        return numCards;
    }

    public List<int> GetRandomHeldCards(int num)
    {
        // Return list
        List<int> randomCards = new();
        foreach (CardSlotUI slot in _playerDeck)
        {
            if (slot.HasCard())
            {
                randomCards.Add(slot.GetCard().GetCardID());
            }
        }

        int numToRemove = randomCards.Count - num;

        if (numToRemove >= 1)
        {
            for (int i = 0; i < numToRemove; i++)
            {
                int randSlot = randomCards[Random.Range(0, randomCards.Count)];
                randomCards.Remove(randSlot);
            }
        }

        return randomCards;
    }
    #endregion

    //================ Other UI ================
    #region Other UI
    private void UpdateHandMaxNum(int handLimit)
    {
        _cardMax.text = handLimit.ToString();

        UpdateHandCountNum();
    }

    private void UpdateHandCountNum()
    {
        int count = 0;
        foreach (CardSlotUI slot in _playerDeck)
        {
            if (slot.HasCard())
                count++;
        }

        _cardCount.text = count.ToString();
    }
    #endregion

    //================ Card Slots ================
    #region Card Slots
    private CardSlotUI CreateNewCardSlot()
    {
        CardSlotUI newSlot = Instantiate(_cardSlotPref, _handZone).GetComponent<CardSlotUI>();
        _playerDeck.Add(newSlot);
        return newSlot;
    }

    private void RemoveCardSlot(CardSlotUI cardToRemove)
    {
        Debug.Log("Removing a card");
        _playerDeck.Remove(cardToRemove);
        cardToRemove.RemoveCard();
    }

    // Adds or removes card slots
    public void UpdateHandSize(int newSlotCount)
    {
        // Note, its possible that this function could cause desync with lag
        // Since it removes a card locally before confirming with the server it was removed
        // But for now it works, but keep an eye on it for future

        UpdateHandMaxNum(newSlotCount);

        int difference = (newSlotCount - _playerDeck.Count);

        // Remove cards if hand gets smaller
        if (difference <= -1)
        {
            Debug.Log("Removing hand slot(s)");
            for (int i = 0; i < difference*-1; i++)
            {
                // Remove last card slot
                CardSlotUI slotToRemove = _playerDeck[_playerDeck.Count - 1];

                // Remove card
                if (slotToRemove.HasCard())
                {
                    Debug.Log("Removing a slot with a card id " + slotToRemove.GetCard().GetCardID());
                    int[] toDiscard = new int[] { slotToRemove.GetCard().GetCardID() };
                    _pcm.DiscardCardsServerRPC(toDiscard, false);

                    RemoveCardSlot(slotToRemove);
                }
                else
                {
                    Debug.LogError("slot to be remvoed does not have a card");
                }
            }
        }
    }
    #endregion

    // ================ Deck Management ================
    #region Deck Management
    public void AddCard(int cardID)
    {
        if (GetNumCardsHeld() >= _pcm.GetMaxHandSize())
        {
            Debug.LogError("Attempting to add a card with no space left!");
            return;
        }

        CardSlotUI slot = CreateNewCardSlot();

        Card newCard = Instantiate(CardDatabase.Instance.GetCard(cardID), slot.transform).GetComponent<Card>();
        newCard.SetupPlayable();
        slot.SlotCard(newCard);

        Debug.Log($"Adding a card {newCard.GetCardName()} to client {NetworkManager.Singleton.LocalClientId}");

        UpdateHandCountNum();
    }

    public void RemoveCard(int cardID)
    {
        Debug.Log($"Removing card with ID {cardID} from client {NetworkManager.Singleton.LocalClientId}");

        CardSlotUI cardToRemove = GetCardInDeck(cardID);

        if (cardToRemove != null)
        {
            RemoveCardSlot(cardToRemove);

            UpdateHandCountNum();
        }
        else
            Debug.Log($"{cardID} not found in player's local hand!");

    }

    public CardSlotUI GetCardInDeck(int cardID)
    {
        foreach (CardSlotUI slot in _playerDeck)
        {
            if (slot.HasCard() && slot.GetCard().GetCardID() == cardID)
                return slot;
        }

        return null;
    }
    #endregion

    // ================ Gear Management ================
    #region Gear Management
    private bool VerifyGearSlot(int gearSlot)
    {
        if (gearSlot != 1 && gearSlot != 2)
        {
            Debug.LogError($"Given gear slot {gearSlot} out of bounds");
            return false;
        }

        return true;
    }

    public void AddGearCard(int cardID, int gearSlot)
    {
        if (!VerifyGearSlot(gearSlot))
            return;

        Gear newGearCard = _gearSlots[gearSlot - 1].EquipGearCard(cardID);
        EquipToSlot(gearSlot, newGearCard);

        Debug.Log($"Equiping a gear card {newGearCard.GetCardName()} to client {NetworkManager.Singleton.LocalClientId}");
    }

    private void EquipToSlot(int gearSlot, Gear gear)
    {
        Debug.Log($"Equiping a gear to slot {gearSlot}");
        _equipedGear[gearSlot-1] = gear;
        _equipedGear[gearSlot - 1].OnEquip();
    }

    public void RemoveGearCard(int gearSlot, bool swapped)
    {
        if (!VerifyGearSlot(gearSlot))
            return;

        Gear gearToRemove = _equipedGear[gearSlot - 1];

        if (!gearToRemove)
        {
            Debug.Log("Gear slot empty, nothing to lose");
            return;
        }

        gearToRemove.OnUnequip();
        _equipedGear[gearSlot - 1] = null;

        _gearSlots[gearSlot - 1].UnequipGearCard(swapped);

        Debug.Log($"Unequipping a gear card from slot {gearSlot}");
    }

    public int CheckGearTagsFor(string tag)
    {
        // Returns gear ID so it can be used

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

    public int CheckForForageGear(string location)
    {
        // Returns number of gear cards with that tag

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
    public bool CheckLocalCanCraft(List<CardTag> requiredTags)
    {
        List<CardTag> tempTagList = requiredTags;

        // Look for resources in local hand
        foreach (CardSlotUI slot in _playerDeck)
        {
            if (slot.HasCard() && tempTagList.Count != 0)
            {
                foreach (CardTag tag in tempTagList)
                {
                    if (slot.GetCard().HasTag(tag))
                    {
                        tempTagList.Remove(tag);
                        break;
                    }
                }
            }
        }

        // Found all tags
        if (tempTagList.Count == 0)
        {
            Debug.Log($"<color=blue>CLIENT: </color>CheckLocalCanCraft Found all needed cards!");
            return true;
        }
        else
        {
            Debug.Log($"<color=blue>CLIENT: </color>CheckLocalCanCraft did not find all needed cards");
            return false;
        }
    }

    public void TryCraft(List<CardTag> requiredTags)
    {
        List<CardTag> tempTagList = requiredTags;
        List<int> cardIds = new();
        //int[] cardIds = new int[requiredTags.Count];

        Debug.Log("<color=blue>CLIENT: </color>Attempting to craft, searching local deck for required tags");

        // Look for them in local hand, then verify them in server
        foreach (CardSlotUI slot in _playerDeck)
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
