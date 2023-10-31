using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Night Event/Danger Bonus")]
public class DangerBonus : EventBonus
{
    [Header("Danger Reduction Ammount")]
    [SerializeField] private int _dangerReduction;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeBonus()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Cannot enact night event. Player object not found!");
            return;
        }

        //player.GetComponent<PlayerData>().ModifyDangerLevel(-_dangerReduction);
    }
}
