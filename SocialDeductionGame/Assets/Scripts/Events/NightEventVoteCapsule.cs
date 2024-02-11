using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NightEventVoteCapsule : MonoBehaviour
{
    // ================== Refrences / Variables ==================
    [Header("Event Details")]
    [SerializeField] private GameObject _eventTagIconPref;
    [SerializeField] private Image _eventArt;
    [SerializeField] private TextMeshProUGUI _eventTitle;
    [SerializeField] private TextMeshProUGUI _eventConsequences;
    [SerializeField] private TextMeshProUGUI _eventBonuses;
    [SerializeField] private Transform _eventTagIconSlot;
    private int _heldEventID;

    [Header("Picker Refrences")]
    [SerializeField] private TextMeshProUGUI _voteText;
    private NightEventPicker _eventPicker;
    private PlayRandomSound _randSound;
    private bool _eventSelected;

    // ================== Setup ==================
    #region Setup
    private void OnEnable()
    {
        GetRefrences();
    }

    private void GetRefrences()
    {
        _eventPicker = GetComponentInParent<NightEventPicker>();

        _randSound = this.GetComponent<PlayRandomSound>();
    }

    public void Setup(int eventID, int playerNum)
    {
        // Clear tags (in case of reused card assets)
        foreach (Transform t in _eventTagIconSlot)
        {
            Destroy(t.gameObject);
        }

        // Setup new
        _heldEventID = eventID;
        NightEvent eventData = CardDatabase.Instance.GetEvent(eventID);
        _eventArt.sprite = eventData.GetEventArt();
        _eventTitle.text = eventData.GetEventName();
        _eventConsequences.text = "Fail: " + eventData.GetEventConsequences();
        _eventBonuses.text = $"Bonus: {eventData.GetEventBonuses()}";

        CardTag primaryTag = eventData.GetPrimaryResource();
        GameObject primaryResource = Instantiate(_eventTagIconPref, _eventTagIconSlot);
        primaryResource.GetComponent<TagIcon>().SetupIcon(primaryTag.visual, primaryTag.name);

        CardTag secondaryTag = eventData.GetSecondaryResource();
        GameObject secondaryResource = Instantiate(_eventTagIconPref, _eventTagIconSlot);
        secondaryResource.GetComponent<TagIcon>().SetupIcon(secondaryTag.visual, secondaryTag.name);
    }
    #endregion

    // ================== Function ==================
    #region Function
    public void OnSelect()
    {
        if (_eventPicker == null)
            GetRefrences();

        if (!_eventSelected)
        {
            _eventSelected = true;
            _eventPicker.SelectEvent(_heldEventID);
            transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);

            if (_randSound)
                _randSound.PlayRandom();
        }
    }

    public void UpdateVotes(int numVotes)
    {
        _voteText.text = numVotes.ToString();
    }

    public void Deselect()
    {
        _eventSelected = false;
        transform.localScale = Vector3.one;
    }
    #endregion
}
