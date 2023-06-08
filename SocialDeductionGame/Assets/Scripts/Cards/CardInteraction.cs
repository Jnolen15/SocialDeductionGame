using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardInteraction : MonoBehaviour,
    IPointerEnterHandler,
    //IPointerExitHandler,
    IDragHandler,
    IBeginDragHandler,
    IEndDragHandler
{
    [SerializeField] private Card _card;
    [SerializeField] private HandManager _handManager;
    [SerializeField] private PlayerController _playerController;

    void Start()
    {
        _card = this.GetComponentInParent<Card>();
        _handManager = this.GetComponentInParent<HandManager>();
        _playerController = this.GetComponentInParent<PlayerController>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"Mouse entered card {_card.GetCardName()}");
    }

    // =============== Drag ===============
    #region Card Drag
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"Card {_card.GetCardName()} begin drag");
    }

    public void OnDrag(PointerEventData eventData)
    {
        //Debug.Log("Dragging");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _playerController.TryCardPlay(_card);

        Debug.Log($"Card {_card.GetCardName()} end drag");
    }
    #endregion
}
