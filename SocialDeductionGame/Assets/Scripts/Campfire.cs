using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Campfire : NetworkBehaviour, ICardPlayable
{
    [SerializeField] private Card.CardType _cardTypeAccepted;

    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (cardToPlay.GetCardType() == _cardTypeAccepted)
            return true;

        return false;
    }

    public void PlayCardHere(Card cardToPlay)
    {
        PlayCardServerRPC(cardToPlay.GetCardID());
    }

    [ServerRpc]
    private void PlayCardServerRPC(int cardID, ServerRpcParams serverRpcParams = default)
    {

    }
}
