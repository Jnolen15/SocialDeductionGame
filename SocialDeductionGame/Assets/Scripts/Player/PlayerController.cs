using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    // Refrences
    private HandManager _handManager;
    private PlayerData _pData;
    [SerializeField] private LayerMask _cardPlayableLayerMask;
    [SerializeField] private GameObject _playerObjPref;

    // Card playing
    [SerializeField] private GameObject _cardPlayLocation;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner && !IsServer) enabled = false;

        if (IsOwner)
            Instantiate(_playerObjPref, transform);
    }

    void Start()
    {
        _handManager = gameObject.GetComponent<HandManager>();
        _pData = gameObject.GetComponent<PlayerData>();
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
    // Tests if card is played onto a card playable object then calls player data server RPC to play the card
    public void TryCardPlay(Card playedCard)
    {
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
    }

    // Instantiates the card prefab then calls its OnPlay function at the played location
    [ClientRpc]
    public void ExecutePlayedCardClientRpc(int cardID, ClientRpcParams clientRpcParams = default)
    {
        // Instantiate the prefab to play it
        Card playedCard = Instantiate(CardDatabase.GetCard(cardID), transform).GetComponent<Card>();

        Debug.Log($"{playedCard.GetCardName()} played on {_cardPlayLocation}");

        // Play the card to location
        playedCard.OnPlay(_cardPlayLocation);
    }
    #endregion
}
