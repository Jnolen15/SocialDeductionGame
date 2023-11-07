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
    [SerializeField] private PlayerCardManager _playerCardManager;
    private GameObject _indicator;
    private bool _dragging;

    // =============== Setup ===============
    void Start()
    {
        _card = this.GetComponentInParent<Card>();
        _handManager = this.GetComponentInParent<HandManager>();
        _playerCardManager = this.GetComponentInParent<PlayerCardManager>();
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
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            Debug.Log("Not primary click");
            return;
        }

        Canvas parentCanvas = gameObject.GetComponentInParent<Canvas>();
        _indicator = Instantiate(_dragIcon, parentCanvas.transform);
        _dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        _indicator.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        bool playedToUI = false;
        GameObject currentHover = eventData.pointerCurrentRaycast.gameObject;
        if (currentHover != null)
        {
            Debug.Log("Mouse Over: " + currentHover.name);
            if(currentHover.tag == "UICardPlayable")
            {
                // Play to UI
                Debug.Log("Playing card to UI");
                _playerCardManager.TryCardPlayToUI(_card, currentHover);
                playedToUI = true;
            }
        }

        if(!playedToUI)
            _playerCardManager.TryCardPlay(_card);

        Destroy(_indicator);
        _indicator = null;
        _dragging = false;
    }
    #endregion
}
