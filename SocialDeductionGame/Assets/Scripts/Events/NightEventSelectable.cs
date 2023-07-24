using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NightEventSelectable : MonoBehaviour
{
    // Refrences
    private NightEventCardVisual _event;
    private NightEventPicker _eventPicker;
    // Variables
    private bool _eventSelected;

    void Start()
    {
        _event = GetComponent<NightEventCardVisual>();
        _eventPicker = GetComponentInParent<NightEventPicker>();
    }

    public void OnSelect()
    {
        _eventPicker.SelectEvent(_event.GetEventID());

        /*if (!_eventSelected)
        {
            _eventSelected = _eventPicker.SelectEvent(_event);

            if (_eventSelected)
                transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }
        else
        {
            _eventPicker.DeselectCard(_card);
            _eventSelected = false;
            transform.localScale = Vector3.one;
        }*/
    }
}
