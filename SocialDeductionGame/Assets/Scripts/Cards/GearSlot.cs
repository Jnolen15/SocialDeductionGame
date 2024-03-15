using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

public class GearSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ICardUIPlayable
{
    // ============== Refrences / Variables ==============
    [Header("Visuals")]
    [SerializeField] private GameObject _pocketClosed;
    [SerializeField] private GameObject _pocketOpenFront;
    [SerializeField] private GameObject _pocketOpenBack;
    [SerializeField] private RectTransform _cardZone;
    [SerializeField] private TextMeshProUGUI _equipMessage;
    [SerializeField] private float _maximizedPocketHeight;
    [SerializeField] private float _maximizedCardHeight;
    private float _minimizedHeight;
    [SerializeField] private float _bufferTimerMax;
    private float _bufferTimer;
    private bool _minimized = true;
    private bool _hovering;
    private RectTransform _rect;
    [Header("Info")]
    [SerializeField] private int _gearSlot;
    [SerializeField] private Gear _currentGearCard;
    private PlayerCardManager _pcm;

    // ============== Setup ==============
    #region Setup
    private void Start()
    {
        _pcm = this.GetComponentInParent<PlayerCardManager>();

        _rect = this.GetComponent<RectTransform>();
        _minimizedHeight = _rect.position.y;

        CardInteraction.OnCardHighlighted += OnCardHighlighted;
        CardInteraction.OnCardUnhighlighted += OnCardUnhighlighted;
    }

    private void OnDestroy()
    {
        CardInteraction.OnCardHighlighted -= OnCardHighlighted;
        CardInteraction.OnCardUnhighlighted -= OnCardUnhighlighted;
    }
    #endregion

    // ============== Slot Function ==============
    #region Slot Function
    public Gear EquipGearCard(int gearID)
    {
        GameObject newGear = Instantiate(CardDatabase.Instance.GetCard(gearID), _cardZone);
        _currentGearCard = newGear.GetComponent<Gear>();
        _currentGearCard.SetupUI();

        OpenPocketVisuals();
        _hovering = false;
        Minimize(false);

        return _currentGearCard;
    }

    public void UnequipGearCard()
    {
        ClosedPocketVisuals();
        _cardZone.DOKill();
        _rect.DOKill();
        Minimize(false);

        Destroy(_currentGearCard.gameObject);

        _currentGearCard = null;
    }

    public void GearBreak(int gearID)
    {
        _pcm.UnequipGear(_gearSlot, gearID);
    }

    private bool HasGear()
    {
        return _currentGearCard;
    }
    #endregion

    // ============== ICardUIPlayable ==============
    #region UIPlayable Interface
    public bool CanPlayCardHere(Card cardToPlay)
    {
        if (cardToPlay.HasTag("Gear"))
        {
            return true;
        }
        else
        {
            Debug.Log("Card is not gear, can't equip");
            return false;
        }
    }

    public void PlayCardHere(int cardID)
    {
        _pcm.EquipGear(_gearSlot, cardID);
    }

    #endregion

    // ============== UI Visuals ==============
    #region UI Visuals
    private void Update()
    {
        if (_hovering)
            return;

        if (_bufferTimer > 0)
            _bufferTimer -= Time.deltaTime;
        else if (!_minimized)
            Minimize(HasGear());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovering = true;
        _bufferTimer = _bufferTimerMax;

        if (_minimized)
            Maximize(HasGear());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_minimized)
            return;

        _hovering = false;
        _bufferTimer = _bufferTimerMax;
    }

    private void OnCardHighlighted(Card cardHighlighted)
    {
        if (!cardHighlighted.HasTag("Gear"))
            return;

        _cardZone.DOKill();
        _rect.DOKill();

        _equipMessage.gameObject.SetActive(true);

        if (HasGear())
        {
            Minimize(true);
            _hovering = true;
            Maximize(false);
            _equipMessage.text = "Replace " + _currentGearCard.GetCardName();
        }
        else
        {
            _hovering = true;
            Maximize(false);
            _equipMessage.text = "Equip Gear to Pocket";
        }
    }

    private void OnCardUnhighlighted(Card cardHighlighted)
    {
        _hovering = false;
        Minimize(false);
        _equipMessage.gameObject.SetActive(false);
    }

    private void Maximize(bool hasGear)
    {
        _minimized = false;

        // lift gear out of pocket
        if (hasGear)
        {
            _cardZone.DOAnchorPosY(_maximizedCardHeight, 0.25f).SetEase(Ease.OutBack, 3);
            _pocketOpenFront.transform.SetSiblingIndex(1);
        }
        // maximise entire pocket
        else
        {
            _rect.DOAnchorPosY(_maximizedPocketHeight, 0.1f);
        }
    }

    private void Minimize(bool hasGear)
    {
        _minimized = true;

        // lift gear out of pocket
        if (hasGear)
        {
            _cardZone.DOAnchorPosY(_minimizedHeight, 0.3f).SetEase(Ease.InBack, 3);
            _pocketOpenFront.transform.SetSiblingIndex(2);
        }
        // maximise entire pocket
        else
        {
            _rect.DOAnchorPosY(_minimizedHeight, 0.3f).SetEase(Ease.InOutSine);
        }
    }

    private void OpenPocketVisuals()
    {
        _pocketClosed.SetActive(false);
        _pocketOpenBack.SetActive(true);
        _pocketOpenFront.SetActive(true);
    }

    private void ClosedPocketVisuals()
    {
        _pocketClosed.SetActive(true);
        _pocketOpenBack.SetActive(false);
        _pocketOpenFront.SetActive(false);
    }
    #endregion
}
