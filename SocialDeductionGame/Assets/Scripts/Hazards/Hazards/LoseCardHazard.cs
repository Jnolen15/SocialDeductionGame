using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hazard/Lose Card Hazard")]
public class LoseCardHazard : Hazard
{
    [Header("Card Loss Stats")]
    [SerializeField] private int _numCardsLost;

    // ========== METHOD OVERRIDES ==========
    public override void InvokeHazardConsequence()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            if (_numCardsLost != 0)
                playerObj.GetComponent<PlayerCardManager>().DiscardRandom(_numCardsLost);
        }
        else
            Debug.LogError("Player object not found!");
    }
}
