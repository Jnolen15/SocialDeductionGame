using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Sirenix.OdinInspector;

public class HandAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // =================== Refrences ===================
    [Header("Positioning")]
    [SerializeField] private RectTransform _cards;
    [SerializeField] private Transform _leftMost;
    [SerializeField] private Transform _rightMost;
    [SerializeField] private float _tilt;
    [SerializeField] private float _height;
    private int _cardsHeld;
     private int _highlightedCardChildCount;

    [Header("Minimizing")]
    [SerializeField] private Transform _hand;
    [SerializeField] private float _maximizedCardHeight;
    private float _minimizedHeight;
    [SerializeField] private bool _minimized;
    [SerializeField] private bool _hovering;
    [SerializeField] private float _bufferTimerMax;
    [SerializeField] private float _bufferTimer;

    // =================== Setup ===================
    private void Start()
    {
        SetCardPositions();

        _minimizedHeight = _cards.position.y;
    }

    // =================== Update ===================
    private void Update()
    {
        RunMinimizeTimer();

        if (_cardsHeld != _cards.childCount)
        {
            SetCardPositions();
        }
    }

    // =================== Card Positioning ===================
    #region Card Positioning
    private void SetCardPositions()
    {        
        _cardsHeld = _cards.childCount;

        float left = _leftMost.localPosition.x;
        float right = _rightMost.localPosition.x;
        float distance = (Mathf.Abs(left) + right);
        float offset = (Mathf.Abs(left));

        if (_cardsHeld == 0)
            return;

        float spacing = (distance / (_cardsHeld + 1));

        // Spacing
        int cardNum = 1;
        foreach (Transform slot in _cards)
        {
            float xPos = (spacing * cardNum) - offset;
            slot.localPosition = new Vector3(xPos, 0, 0);

            Debug.Log($"Placed card {cardNum} at {slot.localPosition.x}");

            cardNum++;
        }

        if (_cardsHeld < 3)
        {
            foreach (Transform slot in _cards)
            {
                CardSlotUI slotUI = slot.GetComponent<CardSlotUI>();
                slotUI.SetCardHandPosition(0, 0);
            }

            return;
        }

        // Tilt
        cardNum = 0 - (_cardsHeld / 2);
        foreach (Transform slot in _cards)
        {
            CardSlotUI slotUI = slot.GetComponent<CardSlotUI>();

            float tiltAmmount = (_tilt * cardNum);

            if (_cardsHeld % 2 == 0 && tiltAmmount == 0)
            {
                cardNum++;
                tiltAmmount = (_tilt * cardNum);
            }

            slotUI.SetCardHandPosition((Mathf.Abs(tiltAmmount) * _height), tiltAmmount);

            cardNum++;
        }
    }

    public void HighlightMe(Transform cardSlot)
    {
        _highlightedCardChildCount = cardSlot.GetSiblingIndex();

        cardSlot.SetAsLastSibling();
    }

    public void UnhighlightMe(Transform cardSlot)
    {
        cardSlot.SetSiblingIndex(_highlightedCardChildCount);
    }
    #endregion

    // =================== Minimizing ===================
    #region Minimizing
    private void RunMinimizeTimer()
    {
        if (_hovering)
            return;

        if (_bufferTimer > 0)
            _bufferTimer -= Time.deltaTime;
        else if (!_minimized)
            Minimize();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovering = true;
        _bufferTimer = _bufferTimerMax;

        if (_minimized)
            Maximize();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_minimized)
            return;

        _hovering = false;
        _bufferTimer = _bufferTimerMax;
    }

    private void Maximize()
    {
        _cards.DOKill();
        _minimized = false;

        _cards.DOAnchorPosY(_maximizedCardHeight, 0.1f);
    }

    private void Minimize()
    {
        _minimized = true;

        _cards.DOAnchorPosY(_minimizedHeight, 0.3f).SetEase(Ease.InOutSine);
    }
    #endregion
}
