using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    // Refrences
    private HandManager _handManager;
    private ServerSidePlayerData _pData;
    private CardDatabase _cardDB;
    [SerializeField] private LayerMask _cardPlayableLayerMask;

    // Card playing
    //[SerializeField] private Card _heldCard;
    [SerializeField] private GameObject _cardPlayLocation;

    void Start()
    {
        _handManager = gameObject.GetComponent<HandManager>();
        _pData = gameObject.GetComponent<ServerSidePlayerData>();
        _cardDB = GameObject.FindGameObjectWithTag("cardDB").GetComponent<CardDatabase>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // TEST Draw a card
        if (Input.GetKeyDown(KeyCode.D))
        {
            _pData.DrawCard();
        }
    }

    // ================ Deck Interaction ================
    #region Deck Interaction
    public void SetHeldCard(Card card)
    {
        //_heldCard = card;
    }

    public void ClearHeldCard()
    {
        //_heldCard = null;
    }

    public void TryCardPlay(Card playedCard)
    {
        // Raycast test if card is played on playable object
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 999f, _cardPlayableLayerMask))
        {
            // Verufy object has script with correct interface
            _cardPlayLocation = hit.collider.gameObject;
            ICardPlayable cardPlayable = _cardPlayLocation.GetComponent<ICardPlayable>();
            if (cardPlayable != null)
            {
                // Verify this card can be played here
                if (cardPlayable.CanPlayCardHere(playedCard))
                {
                    // Try to play the card
                    _pData.PlayCardServerRPC(playedCard.GetCardID());
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


        ClearHeldCard();
    }

    [ClientRpc]
    public void ExecutePlayedCardClientRpc(int cardID, ClientRpcParams clientRpcParams = default)
    {
        // Instantiate the prefab to play it
        Card playedCard = Instantiate(_cardDB.GetCard(cardID), transform).GetComponent<Card>();

        Debug.Log($"{playedCard.GetCardName()} played on {_cardPlayLocation}");

        // Play the card to location
        playedCard.OnPlay(_cardPlayLocation);
    }
    #endregion
}
