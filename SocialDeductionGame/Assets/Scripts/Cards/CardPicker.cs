using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardPicker : MonoBehaviour
{
    // Parameters
    [Header("Parameters")]
    [SerializeField] private List<int> _lootPoolList = new();
    [SerializeField] private int _cardsDelt;
    [SerializeField] private int _cardsChosen;
    // Refrences
    [Header("Refrences")]
    private CardDatabase _cardDB;
    private CardManager _cardManager;
    [SerializeField] private Transform _cardCanvas;
    // Variables
    [Header("Variables")]
    [SerializeField] private List<Card> _chosenCards = new();

    private void OnEnable()
    {
        _cardDB = GameObject.FindGameObjectWithTag("cardDB").GetComponent<CardDatabase>();
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
    }

    public void DealCards()
    {
        Debug.Log("dealing cards");
        for(int i = 0; i < _cardsDelt; i++)
        {
            // Pick card
            int cardID = _lootPoolList[Random.Range(0, _lootPoolList.Count)];

            // Put card on screen
            Card newCard = Instantiate(_cardDB.GetCard(cardID), _cardCanvas).GetComponent<Card>();
            newCard.SetupUI();
        }
    }

    public bool SelectCard(Card card)
    {
        if(_chosenCards.Count < _cardsChosen)
        {
            _chosenCards.Add(card);
            return true;
        }
        else
            return false;
    }

    public void DeselectCard(Card card)
    {
        if (_chosenCards.Contains(card))
            _chosenCards.Remove(card);
    }

    public void TakeCards()
    {
        int[] cardIDs = new int[_cardsChosen];
        for (int i = 0; i < _chosenCards.Count; i++)
        {
            cardIDs[i] = _chosenCards[i].GetCardID();
        }

        // Give cards to Card Manager
        _cardManager.GiveCards(cardIDs);

        // Clear lists
        _chosenCards.Clear();
        foreach (Transform child in _cardCanvas)
        {
            Destroy(child.gameObject);
        }
    }
}
