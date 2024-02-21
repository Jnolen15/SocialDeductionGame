using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Night Event/Damage Event")]
public class DamageEvent : NightEvent
{
    [Header("Damage Ammount")]
    [SerializeField] private int _dmg;
    [Header("Hunger Loss Ammount")]
    [SerializeField] private int _hunger;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeEvent(GameObject player = null)
    {
        if (player == null)
        {
            Debug.LogError("<color=yellow>Server: </color>Cannot enact night event. Player object not given!");
            return;
        }

        PlayerHealth pHealth = player.GetComponent<PlayerHealth>();

        pHealth.ModifyHealth(-_dmg, "DamageEvent Event Consequence");
        pHealth.ModifyHunger(-_hunger, "DamageEvent Event Consequence");
    }
}
