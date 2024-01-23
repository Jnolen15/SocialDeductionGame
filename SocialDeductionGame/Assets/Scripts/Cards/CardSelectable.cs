using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardSelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // ============= Refrences / Variables =============
    private Card _card;
    private ICardPicker _cardPicker;
    private PlayRandomSound _randSound;

    // ============= Setup =============
    void Start()
    {
        _card = GetComponentInParent<Card>();
        _cardPicker = GetComponentInParent<ICardPicker>();
        _randSound = this.GetComponent<PlayRandomSound>();
    }

    // ============= Functions =============
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_randSound)
            _randSound.PlayRandom();

        transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }

    public void OnSelect()
    {
        if (_randSound)
            _randSound.PlayRandom();

        _cardPicker.PickCard(_card);
    }
}
