using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardSelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // ============= Refrences / Variables =============
    private Card _card;
    private Forage _cardPicker;

    // ============= Setup =============
    void Start()
    {
        _card = GetComponentInParent<Card>();
        _cardPicker = GetComponentInParent<Forage>();
    }

    // ============= Functions =============
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }

    public void OnSelect()
    {
        _cardPicker.SelectCard(_card);
    }
}
