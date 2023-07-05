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
        PlayerHealth pHealth = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHealth>();
        pHealth.ModifyHealth(-_dmg);
    }

    public override int SPCalculation(int numPlayers)
    {
        return numPlayers;
    }
}
