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
    private bool isMoving;

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
        StartCoroutine(RemoveCardCoroutine());
    }

    IEnumerator RemoveCardCoroutine()
    {
        transform.DOKill();
        _cardTransform.DOKill();
        _handAnimator.UnparentMe(transform);
        _cardTransform.rotation = Quaternion.Euler(Vector3.zero);

        _cardTransform.DOAnchorPosY(_cardMaximizedHeight * 3, 0.25f).SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(0.2f);

        HeldCard.GetComponentInChildren<CanvasGroup>().DOFade(0f, 0.15f);
        _cardTransform.DOShakeAnchorPos(0.1f, 10, 2);

        yield return new WaitForSeconds(0.2f);

        transform.DOKill();
        _cardTransform.DOKill();
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
    #region Animating
    public void SetSlotPosition(Vector2 newPos)
    {
        transform.DOLocalMove(newPos, 0.2f).SetEase(Ease.OutSine);
    }

    public void SetCardHandPosition(float horizontal, float tilt)
    {
        _minimizedHeight = horizontal;
        _tiltRotation = tilt;

        isMoving = true;

        _cardTransform.DORotate(new Vector3(0, 0, tilt), 0.1f);
        _cardTransform.DOLocalMove(new Vector3(0, horizontal, 0), 0.1f).OnComplete(() => { isMoving = false; });
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isMoving)
            return;

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
    #endregion
}
