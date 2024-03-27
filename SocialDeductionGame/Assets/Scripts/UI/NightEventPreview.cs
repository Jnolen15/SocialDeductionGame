using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class NightEventPreview : MonoBehaviour
{
    // =================== Refrences ===================
    [Header("Preview Refrences")]
    [SerializeField] private GameObject _eventPreviewPage;
    [SerializeField] private NightEventCardVisual _previewEventCard;
    [SerializeField] private GameObject _passedText;
    [SerializeField] private GameObject _failedText;
    private int _currentNightEventID;

    // =================== Setup ===================
    private void OnEnable()
    {
        GameManager.OnStateMorning += Show;
        GameManager.OnStateNight += ClearEventResults;
        TabButtonUI.OnEventPressed += ToggleShow;
    }

    private void OnDisable()
    {
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
