using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NightEventCardVisual : MonoBehaviour
{
    // ================== Refrences ==================
    [SerializeField] private GameObject _eventTagIconPref;
    [SerializeField] private Image _eventArt;
    [SerializeField] private TextMeshProUGUI _eventTitle;
    [SerializeField] private TextMeshProUGUI _eventConsequences;
    [SerializeField] private TextMeshProUGUI _eventBonuses;
    [SerializeField] private Transform _eventTagIconSlot;
    private int _heldEventID;

    // ================== Setup ==================
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
        _eventBonuses.text = $"Bonus (2 additional cards): {eventData.GetEventBonuses()}";

        Vector2 requirements = eventData.GetRequirements(playerNum);
        CardTag primaryTag = eventData.GetPrimaryResource();
        GameObject primaryResource = Instantiate(_eventTagIconPref, _eventTagIconSlot);
        primaryResource.GetComponentInChildren<TagIcon>().SetupIcon(primaryTag.visual, primaryTag.name);
        primaryResource.GetComponentInChildren<TextMeshProUGUI>().text = requirements.x.ToString();

        if(requirements.y > 0)
        {
            CardTag secondaryTag = eventData.GetSecondaryResource();
            GameObject secondaryResource = Instantiate(_eventTagIconPref, _eventTagIconSlot);
            secondaryResource.GetComponentInChildren<TagIcon>().SetupIcon(secondaryTag.visual, secondaryTag.name);
            secondaryResource.GetComponentInChildren<TextMeshProUGUI>().text = requirements.y.ToString();
        }
    }

    public int GetEventID()
    {
        return _heldEventID;
    }
}
