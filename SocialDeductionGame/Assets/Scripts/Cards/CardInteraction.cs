using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardInteraction : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IDragHandler,
    IBeginDragHandler,
    IEndDragHandler
{
    // ================== Events / Refrences ==================
    [SerializeField] private GameObject _dragIcon;
    [SerializeField] private Card _card;
    [SerializeField] private HandManager _handManager;
    [SerializeField] private PlayerCardManager _playerCardManager;
    private PlayRandomSound _randSound;
    private GameObject _indicator;
    private bool _dragging;
    private bool _dontHighlight;    // Cards should not highlight if another card is being highlighted

    public delegate void CardHighlightAction(Card cardHighlighted);
    public static event CardHighlightAction OnCardHighlighted;
    public static event CardHighlightAction OnCardUnhighlighted;

    // =============== Setup ===============
    void Start()
    {
        _card = this.GetComponentInParent<Card>();
        _handManager = this.GetComponentInParent<HandManager>();
        _playerCardManager = this.GetComponentInParent<PlayerCardManager>();
        _randSound = this.GetComponent<PlayRandomSound>();

        OnCardHighlighted += OtherCardHighlighted;
        OnCardUnhighlighted += OtherCardUnhighlighted;
        GameManager.OnStateChange += LetGo;
    }

    private void OnDestroy()
    {
        OnCardHighlighted -= OtherCardHighlighted;
        OnCardUnhighlighted -= OtherCardUnhighlighted;
        GameManager.OnStateChange -= LetGo;
    }

    // =============== Interaction ===============
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_dontHighlight)
        {
            OnCardHighlighted?.Invoke(_card);

            if (_randSound)
                _randSound.PlayRandom();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(!_dragging && !_dontHighlight)
            OnCardUnhighlighted?.Invoke(_card);
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

        if (_randSound)
            _randSound.PlayRandom();
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

        LetGo();
    }

    private void LetGo()
    {
        Destroy(_indicator);
        _indicator = null;
        _dragging = false;

        if (!_dontHighlight)
            OnCardUnhighlighted?.Invoke(_card);
    }

    private void LetGo(GameManager.GameState prev, GameManager.GameState current)
    {
        LetGo();
    }

    private void OtherCardHighlighted(Card otherCard)
    {
        if (otherCard != _card)
            _dontHighlight = true;
    }

    private void OtherCardUnhighlighted(Card otherCard)
    {
        _dontHighlight = false;
    }
    #endregion
}
