using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ResourcePile : NetworkBehaviour, ICardPlayable
{
    [SerializeField] private Card.CardType _cardTypeAccepted;

    [SerializeField] private TextMeshPro _woodNumText;
    [SerializeField] private TextMeshPro _stoneNumText;
    [SerializeField] private TextMeshPro _plantNumText;

    [SerializeField] private NetworkVariable<int> _netNumWood = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netNumStone = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<int> _netNumPlant = new(writePerm: NetworkVariableWritePermission.Server);

    private void Awake()
    {
        _netNumWood.OnValueChanged += UpdateWoodText;
        _netNumStone.OnValueChanged += UpdateStoneText;
        _netNumPlant.OnValueChanged += UpdatePlantsText;
    }

    public override void OnNetworkSpawn()
    {
        _woodNumText.text = "Wood: " + _netNumWood.Value;
        _stoneNumText.text = "Stone: " + _netNumStone.Value;
        _plantNumText.text = "Plant: " + _netNumPlant.Value;
    }

    // ================== Text ==================
    private void UpdateWoodText(int prev, int next)
    {
        _woodNumText.text = "Wood: " + next;
    }

    private void UpdateStoneText(int prev, int next)
    {
        _stoneNumText.text = "Stone: " + next;
    }

    private void UpdatePlantsText(int prev, int next)
    {
        _plantNumText.text = "Plant: " + next;
    }


    // ================== Interface ==================
    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (cardToPlay.GetCardType() == _cardTypeAccepted)
            return true;

        return false;
    }


    // ================== Functions ==================
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
