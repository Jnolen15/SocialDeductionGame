using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class Stockpile : NetworkBehaviour, ICardPlayable
{
    [SerializeField] private TextMeshPro _numCards;

    [SerializeField] private List<int> _stockpileCardIDs = new();
    [SerializeField] private NetworkVariable<int> _netCardsInStockpile = new(writePerm: NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        _netCardsInStockpile.OnValueChanged += UpdateCardsText;
    }

    private void OnDisable()
    {
        _netCardsInStockpile.OnValueChanged -= UpdateCardsText;
    }

    // ================== Text ==================
    private void UpdateCardsText(int prev, int next)
    {
        _numCards.text = "Cards: " + next;
    }


    // ================== Interface ==================
    // Stockpile accepts any card types ATM
    public bool CanPlayCardHere(Card cardToPlay)
    {
        return true;
    }


    // ================== Functions ==================
    public void AddCard(int cardID)
    {
        AddCardsServerRpc(cardID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddCardsServerRpc(int cardID)
    {
        _stockpileCardIDs.Add(cardID);
        _netCardsInStockpile.Value++;
    }

    public int GetNumCards()
    {
        return _netCardsInStockpile.Value;
    }

    // Gets and removes the top card of teh stockpile
    public int GetTopCard()
    {
        if (_stockpileCardIDs.Count <= 0)
            return -1;

        int topCard = _stockpileCardIDs[0];
        _stockpileCardIDs.RemoveAt(0);
        _netCardsInStockpile.Value--;

        Debug.Log("Pop: " + topCard);

        return topCard;
    }
}
