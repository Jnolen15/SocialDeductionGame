using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Night Event/Danger Event")]
public class DangerEvent : NightEvent
{
    [Header("Danger Increase Ammount")]
    [SerializeField] private int _danger;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeEvent()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Cannot enact night event. Player object not found!");
            return;
        }

        //player.GetComponent<PlayerData>().ModifyDangerLevel(_danger);
    }
}
