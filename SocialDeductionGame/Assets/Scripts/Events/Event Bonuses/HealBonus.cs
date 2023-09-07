using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Night Event/Heal Bonus")]
public class HealBonus : EventBonus
{
    [Header("Heal Ammount")]
    [SerializeField] private int _heal;
    [Header("Hunger Gain Ammount")]
    [SerializeField] private int _hunger;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeBonus()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Cannot enact night event. Player object not found!");
            return;
        }

        player.GetComponent<PlayerHealth>().ModifyHealth(+_heal);
        player.GetComponent<PlayerHealth>().ModifyHunger(+_hunger);
    }
}