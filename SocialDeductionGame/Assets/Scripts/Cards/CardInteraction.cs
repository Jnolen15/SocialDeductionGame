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
    // ================== Events / Refrences ==================
    [SerializeField] private GameObject _dragIcon;
    [SerializeField] private Card _card;
    [SerializeField] private HandManager _handManager;
    [SerializeField] private PlayerController _playerController;
    private GameObject _indicator;

    // =============== Setup ===============
    void Start()
    {
        _card = this.GetComponentInParent<Card>();
        _handManager = this.GetComponentInParent<HandManager>();
        _playerController = this.GetComponentInParent<PlayerController>();
    }

    // =============== Interaction ===============
    public void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log($"Mouse entered card {_card.GetCardName()}");
    }

    // =============== Drag ===============
    #region Card Drag
    public void OnBeginDrag(PointerEventData eventData)
    {
        Canvas parentCanvas = gameObject.GetComponentInParent<Canvas>();
        _indicator = Instantiate(_dragIcon, parentCanvas.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _indicator.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _playerController.TryCardPlay(_card);

        Destroy(_indicator);
        _indicator = null;
    }
    #endregion
}
