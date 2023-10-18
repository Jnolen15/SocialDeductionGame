using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NightEventThumbnail : MonoBehaviour
{
    // =================== Refrences ===================
    [Header("Event Refrences")]
    [SerializeField] private GameObject _eventTagIconPref;
    [SerializeField] private TextMeshProUGUI _eventThumbnailTitle;
    [SerializeField] private TextMeshProUGUI _eventThumbnailRequiredNum;
    [SerializeField] private Transform _eventThumbnailTagIconSlot;
    [SerializeField] private GameObject _expandIcon;
    [SerializeField] private GameObject _minimizeIcon;
    [SerializeField] private GameObject _passedText;
    [SerializeField] private GameObject _failedText;
    [SerializeField] private NightEventCardVisual _eventCardSmall;
    private int _currentNightEventID;
    private bool _expanded;

    // =================== Setup ===================
    private void OnEnable()
    {
        GameManager.OnStateNight += ClearEventResults;
    }

    private void OnDisable()
    {
        GameManager.OnStateNight -= ClearEventResults;
    }


    // =================== Event Info ===================
    #region Event Info
    public void SetEvent(int eventID, int playerNum)
    {
        Debug.Log("Updating event UI info");

        _currentNightEventID = eventID;

        UpdateEventThumbnail(playerNum);
        UpdateEventCard(playerNum);
    }

    private void UpdateEventThumbnail(int playerNum)
    {
        // Clear tags (in case of reused card assets)
        foreach (Transform t in _eventThumbnailTagIconSlot)
        {
            if (t != _eventThumbnailTagIconSlot.GetChild(0))
                Destroy(t.gameObject);
        }

        // Setup new
        NightEvent eventData = CardDatabase.Instance.GetEvent(_currentNightEventID);
        _eventThumbnailTitle.text = eventData.GetEventName();
        _eventThumbnailRequiredNum.text = eventData.GetSuccessPoints(playerNum) + " = ";
        foreach (CardTag t in eventData.GetRequiredCardTags())
        {
            TagIcon icon = Instantiate(_eventTagIconPref, _eventThumbnailTagIconSlot).GetComponent<TagIcon>();
            icon.SetupIcon(t.visual, t.name);
        }
    }

    private void UpdateEventCard(int playerNum)
    {
        _eventCardSmall.Setup(_currentNightEventID, playerNum);
    }

    public void ToggleExpanded()
    {
        _expanded = !_expanded;

        _eventCardSmall.gameObject.SetActive(_expanded);

        if (!_expanded)
        {
            _expandIcon.SetActive(true);
            _minimizeIcon.SetActive(false);
        }
        else
        {
            _expandIcon.SetActive(false);
            _minimizeIcon.SetActive(true);
        }
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
