using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCardManager : NetworkBehaviour
{
    // ================ Refrences ================
    private PlayerData _pData;
    private PlayerHealth _pHealth;
    private HandManager _handManager;
    [SerializeField] private LayerMask _cardPlayableLayerMask;
    [SerializeField] private GameObject _cardPlayLocation;

    // ================ Variables ================
    [SerializeField] private int _defaultHandSize;
    [SerializeField] private NetworkVariable<int> _netHandSize = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private List<int> _playerDeckIDs = new();
    [SerializeField] private bool _discardMode;
    [SerializeField] private int[] _playerGear;
    private int _gearSlotHovered;

    // ================ Setup ================
    #region Setup
    public override void OnNetworkSpawn()
    {
        if (!IsOwner && !IsServer) enabled = false;

        if (IsServer)
        {
            _netHandSize.Value = _defaultHandSize;
        }

        if (IsOwner)
        {
            _netHandSize.OnValueChanged += UpdateHandSize;
            CardManager.OnCardsGained += GainCards;
        }
    }

    void Start()
    {
        _pData = gameObject.GetComponent<PlayerData>();
        _pHealth = gameObject.GetComponent<PlayerHealth>();
        _handManager = gameObject.GetComponent<HandManager>();

        _playerGear = new int[2];
    }

    public override void OnDestroy()
    {
        if (IsOwner)
        {
            _netHandSize.OnValueChanged -= UpdateHandSize;
            CardManager.OnCardsGained -= GainCards;
        }

        // Always invoked the base 
        base.OnDestroy();
    }
    #endregion

    // ================ Player Deck Helpers ================
    #region Deck Helpers
    public int GetDeckSize()
    {
        return _playerDeckIDs.Count;
    }

    public int GetHandSize()
    {
        return _netHandSize.Value;
    }

    public int GetNumCardsHeldClient()
    {
        return _handManager.GetNumCardsHeld();
    }

    public int GetNumCardsHeldServer()
    {
        return _playerDeckIDs.Count;
    }
    #endregion

    // =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+= PLAYER DECK =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    // ================ Card Add ================
    #region Card Add
    // ~~~~~~~~~~~ Local ~~~~~~~~~~~
    public void GainCards(int[] cardIDs)
    {
        // Maker sure player isn't dead
        if (!_pHealth.IsLiving())
            return;

        DrawCardsServerRPC(cardIDs);
    }

    // ~~~~~~~~~~~ RPCS ~~~~~~~~~~~
    [ServerRpc]
    private void DrawCardsServerRPC(int[] cardIDs, ServerRpcParams serverRpcParams = default)
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

        List<int> cardsGiven = new();

        foreach (int id in cardIDs)
        {
            // Make sure hand is not full
            if (GetNumCardsHeldServer() >= _netHandSize.Value)
            {
                Debug.Log("<color=yellow>SERVER: </color>Player " + clientId + "'s hand is full, cannot add more cards");
                break;
            }

            // Add to player networked deck
            _playerDeckIDs.Add(id);

            // Add to list to give to player
            cardsGiven.Add(id);
        }

        // Update player hand
        GiveCardsClientRpc(cardsGiven.ToArray(), clientRpcParams);
    }

    [ClientRpc]
    private void GiveCardsClientRpc(int[] cardIDs, ClientRpcParams clientRpcParams = default)
    {
        foreach(int id in cardIDs)
        {
            // Make sure hand is not full
            if (GetNumCardsHeldClient() >= _netHandSize.Value)
            {
                Debug.LogError("<color=blue>CLIENT: </color>Player " + NetworkManager.Singleton.LocalClientId + "'s hand is full, DESYNC!");
                return;
            }

            Debug.Log($"{NetworkManager.Singleton.LocalClientId} recieved a card with id {id}");

            _handManager.AddCard(id);
        }
    }
    #endregion

    // ================ Card Remove ================
    #region Card Remove
    // ~~~~~~~~~~~ Local ~~~~~~~~~~~
    public void EnableDiscard()
    {
        _discardMode = true;
    }

    public void DisableDiscard()
    {
        _discardMode = false;
    }

    public void DiscardRandom(int numToDiscard)
    {
        Debug.Log($"Discarding {numToDiscard} random cards");

        List<int> cardIDs = new(_handManager.GetRandomHeldCards(numToDiscard));
        if (cardIDs.Count > 0)
            DiscardCardsServerRPC(cardIDs.ToArray(), true);
        else
            Debug.Log("No cards to discard!");
    }

    // ~~~~~~~~~~~ RPCS ~~~~~~~~~~~
    [ServerRpc]
    public void DiscardCardsServerRPC(int[] cardIDs, bool removeFromHandManager, ServerRpcParams serverRpcParams = default)
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

        DiscardCardsOnServer(cardIDs, removeFromHandManager, clientRpcParams);
    }

    [ServerRpc]
    public void DiscardHandServerRPC(ServerRpcParams serverRpcParams = default)
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

        List<int> cardsRemoved = new(_playerDeckIDs);

        // Remove all cards from hand
        _playerDeckIDs.Clear();

        // Update player client hand
        RemoveCardsClientRpc(cardsRemoved.ToArray(), clientRpcParams);
    }

    [ClientRpc]
    private void RemoveCardsClientRpc(int[] cardIDs, ClientRpcParams clientRpcParams = default)
    {
        foreach (int id in cardIDs)
        {
            Debug.Log($"{NetworkManager.Singleton.LocalClientId} removing card with ID {id}");
            _handManager.RemoveCard(id);
        }
    }

    // ~~~~~~~~~~~ Server Only ~~~~~~~~~~~
    private void DiscardCardsOnServer(int[] cardIDs, bool removeFromHandManager, ClientRpcParams clientRpcParams)
    {
        if (!IsServer)
        {
            Debug.LogError("DiscardCardsOnServer Not called from server!");
            return;
        }

        List<int>cardsRemoved = new();

        foreach (int id in cardIDs)
        {
            // Test if networked deck contains the card
            if (_playerDeckIDs.Contains(id))
            {
                Debug.Log($"<color=yellow>SERVER: </color> removed card {id} from {clientRpcParams.Send.TargetClientIds.ToString()}");

                // Remove from player's networked deck
                _playerDeckIDs.Remove(id);

                cardsRemoved.Add(id);
            }
            else
                Debug.LogError($"{id} not found in player's networked deck!");
        }

        // Update player client hand
        if (removeFromHandManager)
            RemoveCardsClientRpc(cardsRemoved.ToArray(), clientRpcParams);
    }
    #endregion

    // ================ Card Play / Validate ================
    #region Card Play / Validate
    // ~~~~~~~~~~~ Local ~~~~~~~~~~~
    // Tests if card is played onto a card playable object then calls player data server RPC to play the card
    public void TryCardPlay(Card playedCard)
    {
        // If over discard zone
        if (_discardMode)
        {
            int[] toDiscard = new int[] { playedCard.GetCardID() };
            DiscardCardsServerRPC(toDiscard, true);
            return;
        }

        // If over gear slot
        if (_gearSlotHovered != 0)
        {
            EquipGear(_gearSlotHovered, playedCard);
            return;
        }

        // Raycast test if card is played on playable object
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 999f, _cardPlayableLayerMask))
        {
            // Verify object has script with correct interface
            _cardPlayLocation = hit.collider.gameObject;
            ICardPlayable cardPlayable = _cardPlayLocation.GetComponent<ICardPlayable>();
            if (cardPlayable != null)
            {
                // Verify this card can be played here
                if (cardPlayable.CanPlayCardHere(playedCard))
                {
                    // Try to play the card
                    PlayCardServerRPC(playedCard.GetCardID());
                    return;
                }
                else
                    Debug.Log("Card cannot be played here");
            }
            else
                Debug.LogError("Card Played on object on playable layer without ICardPlayable implementation");
        }
        else
            Debug.Log("Card not played on playable object");
    }

    // ~~~~~~~~~~~ RPCS ~~~~~~~~~~~
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
            // Discard the card
            int[] toDiscard = new int[] { cardID };
            DiscardCardsOnServer(toDiscard, true, clientRpcParams);

            // Play card
            ExecutePlayedCardClientRpc(cardID, clientRpcParams);
        }
        else
            Debug.LogError($"{cardID} not found in player's networked deck!");
    }

    [ClientRpc]
    public void ExecutePlayedCardClientRpc(int cardID, ClientRpcParams clientRpcParams = default)
    {
        // Instantiate the prefab to play it
        Card playedCard = Instantiate(CardDatabase.Instance.GetCard(cardID), transform).GetComponent<Card>();

        Debug.Log($"{playedCard.GetCardName()} played on {_cardPlayLocation}");

        // Play card to stockpile
        if (_cardPlayLocation.CompareTag("Stockpile"))
        {
            Stockpile stockpile = _cardPlayLocation.GetComponent<Stockpile>();
            playedCard.PlayToStockpile(stockpile);
        }
        // Play the card to location
        else
        {
            playedCard.OnPlay(_cardPlayLocation);
        }
    }

    [ServerRpc]
    public void ValidateAndDiscardCardsServerRpc(int[] cardIds, ServerRpcParams serverRpcParams = default)
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

        Debug.Log("<color=yellow>SERVER: </color>Verifying player has cards for crafting ", gameObject);

        // Test if client has cards
        bool hasCards = true;
        List<int> cardIDs = new();
        foreach (int id in cardIds)
        {
            if (_playerDeckIDs.Contains(id))
                cardIDs.Add(id);
            else
                hasCards = false;
        }

        // If does have all cards, discard them
        if (hasCards)
        {
            Debug.Log("<color=yellow>SERVER: </color>Verified player had cards, discarding");
            DiscardCardsOnServer(cardIDs.ToArray(), true, clientRpcParams);
        }

        ValidateAndDiscardCardsClientRpc(hasCards, clientRpcParams);
    }

    [ClientRpc]
    private void ValidateAndDiscardCardsClientRpc(bool crafted, ClientRpcParams clientRpcParams = default)
    {
        _handManager.CraftResults(crafted);
    }
    #endregion

    // ================ Player Hand Size ================
    #region Player Hand Size
    // ~~~~~~~~~~~ Local ~~~~~~~~~~~
    public void IncrementPlayerHandSize(int num)
    {
        IncrementPlayerHandSizeServerRpc(num);
    }

    private void UpdateHandSize(int prev, int cur)
    {
        Debug.Log($"<color=blue>CLIENT: </color> Adjusted player hand size. Was {prev}, now {cur}");

        _handManager.UpdateHandSlots(cur);
    }

    // ~~~~~~~~~~~ RPCS ~~~~~~~~~~~
    [ServerRpc]
    public void IncrementPlayerHandSizeServerRpc(int num, ServerRpcParams serverRpcParams = default)
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

        _netHandSize.Value += num;
        Debug.Log($"<color=yellow>SERVER: </color> Incremented player {clientId}'s hand size");
    }
    #endregion

    // ================ Gear ================
    #region Gear
    // ~~~~~~~~~~~ Local ~~~~~~~~~~~
    public void HoveringGearSlot(int gearNum)
    {
        _gearSlotHovered = gearNum;
    }

    public void EndHoveringGearSlot()
    {
        _gearSlotHovered = 0;
    }

    public void EquipGear(int gearSlot, Card card)
    {
        if (!card.HasTag("Gear"))
        {
            Debug.Log("Card is not gear, can't equip");
            return;
        }

        Debug.Log("Attempting to equip gear!");

        if (gearSlot == 1 || gearSlot == 2)
            EquipGearServerRPC(gearSlot, card.GetCardID());
        else
            Debug.Log("Attempting to equip to non-existant gear slot");
    }

    public void UnequipGear(int gearSlot, int gearID)
    {
        Debug.Log("Attempting to Unequip gear!");

        if (gearSlot == 1 || gearSlot == 2)
            UnequipGearServerRPC(gearSlot, gearID);
        else
            Debug.Log("Attempting to equip to non-existant gear slot");
    }

    public void LoseGear(int gearSlot)
    {
        Debug.Log("Discarding gear in slot " + gearSlot);

        if (gearSlot == 1 || gearSlot == 2)
        {
            UnequipGearServerRPC(gearSlot, 9999);
        }
        else
            Debug.Log("Attempting to unequip to non-existant gear slot");
    }

    // ~~~~~~~~~~~ RPCS ~~~~~~~~~~~
    [ServerRpc]
    public void EquipGearServerRPC(int gearSlot, int cardID, ServerRpcParams serverRpcParams = default)
    {
        if (gearSlot != 1 && gearSlot != 2)
        {
            Debug.LogError($"Given gear slot {gearSlot} out of bounds");
            return;
        }

        // Get client data
        var clientId = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        // Test if gear slot already has something in it, if so swap
        bool swap = false;
        if (_playerGear[gearSlot - 1] != 0)
        {
            Debug.Log("<color=yellow>SERVER: </color>Gear slot is full, Swapping!");
            _playerGear[gearSlot - 1] = 0;
            swap = true;
        }

        // Test if networked deck contains the card that is being played
        if (_playerDeckIDs.Contains(cardID))
        {
            // Discard the card
            int[] toDiscard = new int[] { cardID };
            DiscardCardsOnServer(toDiscard, true, clientRpcParams);

            // Equip Card
            _playerGear[gearSlot - 1] = cardID;

            if(!swap)
                EquipGearClientRpc(gearSlot, cardID, clientRpcParams);
            else
                SwapGearClientRpc(gearSlot, cardID, clientRpcParams);
        }
        else
            Debug.LogError($"{cardID} not found in player's networked deck!");
    }

    [ClientRpc]
    public void EquipGearClientRpc(int gearSlot, int cardID, ClientRpcParams clientRpcParams = default)
    {
        // re-instantiate card
        _handManager.AddGearCard(cardID, gearSlot);
    }

    [ClientRpc]
    public void SwapGearClientRpc(int gearSlot, int cardID, ClientRpcParams clientRpcParams = default)
    {
        // re-instantiate card
        _handManager.UpdateGearCard(cardID, gearSlot);
    }

    [ServerRpc]
    public void UnequipGearServerRPC(int gearSlot, int cardID, ServerRpcParams serverRpcParams = default)
    {
        if (gearSlot != 1 && gearSlot != 2)
        {
            Debug.LogError($"Given gear slot {gearSlot} out of bounds");
            return;
        }

        // Get client data
        var clientId = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        // Test if gear slot contains the card that is being discarded (or 9999 for discard whatever)
        if (_playerGear[gearSlot - 1] == cardID || cardID == 9999)
        {
            Debug.Log("<color=yellow>SERVER: </color>Unequiping gear from slot 1");
            _playerGear[gearSlot - 1] = 0;

            UnequipGearClientRpc(gearSlot, clientRpcParams);
        }
        else
            Debug.Log("<color=yellow>SERVER: </color>Gear slot did not contain card that is being unequipped");
    }

    [ClientRpc]
    public void UnequipGearClientRpc(int gearSlot, ClientRpcParams clientRpcParams = default)
    {
        _handManager.RemoveGearCard(gearSlot);
    }
    #endregion
}
