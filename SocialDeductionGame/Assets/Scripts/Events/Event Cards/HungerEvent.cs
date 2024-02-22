using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Night Event/Hunger Event")]
public class HungerEvent : NightEvent
{
    // ========== METHOD OVERRIDES ==========
    public override void InvokeEvent(GameObject player = null)
    {
        if (player == null)
        {
            Debug.LogError("<color=yellow>Server: </color>Cannot enact night event. Player object not given!");
            return;
        }

        PlayerHealth pHealth = player.GetComponent<PlayerHealth>();

        pHealth.HungerDrainIncreaseServerRpc();
    }
}
