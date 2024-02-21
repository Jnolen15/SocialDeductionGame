using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Night Event/Danger Event")]
public class DangerEvent : NightEvent
{
    [Header("Danger Event Details")]
    [SerializeField] private LocationManager.LocationName _location;

    // ========== METHOD OVERRIDES ==========
    // This event bonus should only be invoked by server
    public override void InvokeEvent(GameObject player = null)
    {
        GameObject gameManager = GameObject.FindGameObjectWithTag("GameManager");

        if (gameManager != null)
        {
            Debug.Log("<color=yellow>Server: </color>Setting Location Debuff at " + _location.ToString());
            gameManager.GetComponent<LocationManager>().SetLocationDebuff(_location);
        }
        else
        {
            Debug.LogError("<color=yellow>Server: </color>Cannot enact night event. Game Manager object not found!");
            return;
        }
    }
}
