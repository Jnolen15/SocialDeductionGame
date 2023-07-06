using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class Stockpile : NetworkBehaviour, ICardPlayable
{
    // ================== Refrences ==================
    [SerializeField] private TextMeshPro _numCards;

    // ================== Variables ==================
    [SerializeField] private bool _acceptingCards;
    [SerializeField] private List<int> _stockpileCardIDs = new();
    [SerializeField] private NetworkVariable<int> _netCardsInStockpile = new(writePerm: NetworkVariableWritePermission.Server);

    // ================== Setup ==================
    public override void OnNetworkSpawn()
    {
        _netCardsInStockpile.OnValueChanged += UpdateCardsText;

        GameManager.OnStateAfternoon += ToggleAcceptingCards;
        GameManager.OnStateEvening += ToggleAcceptingCards;
    }

    private void OnDisable()
    {
        _netCardsInStockpile.OnValueChanged -= UpdateCardsText;

        GameManager.OnStateAfternoon -= ToggleAcceptingCards;
        GameManager.OnStateEvening -= ToggleAcceptingCards;
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
        return _acceptingCards;
    }


    // ================== Functions ==================
    // only accepts cards during afternoon phase
    private void ToggleAcceptingCards()
    {
        _acceptingCards = !_acceptingCards;
    }

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

        //Debug.Log("Pop: " + topCard);

        return topCard;
    }
}
