using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Night Event/Hunger Bonus")]
public class HungerBonus : EventBonus
{
    // ========== METHOD OVERRIDES ==========
    public override void InvokeBonus(GameObject player = null)
    {
        if (player == null)
        {
            Debug.LogError("Cannot enact night bonus. Player object not found!");
            return;
        }

        PlayerHealth pHealth = player.GetComponent<PlayerHealth>();

        pHealth.HungerDrainDiminishServerRpc();
    }
}
