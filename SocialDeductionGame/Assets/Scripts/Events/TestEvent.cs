using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Night Event/Test Event")]
public class TestEvent : NightEvent
{
    [Header("Message")]
    [SerializeField] private string _message;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeEvent()
    {
        Debug.Log("<color=red>EVENT: </color>" + _message);
    }
}
