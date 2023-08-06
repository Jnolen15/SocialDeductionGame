using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Night Event/Damage Event")]
public class DamageEvent : NightEvent
{
    [Header("Damage Ammount")]
    [SerializeField] private int _dmg;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeEvent()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Cannot enact night event. Player object not found!");
            return;
        }

        player.GetComponent<PlayerHealth>().ModifyHealth(-_dmg);
    }

    public override int SPCalculation(int numPlayers)
    {
        return numPlayers;
    }
}
