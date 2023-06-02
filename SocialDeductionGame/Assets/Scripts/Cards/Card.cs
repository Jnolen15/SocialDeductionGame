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
        Food
    }
    [SerializeField] private CardType _cardType;

    [Header("Card Visual Prefab")]
    [SerializeField] private GameObject _cardPrefab;

    // ========== Getters ==========
    public int GetCardID()
    {
        return _cardID;
    }

    public string GetCardName()
    {
        return _cardName;
    }


    // ========== Card Functionality ==========
    public void Setup()
    {
        GameObject cardVisual = Instantiate(_cardPrefab, transform);
        cardVisual.GetComponent<CardVisual>().Setup(_cardName, _cardDescription);
    }


    // ========== OVERRIDE CLASSES ==========
    public abstract void OnPlay();
}
