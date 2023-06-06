using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardSelectable : MonoBehaviour
{
    // Refrences
    private Card _card;
    private CardPicker _cardPicker;
    // Variables
    private bool _cardSelected;

    void Start()
    {
        _card = GetComponentInParent<Card>();
        _cardPicker = GetComponentInParent<CardPicker>();
    }

    public void OnSelect()
    {
        if (!_cardSelected)
        {
            _cardSelected = _cardPicker.SelectCard(gameObject);

            if (_cardSelected)
                transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }
        else
        {
            _cardPicker.DeselectCard(gameObject);
            _cardSelected = false;
            transform.localScale = Vector3.one;
        }
    }
}
