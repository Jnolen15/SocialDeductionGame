using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Card : MonoBehaviour
{
    [Header("Card ID Type -, Card ---")]
    [SerializeField] private int _cardID;

    [Header("Card Details")]
    [SerializeField] private string _cardName;
    [TextArea]
    [SerializeField] private string _cardDescription;
    [SerializeField] private Sprite _cardArt;

    public enum CardType
    {
        Resource,
        Food,
        Meal
    }
    [SerializeField] private CardType _cardType;

    public enum CardSubType
    {
        None,
        Wood,
        Stone,
        Plant
    }
    [SerializeField] private CardSubType _cardSubType;

    [Header("Card Prefabs")]
    [SerializeField] private GameObject _cardPlayablePrefab;
    [SerializeField] private GameObject _cardUIPrefab;

    // ========== Getters ==========
    public int GetCardID()
    {
        return _cardID;
    }

    public string GetCardName()
    {
        return _cardName;
    }

    public CardType GetCardType()
    {
        return _cardType;
    }

    public CardSubType GetCardSubType()
    {
        return _cardSubType;
    }


    // ========== Card Functionality ==========
    public void SetupPlayable()
    {
        GameObject cardVisual = Instantiate(_cardPlayablePrefab, transform);
        cardVisual.GetComponent<CardVisual>().Setup(_cardName, _cardDescription);
    }

    public void SetupUI()
    {
        GameObject cardVisual = Instantiate(_cardUIPrefab, transform);
        cardVisual.GetComponent<CardVisual>().Setup(_cardName, _cardDescription);
    }

    // Adding a card to the Stockpile which contributes to night events
    // All cards can be played to stockpile with same functionality, so doing it here.
    public void PlayToStockpile(Stockpile stockpile)
    {
        if (stockpile != null)
        {
            Debug.Log("Playng card to stockpile: " + GetCardSubType());
            stockpile.AddCard(GetCardID());
        }
        else
            Debug.LogError("Card was played on a location it can't do anything with");
    }

    // ========== OVERRIDE CLASSES ==========
    public abstract void OnPlay(GameObject playLocation);
}
