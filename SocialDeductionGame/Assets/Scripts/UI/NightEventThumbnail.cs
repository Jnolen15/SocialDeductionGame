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
        GameManager.OnStateMorning += Expand;
        GameManager.OnStateNight += ClearEventResults;
    }

    private void OnDisable()
    {
        GameManager.OnStateMorning -= Expand;
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
            Destroy(t.gameObject);
        }

        // Setup new
        NightEvent eventData = CardDatabase.Instance.GetEvent(_currentNightEventID);
        _eventThumbnailTitle.text = eventData.GetEventName();
        
        Vector2 requirements = eventData.GetRequirements(playerNum);
        CardTag primaryTag = eventData.GetPrimaryResource();
        GameObject primaryResource = Instantiate(_eventTagIconPref, _eventThumbnailTagIconSlot);
        primaryResource.GetComponentInChildren<TagIcon>().SetupIcon(primaryTag.visual, primaryTag.name);
        primaryResource.GetComponentInChildren<TextMeshProUGUI>().text = requirements.x.ToString();

        if (requirements.y > 0)
        {
            CardTag secondaryTag = eventData.GetSecondaryResource();
            GameObject secondaryResource = Instantiate(_eventTagIconPref, _eventThumbnailTagIconSlot);
            secondaryResource.GetComponentInChildren<TagIcon>().SetupIcon(secondaryTag.visual, secondaryTag.name);
            secondaryResource.GetComponentInChildren<TextMeshProUGUI>().text = requirements.y.ToString();
        }
    }

    private void UpdateEventCard(int playerNum)
    {
        _eventCardSmall.Setup(_currentNightEventID, playerNum);
    }

    private void Expand()
    {
        _expanded = false;
        ToggleExpanded();
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
