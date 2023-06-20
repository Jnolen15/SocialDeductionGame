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
        RawFood,
        CookedFood
    }
    [SerializeField] private CardType _cardType;

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

    // ========== OVERRIDE CLASSES ==========
    public abstract void OnPlay(GameObject playLocation);
}
