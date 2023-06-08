using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class Campfire : NetworkBehaviour, ICardPlayable
{
    [SerializeField] private Card.CardType _cardTypeAccepted;

    [SerializeField] private TextMeshPro _servingsText;

    [SerializeField] private NetworkVariable<float> _netServingsStored = new(writePerm: NetworkVariableWritePermission.Server);

    private void Awake()
    {
        _netServingsStored.OnValueChanged += UpdateServingsText;
    }

    public override void OnNetworkSpawn()
    {
        _servingsText.text = "Servings: " + _netServingsStored.Value;
    }

    private void OnDisable()
    {
        _netServingsStored.OnValueChanged -= UpdateServingsText;
    }

    // ================== Text ==================
    private void UpdateServingsText(float prev, float next)
    {
        _servingsText.text = "Servings: " + next;
    }

    // ================== Interface ==================
    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (cardToPlay.GetCardType() == _cardTypeAccepted)
            return true;

        return false;
    }

    // ================== Functions ==================
    public void AddFood(float servings)
    {
        AddFoodServerRpc(servings);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddFoodServerRpc(float servings)
    {
        _netServingsStored.Value += servings;
    }
}
