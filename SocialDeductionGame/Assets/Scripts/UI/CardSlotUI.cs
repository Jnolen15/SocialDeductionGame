using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // =================== Refrences ===================
    [SerializeField] private float _cardMaximizedHeight;
    private float _minimizedHeight;
    private float _tiltRotation;
    public Card HeldCard;
    private RectTransform _cardTransform;
    private bool _minimized = true;
    private HandAnimator _handAnimator;

    private void Start()
    {
        _handAnimator = GetComponentInParent<HandAnimator>();
    }

    // ================ Card ================
    #region Card
    public void SlotCard(Card newCard)
    {
        HeldCard = newCard;

        _cardTransform = HeldCard.GetComponent<RectTransform>();
    }

    public void RemoveCard()
    {
        Destroy(HeldCard.gameObject);
        Destroy(gameObject);
    }
    #endregion

    //================ Helpers ================
    #region Helpers
    public Card GetCard()
    {
        return HeldCard;
    }

    public bool HasCard()
    {
        if (HeldCard)
            return true;

        return false;
    }
    #endregion

    //================ Animating ================
    public void SetCardHandPosition(float horizontal, float tilt)
    {
        _minimizedHeight = horizontal;
        _tiltRotation = tilt;

        _cardTransform.rotation = Quaternion.Euler(new Vector3(0, 0, tilt));
        _cardTransform.localPosition = new Vector3(0, horizontal, 0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Maximize();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_minimized)
            return;

        Minimize();
    }

    private void Maximize()
    {
        _cardTransform.DOKill();
        _minimized = false;

        _handAnimator.HighlightMe(transform);

        _cardTransform.DOAnchorPosY(_cardMaximizedHeight, 0.1f);
        _cardTransform.DORotate(new Vector3(0, 0, 0), 0.1f);
        _cardTransform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.1f);
    }

    private void Minimize()
    {
        _minimized = true;

        _handAnimator.UnhighlightMe(transform);

        _cardTransform.DOAnchorPosY(_minimizedHeight, 0.3f).SetEase(Ease.InOutSine);
        _cardTransform.DORotate(new Vector3(0, 0, _tiltRotation), 0.3f).SetEase(Ease.InOutSine);
        _cardTransform.DOScale(new Vector3(1f, 1f, 1f), 0.3f).SetEase(Ease.InOutSine);
    }
}
