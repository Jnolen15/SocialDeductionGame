using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ResourcePile : NetworkBehaviour, ICardPlayable
{
    [SerializeField] private Card.CardType _cardTypeAccepted;

    [SerializeField] private NetworkVariable<int> _netNumWood = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netNumStone = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netNumPlant = new(writePerm: NetworkVariableWritePermission.Server);

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
        // IDK if this or PlayCardHere is needed
    }

    public void AddResources(ResourceCard.ResourceType resourceType)
    {
        AddResourcesServerRpc(resourceType);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddResourcesServerRpc(ResourceCard.ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceCard.ResourceType.Wood:
                _netNumWood.Value++;
                break;
            case ResourceCard.ResourceType.Stone:
                _netNumStone.Value++;
                break;
            case ResourceCard.ResourceType.Plant:
                _netNumPlant.Value++;
                break;
        }
    }
}
