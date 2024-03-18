using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCardSetup : MonoBehaviour
{
    [SerializeField] private List<Card> _cardsUI;
    [SerializeField] private List<Card> _cardsPlayable;
    [SerializeField] private List<HazardCardVisual> _hazards;
    [SerializeField] private GameObject _cardSlotPref;
    [SerializeField] private Transform _handZone;
    [SerializeField] private List<CardSlotUI> _cardList;

    void Start()
    {
        foreach (Card card in _cardsUI)
        {
            card.SetupUI();
        }

        foreach (Card card in _cardsPlayable)
        {
            card.SetupPlayable();
        }

        int hazardID = 1001;
        foreach (HazardCardVisual hazard in _hazards)
        {
            hazard.Setup(hazardID);

            hazardID++;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            MakeCard(1001);

        if (Input.GetKeyDown(KeyCode.G))
            MakeCard(4001);
        
        if (Input.GetKeyDown(KeyCode.H))
            DiscardCard();
    }

    private void MakeCard(int cardID)
    {
        CardSlotUI slot = CreateNewCardSlot();

        Card newCard = Instantiate(CardDatabase.Instance.GetCard(cardID), slot.transform).GetComponent<Card>();
        newCard.SetupPlayable();
        slot.SlotCard(newCard);

        _cardList.Add(slot);
    }

    private void DiscardCard()
    {
        CardSlotUI cardToRemove = _cardList[0];

        _cardList.Remove(cardToRemove);
        cardToRemove.RemoveCard();
    }

    private CardSlotUI CreateNewCardSlot()
    {
        CardSlotUI newSlot = Instantiate(_cardSlotPref, _handZone).GetComponent<CardSlotUI>();
        return newSlot;
    }
}
