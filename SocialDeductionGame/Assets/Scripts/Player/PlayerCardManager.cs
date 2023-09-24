using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCardManager : NetworkBehaviour
{
    // ================ Refrences ================
    private PlayerData _pData;
    private HandManager _handManager;
    [SerializeField] private LayerMask _cardPlayableLayerMask;
    [SerializeField] private GameObject _cardPlayLocation;

    // ================ Variables ================
    [SerializeField] private int _defaultHandSize;
    [SerializeField] private NetworkVariable<int> _netHandSize = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private List<int> _playerDeckIDs = new();
    [SerializeField] private bool _discardMode;
    [SerializeField] private int _playerGearOne;
    [SerializeField] private int _playerGearTwo;
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
            CardManager.OnCardsGained += GainCards;
        }
    }

    void Start()
    {
        _pData = gameObject.GetComponent<PlayerData>();
        _handManager = gameObject.GetComponent<HandManager>();
    }

    public override void OnDestroy()
    {
        CardManager.OnCardsGained -= GainCards;

        // Always invoked the base 
        base.OnDestroy();
    }
    #endregion

    // ================ Player Deck ================
    #region Player Deck
    // Triggered by CardManager's On Card Gained event, adds cards to players hand (server and client)
    public void GainCards(int[] cardIDs)
    {
        DrawCardsServerRPC(cardIDs);
    }

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

    public void IncrementPlayerHandSize(int num)
    {
        IncrementPlayerHandSizeServerRpc(num);
    }

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

        IncrementPlayerHandSizeClientRpc(clientRpcParams);
    }

    [ClientRpc]
    public void IncrementPlayerHandSizeClientRpc(ClientRpcParams clientRpcParams = default)
    {
        _handManager.UpdateHandSlots(_netHandSize.Value);
        Debug.Log("<color=blue>CLIENT: </color> Incremented player hand size");
    }
    #endregion

    // ================ Card Add / Remove ================
    #region Card Draw
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

        foreach (int id in cardIDs)
        {
            // Make sure hand is not full
            if (GetNumCardsHeldServer() >= _netHandSize.Value)
            {
                Debug.Log("<color=yellow>SERVER: </color>Player " + clientId + "'s hand is full, cannot add more cards");
                return;
            }

            // Add to player networked deck
            _playerDeckIDs.Add(id);

            // Update player hand
            GiveCardClientRpc(id, clientRpcParams);
        }
    }

    [ClientRpc]
    private void GiveCardClientRpc(int cardID, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} recieved a card with id {cardID}");

        // Make sure hand is not full
        if (GetNumCardsHeldClient() >= _netHandSize.Value)
        {
            Debug.Log("<color=blue>CLIENT: </color>Player " + NetworkManager.Singleton.LocalClientId + "'s hand is full, cannot add more cards");
            return;
        }

        _handManager.AddCard(cardID);
    }
    #endregion

    #region Card Discard
    public void EnableDiscard()
    {
        _discardMode = true;
    }

    public void DisableDiscard()
    {
        _discardMode = false;
    }

    [ServerRpc]
    public void DiscardCardServerRPC(int cardID, ServerRpcParams serverRpcParams = default)
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
            Debug.Log($"<color=yellow>SERVER: </color> removed card {cardID} from {clientId}");

            // Remove from player's networked deck
            _playerDeckIDs.Remove(cardID);

            // Update player client hand
            RemoveCardClientRpc(cardID, clientRpcParams);
        }
        else
            Debug.LogError($"{cardID} not found in player's networked deck!");
    }

    // Removes cards from the clients hand
    [ClientRpc]
    private void RemoveCardClientRpc(int cardID, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"{NetworkManager.Singleton.LocalClientId} removing card with ID {cardID}");

        _handManager.RemoveCard(cardID);
    }

    // Discards all cards in players netwworked deck, and Hand Manager local deck
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

        // Remove all cards from hand
        _playerDeckIDs.Clear();

        // Update player client hand
        DiscardHandClientRpc(clientRpcParams);
    }

    // Removes all cards from the clients hand locally
    [ClientRpc]
    private void DiscardHandClientRpc(ClientRpcParams clientRpcParams = default)
    {
        _handManager.DiscardHand();
    }
    #endregion

    // ================ Card Play ================
    #region Card Play
    // Tests if card is played onto a card playable object then calls player data server RPC to play the card
    public void TryCardPlay(Card playedCard)
    {
        // If over discard zone
        if (_discardMode)
        {
            DiscardCardServerRPC(playedCard.GetCardID());
            return;
        }

        // If over gear slot
        if(_gearSlotHovered != 0)
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

    // Test if card is in deck, then removes it and calls player controller to play it
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
            // Remove from player's networked deck
            _playerDeckIDs.Remove(cardID);

            // Update player client hand
            RemoveCardClientRpc(cardID, clientRpcParams);

            // Play card
            ExecutePlayedCardClientRpc(cardID, clientRpcParams);
        }
        else
            Debug.LogError($"{cardID} not found in player's networked deck!");
    }

    // Instantiates the card prefab then calls its OnPlay function at the played location
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

    #endregion

    // ================ Gear ================
    #region Gear
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

    [ServerRpc]
    public void EquipGearServerRPC(int gearSlot, int cardID, ServerRpcParams serverRpcParams = default)
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
            // Remove from player's networked deck
            _playerDeckIDs.Remove(cardID);

            // Update player client hand
            RemoveCardClientRpc(cardID, clientRpcParams);

            // Equip Card
            if (gearSlot == 1)
                _playerGearOne = cardID;
            else if (gearSlot == 2)
                _playerGearTwo = cardID;

            EquipGearClientRpc(gearSlot, cardID, clientRpcParams);
        }
        else
            Debug.LogError($"{cardID} not found in player's networked deck!");
    }

    // Instantiates the card prefab then calls its OnPlay function at the played location
    [ClientRpc]
    public void EquipGearClientRpc(int gearSlot, int cardID, ClientRpcParams clientRpcParams = default)
    {
        // re-instantiate card
        _handManager.AddGearCard(cardID, gearSlot);
    }

    public CardTag CheckGearTags()
    {
        return null;
    }

    #endregion
}
