using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hazard/Lose Gear Hazard")]
public class LoseGearHazard : Hazard
{
    [Header("Card Loss Stats")]
    [SerializeField] private int[] _gearSlots;
    [SerializeField] private int _numCardsLost;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeHazardConsequence()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            // Loose gear
            for(int i = 0; i < _gearSlots.Length; i++)
            {
                playerObj.GetComponent<PlayerCardManager>().LoseGear(_gearSlots[i]);
            }

            // Loose cards
            if (_numCardsLost != 0)
                playerObj.GetComponent<PlayerCardManager>().DiscardRandom(_numCardsLost);
        }
        else
            Debug.LogError("Player object not found!");
    }
}
