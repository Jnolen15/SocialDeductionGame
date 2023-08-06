using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardPicker : MonoBehaviour
{
    // Parameters
    [Header("Parameters")]
    [SerializeField] private CardDropTable _cardDropTable = new CardDropTable();
    [SerializeField] private int _cardsDelt;
    [SerializeField] private int _cardsChosen;
    // Refrences
    [Header("Refrences")]
    private CardManager _cardManager;
    [SerializeField] private Transform _cardCanvas;
    [SerializeField] private GameObject _takeButton;
    [SerializeField] private GameObject _requirementText;
    // Variables
    [Header("Variables")]
    [SerializeField] private List<Card> _chosenCards = new();

    void OnValidate()
    {
        _cardDropTable.ValidateTable();
    }

    private void OnEnable()
    {
        _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();
        _cardDropTable.ValidateTable();
        DealCards();
    }

    public void DealCards()
    {
        if(_cardManager == null)
            _cardManager = GameObject.FindGameObjectWithTag("CardManager").GetComponent<CardManager>();

        Debug.Log("Dealing cards");
        for(int i = 0; i < _cardsDelt; i++)
        {
            // Pick card
            int cardID = _cardDropTable.PickCardDrop();

            // Put card on screen
            Card newCard = Instantiate(CardDatabase.GetCard(cardID), _cardCanvas).GetComponent<Card>();
            newCard.SetupSelectable();
        }
    }

    public bool SelectCard(Card card)
    {
        if (_chosenCards.Count < _cardsChosen)
        {
            _chosenCards.Add(card);
            UpdateUI();
            return true;
        }
        else
            return false;
    }

    public void DeselectCard(Card card)
    {
        if (_chosenCards.Contains(card))
            _chosenCards.Remove(card);

        UpdateUI();
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

    private void UpdateUI()
    {
        if (_chosenCards.Count != _cardsChosen)
        {
            _takeButton.SetActive(false);
            _requirementText.SetActive(true);
        } else
        {
            _takeButton.SetActive(true);
            _requirementText.SetActive(false);
        }
    }
}
