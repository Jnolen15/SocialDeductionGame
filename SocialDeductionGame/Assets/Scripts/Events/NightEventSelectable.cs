using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NightEventSelectable : MonoBehaviour
{
    // ================== Refrences ==================
    private NightEventCardVisual _event;
    private NightEventPicker _eventPicker;
    [SerializeField] private TextMeshProUGUI _voteText;
    private PlayRandomSound _randSound;
    // ================== Variables ==================
    private bool _eventSelected;

    // ================== Setup ==================
    private void OnEnable()
    {
        Setup();
    }

    void Setup()
    {
        _event = GetComponent<NightEventCardVisual>();
        _eventPicker = GetComponentInParent<NightEventPicker>();

        _randSound = this.GetComponent<PlayRandomSound>();
    }

    // ================== Function ==================
    public void OnSelect()
    {
        if (_eventPicker == null)
            Setup();

        if (!_eventSelected)
        {
            _eventSelected = true;
            _eventPicker.SelectEvent(_event.GetEventID());
            transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

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
}
