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
    [Header("Requirement = this * number of players")]
    [SerializeField] private float _requirementCalculation;

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
        player.GetComponent<PlayerHealth>().ModifyHunger(-_hunger);
    }

    public override int SPCalculation(int numPlayers)
    {
        int num = (int)((float)numPlayers * _requirementCalculation);

        if (num <= 1)
            num = 1;

        return num;
    }
}
