using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Card : MonoBehaviour
{
    [Header("Card ID Type -, Card ---")]
    [SerializeField] protected int _cardID;

    [Header("Card Details")]
    [SerializeField] protected string _cardName;
    [TextArea]
    [SerializeField] protected string _cardDescription;
    [SerializeField] protected Sprite _cardArt;
    [SerializeField] protected List<CardTag> _tags;

    [Header("Card Prefabs")]
    [SerializeField] protected GameObject _cardPlayablePrefab;
    [SerializeField] protected GameObject _cardSelectablePrefab;
    [SerializeField] protected GameObject _cardUIPrefab;

    // ========== Getters ==========
    public int GetCardID()
    {
        return _cardID;
    }

    public string GetCardName()
    {
        return _cardName;
    }

    public List<CardTag> GetCardTags()
    {
        return new List<CardTag>(_tags);
    }

    // ========== Tags ==========
    public bool HasTag(CardTag t)
    {
        return _tags.Contains(t);
    }

    public bool HasTag(string tagName)
    {
        return _tags.Exists(t => t.Name.Equals(tagName, System.StringComparison.CurrentCultureIgnoreCase));
    }

    // Returns true if any tag in the given list matches any tag in this cards tags
    public bool HasAnyTag(List<CardTag> tags)
    {
        foreach(CardTag localTag in _tags)
        {
            foreach (CardTag givenTag in tags)
            {
                if (givenTag == localTag)
                    return true;
            }
        }

        return false;
    }

    // ========== Card Functionality ==========
    // Playable card for in the playerss hand
    public virtual void SetupPlayable()
    {
        GameObject cardVisual = Instantiate(_cardPlayablePrefab, transform);
        cardVisual.GetComponent<CardVisual>().Setup(_cardName, _cardDescription, _cardArt, _tags);
    }

    // Selectable card for foraging
    public virtual void SetupSelectable()
    {
        GameObject cardVisual = Instantiate(_cardSelectablePrefab, transform);
        cardVisual.GetComponent<CardVisual>().Setup(_cardName, _cardDescription, _cardArt, _tags);
    }

    // Visual card for non-interactable UI
    public virtual void SetupUI()
    {
        GameObject cardVisual = Instantiate(_cardUIPrefab, transform);
        cardVisual.GetComponent<CardVisual>().Setup(_cardName, _cardDescription, _cardArt, _tags);
    }

    // Adding a card to the Stockpile which contributes to night events
    // All cards can be played to stockpile with same functionality, so doing it here.
    public void PlayToStockpile(Stockpile stockpile)
    {
        if (stockpile != null)
        {
            Debug.Log("Playng card to stockpile: " + _cardName);
            stockpile.AddCard(GetCardID(), PlayerConnectionManager.Instance.GetLocalPlayersID());
        }
        else
            Debug.LogError("Card was played on a location it can't do anything with");
    }

    // ========== OVERRIDE CLASSES ==========
    public abstract void OnPlay(GameObject playLocation);
}
