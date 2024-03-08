using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class NightEventPreview : MonoBehaviour
{
    // =================== Refrences ===================
    [Header("large Card Refrences")]
    [SerializeField] private NightEventCardVisual _largeEventCard;
    [SerializeField] private Transform _startingPostion;
    [SerializeField] private Transform _midPostion;
    [SerializeField] private Transform _endPostion;
    [Header("Preview Refrences")]
    [SerializeField] private GameObject _eventPreviewPage;
    [SerializeField] private NightEventCardVisual _previewEventCard;
    [SerializeField] private GameObject _passedText;
    [SerializeField] private GameObject _failedText;
    private int _currentNightEventID;

    // =================== Setup ===================
    private void OnEnable()
    {
        //GameManager.OnStateMorning += DisplayLargeEvent;
        GameManager.OnStateMorning += Show;
        GameManager.OnStateNight += ClearEventResults;
        TabButtonUI.OnEventPressed += ToggleShow;
    }

    private void OnDisable()
    {
        //GameManager.OnStateMorning -= DisplayLargeEvent;
        GameManager.OnStateMorning -= Show;
        GameManager.OnStateNight -= ClearEventResults;
        TabButtonUI.OnEventPressed -= ToggleShow;
    }

    // =================== UI ===================
    private void ToggleShow()
    {
        if (GameManager.Instance.IsCurrentState(GameManager.GameState.Evening))
            return;

        _eventPreviewPage.SetActive(!_eventPreviewPage.activeSelf);
    }

    public void Show()
    {
        _eventPreviewPage.SetActive(true);
    }

    public void Hide()
    {
        _eventPreviewPage.SetActive(false);
    }

    // =================== Event Info ===================
    #region Event Info
    public void SetEvent(int eventID, int playerNum)
    {
        Debug.Log("Updating event UI info");

        _currentNightEventID = eventID;

        UpdateEventCard(playerNum);
    }

    private void UpdateEventCard(int playerNum)
    {
        _previewEventCard.Setup(_currentNightEventID, playerNum);
        _largeEventCard.Setup(_currentNightEventID, playerNum);
    }

    private void DisplayLargeEvent()
    {
        _largeEventCard.gameObject.SetActive(true);
        Transform cardTrans = _largeEventCard.transform;
        cardTrans.position = _startingPostion.position;

        Sequence LargeEventSequence = DOTween.Sequence();
        LargeEventSequence.Append(cardTrans.DOMove(_midPostion.position, 1))
          .AppendInterval(2)
          .Append(cardTrans.DOMove(_endPostion.position, 0.2f))
          .AppendCallback(() => _largeEventCard.gameObject.SetActive(false));
    }

    public void SetEventResults(bool passed)
    {
        if (passed)
            _passedText.SetActive(true);
        else
            _failedText.SetActive(true);
    }

    private void ClearEventResults()
    {
        _passedText.SetActive(false);
        _failedText.SetActive(false);
    }
    #endregion
}
