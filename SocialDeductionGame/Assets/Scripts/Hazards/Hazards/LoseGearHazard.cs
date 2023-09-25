using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hazard/Lose Gear Hazard")]
public class LoseGearHazard : Hazard
{
    [Header("Card Loss Stats")]
    [SerializeField] private int[] _gearSlots;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeHazardConsequence()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            for(int i = 0; i < _gearSlots.Length; i++)
            {
                playerObj.GetComponent<PlayerCardManager>().LoseGear(_gearSlots[i]);
            }
        }
        else
            Debug.LogError("Player object not found!");
    }
}
